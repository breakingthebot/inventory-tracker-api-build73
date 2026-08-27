// src/InventoryTracker.Api/Services/TransferService.cs
// Implementation of multi-stage inter-warehouse stock transfer workflows and inventory balance synchronization.
// Connects to: src/InventoryTracker.Api/Data/InventoryDbContext.cs, src/InventoryTracker.Api/Models/StockTransfer.cs
// Created: 2026-08-27

using InventoryTracker.Api.Data;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Orchestrates inter-warehouse transfer workflows: order creation, shipment dispatch, destination receiving, and cancellation.
/// </summary>
public class TransferService : ITransferService
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<TransferService> _logger;

    public TransferService(InventoryDbContext context, ILogger<TransferService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<StockTransferDto> CreateTransferAsync(CreateStockTransferDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.SourceWarehouseId == dto.DestinationWarehouseId)
        {
            throw new ArgumentException("Source and Destination warehouses must be different facilities.");
        }

        if (dto.Items == null || dto.Items.Count == 0)
        {
            throw new ArgumentException("Transfer must include at least one item.");
        }

        var sourceWarehouse = await _context.Warehouses.FindAsync(new object[] { dto.SourceWarehouseId }, cancellationToken);
        if (sourceWarehouse == null || !sourceWarehouse.IsActive)
        {
            throw new KeyNotFoundException($"Source warehouse with ID {dto.SourceWarehouseId} was not found or is inactive.");
        }

        var destWarehouse = await _context.Warehouses.FindAsync(new object[] { dto.DestinationWarehouseId }, cancellationToken);
        if (destWarehouse == null || !destWarehouse.IsActive)
        {
            throw new KeyNotFoundException($"Destination warehouse with ID {dto.DestinationWarehouseId} was not found or is inactive.");
        }

        var transferItems = new List<StockTransferItem>();

        foreach (var itemDto in dto.Items)
        {
            if (itemDto.Quantity <= 0)
            {
                throw new ArgumentException($"Quantity for Product ID {itemDto.ProductId} must be greater than zero.");
            }

            var product = await _context.Products.FindAsync(new object[] { itemDto.ProductId }, cancellationToken);
            if (product == null || !product.IsActive)
            {
                throw new KeyNotFoundException($"Product with ID {itemDto.ProductId} was not found or is inactive.");
            }

            var sourceStock = await _context.WarehouseStocks
                .FirstOrDefaultAsync(ws => ws.WarehouseId == dto.SourceWarehouseId && ws.ProductId == itemDto.ProductId, cancellationToken);

            var available = sourceStock?.AvailableQuantity ?? 0;
            if (available < itemDto.Quantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient available stock for '{product.Sku}' in {sourceWarehouse.Code}. Requested: {itemDto.Quantity}, Available: {available}.");
            }

            // Reserve stock at source warehouse
            sourceStock!.QuantityReserved += itemDto.Quantity;
            sourceStock.UpdatedAtUtc = DateTime.UtcNow;

            transferItems.Add(new StockTransferItem
            {
                ProductId = product.Id,
                QuantityRequested = itemDto.Quantity,
                QuantityShipped = 0,
                QuantityReceived = 0,
                UnitCost = product.UnitCost
            });
        }

        var transferNumber = $"TRF-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

        var transfer = new StockTransfer
        {
            TransferNumber = transferNumber,
            SourceWarehouseId = dto.SourceWarehouseId,
            DestinationWarehouseId = dto.DestinationWarehouseId,
            Status = StockTransferStatus.Pending,
            RequestedBy = string.IsNullOrWhiteSpace(dto.RequestedBy) ? "system" : dto.RequestedBy.Trim(),
            Notes = dto.Notes?.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            Items = transferItems
        };

        await _context.StockTransfers.AddAsync(transfer, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Stock transfer created: {TransferNumber} from {Source} to {Dest} ({Count} items)",
            transfer.TransferNumber, sourceWarehouse.Code, destWarehouse.Code, transferItems.Count);

        return await LoadTransferDtoAsync(transfer.Id, cancellationToken);
    }

    public async Task<StockTransferDto> ShipTransferAsync(int transferId, ShipTransferDto dto, CancellationToken cancellationToken = default)
    {
        var transfer = await _context.StockTransfers
            .Include(t => t.Items)
            .ThenInclude(i => i.Product)
            .Include(t => t.SourceWarehouse)
            .Include(t => t.DestinationWarehouse)
            .FirstOrDefaultAsync(t => t.Id == transferId, cancellationToken);

        if (transfer == null)
        {
            throw new KeyNotFoundException($"Stock transfer with ID {transferId} was not found.");
        }

        if (transfer.Status != StockTransferStatus.Pending && transfer.Status != StockTransferStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot ship transfer '{transfer.TransferNumber}' in status '{transfer.Status}'. Must be Pending or Draft.");
        }

        foreach (var item in transfer.Items)
        {
            var sourceStock = await _context.WarehouseStocks
                .FirstOrDefaultAsync(ws => ws.WarehouseId == transfer.SourceWarehouseId && ws.ProductId == item.ProductId, cancellationToken);

            if (sourceStock == null || sourceStock.QuantityOnHand < item.QuantityRequested)
            {
                throw new InvalidOperationException($"Source warehouse stock shortage during shipment for Product ID {item.ProductId}.");
            }

            // Deduct physical on-hand and release reservation
            sourceStock.QuantityOnHand -= item.QuantityRequested;
            sourceStock.QuantityReserved -= item.QuantityRequested;
            sourceStock.UpdatedAtUtc = DateTime.UtcNow;

            item.QuantityShipped = item.QuantityRequested;

            // Record stock movement transaction
            var tx = new InventoryTransaction
            {
                ProductId = item.ProductId,
                Type = TransactionType.StockOut,
                QuantityChange = -item.QuantityRequested,
                QuantityAfter = sourceStock.QuantityOnHand,
                UnitCost = item.UnitCost,
                Reason = $"Inter-warehouse transfer {transfer.TransferNumber} shipped to {transfer.DestinationWarehouse?.Code}",
                ReferenceNumber = transfer.TransferNumber,
                PerformedBy = dto.ShippedBy,
                TimestampUtc = DateTime.UtcNow
            };
            await _context.InventoryTransactions.AddAsync(tx, cancellationToken);
        }

        transfer.Status = StockTransferStatus.InTransit;
        transfer.ShippedAtUtc = DateTime.UtcNow;
        transfer.TrackingNumber = dto.TrackingNumber?.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Notes))
        {
            transfer.Notes = string.IsNullOrWhiteSpace(transfer.Notes) ? dto.Notes : $"{transfer.Notes} | {dto.Notes}";
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Stock transfer shipped: {TransferNumber} (In-Transit)", transfer.TransferNumber);

        return await LoadTransferDtoAsync(transfer.Id, cancellationToken);
    }

    public async Task<StockTransferDto> ReceiveTransferAsync(int transferId, ReceiveTransferDto dto, CancellationToken cancellationToken = default)
    {
        var transfer = await _context.StockTransfers
            .Include(t => t.Items)
            .ThenInclude(i => i.Product)
            .Include(t => t.SourceWarehouse)
            .Include(t => t.DestinationWarehouse)
            .FirstOrDefaultAsync(t => t.Id == transferId, cancellationToken);

        if (transfer == null)
        {
            throw new KeyNotFoundException($"Stock transfer with ID {transferId} was not found.");
        }

        if (transfer.Status != StockTransferStatus.InTransit)
        {
            throw new InvalidOperationException($"Cannot receive transfer '{transfer.TransferNumber}' in status '{transfer.Status}'. Must be InTransit.");
        }

        foreach (var item in transfer.Items)
        {
            var destStock = await _context.WarehouseStocks
                .FirstOrDefaultAsync(ws => ws.WarehouseId == transfer.DestinationWarehouseId && ws.ProductId == item.ProductId, cancellationToken);

            if (destStock == null)
            {
                destStock = new WarehouseStock
                {
                    WarehouseId = transfer.DestinationWarehouseId,
                    ProductId = item.ProductId,
                    QuantityOnHand = item.QuantityShipped,
                    QuantityReserved = 0,
                    BinLocation = "UNASSIGNED",
                    UpdatedAtUtc = DateTime.UtcNow
                };
                await _context.WarehouseStocks.AddAsync(destStock, cancellationToken);
            }
            else
            {
                destStock.QuantityOnHand += item.QuantityShipped;
                destStock.UpdatedAtUtc = DateTime.UtcNow;
            }

            item.QuantityReceived = item.QuantityShipped;

            // Record stock intake transaction at destination
            var tx = new InventoryTransaction
            {
                ProductId = item.ProductId,
                Type = TransactionType.StockIn,
                QuantityChange = item.QuantityShipped,
                QuantityAfter = destStock.QuantityOnHand,
                UnitCost = item.UnitCost,
                Reason = $"Inter-warehouse transfer {transfer.TransferNumber} received from {transfer.SourceWarehouse?.Code}",
                ReferenceNumber = transfer.TransferNumber,
                PerformedBy = dto.ReceivedBy,
                TimestampUtc = DateTime.UtcNow
            };
            await _context.InventoryTransactions.AddAsync(tx, cancellationToken);
        }

        transfer.Status = StockTransferStatus.Received;
        transfer.ReceivedAtUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(dto.Notes))
        {
            transfer.Notes = string.IsNullOrWhiteSpace(transfer.Notes) ? dto.Notes : $"{transfer.Notes} | {dto.Notes}";
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Stock transfer received: {TransferNumber} at {Dest}", transfer.TransferNumber, transfer.DestinationWarehouse?.Code);

        return await LoadTransferDtoAsync(transfer.Id, cancellationToken);
    }

    public async Task<StockTransferDto> CancelTransferAsync(int transferId, string reason, CancellationToken cancellationToken = default)
    {
        var transfer = await _context.StockTransfers
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == transferId, cancellationToken);

        if (transfer == null)
        {
            throw new KeyNotFoundException($"Stock transfer with ID {transferId} was not found.");
        }

        if (transfer.Status != StockTransferStatus.Pending && transfer.Status != StockTransferStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot cancel transfer '{transfer.TransferNumber}' in status '{transfer.Status}'. Items have already shipped.");
        }

        // Release reserved stock at source warehouse
        foreach (var item in transfer.Items)
        {
            var sourceStock = await _context.WarehouseStocks
                .FirstOrDefaultAsync(ws => ws.WarehouseId == transfer.SourceWarehouseId && ws.ProductId == item.ProductId, cancellationToken);

            if (sourceStock != null)
            {
                sourceStock.QuantityReserved = Math.Max(0, sourceStock.QuantityReserved - item.QuantityRequested);
                sourceStock.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        transfer.Status = StockTransferStatus.Cancelled;
        transfer.Notes = string.IsNullOrWhiteSpace(transfer.Notes) ? $"Cancelled: {reason}" : $"{transfer.Notes} | Cancelled: {reason}";

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Stock transfer cancelled: {TransferNumber}", transfer.TransferNumber);

        return await LoadTransferDtoAsync(transfer.Id, cancellationToken);
    }

    public async Task<StockTransferDto?> GetTransferByIdAsync(int transferId, CancellationToken cancellationToken = default)
    {
        var exists = await _context.StockTransfers.AnyAsync(t => t.Id == transferId, cancellationToken);
        return exists ? await LoadTransferDtoAsync(transferId, cancellationToken) : null;
    }

    public async Task<PagedResult<StockTransferDto>> GetTransfersAsync(TransferFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.StockTransfers
            .AsNoTracking()
            .Include(t => t.SourceWarehouse)
            .Include(t => t.DestinationWarehouse)
            .Include(t => t.Items)
            .ThenInclude(i => i.Product)
            .AsQueryable();

        if (filter.SourceWarehouseId.HasValue)
        {
            query = query.Where(t => t.SourceWarehouseId == filter.SourceWarehouseId.Value);
        }

        if (filter.DestinationWarehouseId.HasValue)
        {
            query = query.Where(t => t.DestinationWarehouseId == filter.DestinationWarehouseId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(t => t.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(t => t.TransferNumber.ToLower().Contains(search) ||
                                     (t.TrackingNumber != null && t.TrackingNumber.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapToDto).ToList();
        return PagedResult<StockTransferDto>.Create(dtos, totalCount, page, pageSize);
    }

    private async Task<StockTransferDto> LoadTransferDtoAsync(int transferId, CancellationToken cancellationToken)
    {
        var t = await _context.StockTransfers
            .AsNoTracking()
            .Include(x => x.SourceWarehouse)
            .Include(x => x.DestinationWarehouse)
            .Include(x => x.Items)
            .ThenInclude(i => i.Product)
            .FirstAsync(x => x.Id == transferId, cancellationToken);

        return MapToDto(t);
    }

    private static StockTransferDto MapToDto(StockTransfer t) => new()
    {
        Id = t.Id,
        TransferNumber = t.TransferNumber,
        SourceWarehouseId = t.SourceWarehouseId,
        SourceWarehouseCode = t.SourceWarehouse?.Code ?? string.Empty,
        SourceWarehouseName = t.SourceWarehouse?.Name ?? string.Empty,
        DestinationWarehouseId = t.DestinationWarehouseId,
        DestinationWarehouseCode = t.DestinationWarehouse?.Code ?? string.Empty,
        DestinationWarehouseName = t.DestinationWarehouse?.Name ?? string.Empty,
        Status = t.Status,
        RequestedBy = t.RequestedBy,
        TrackingNumber = t.TrackingNumber,
        Notes = t.Notes,
        TotalItemsRequested = t.Items.Sum(i => i.QuantityRequested),
        TotalItemsShipped = t.Items.Sum(i => i.QuantityShipped),
        TotalItemsReceived = t.Items.Sum(i => i.QuantityReceived),
        TotalTransferValuation = t.Items.Sum(i => i.QuantityRequested * i.UnitCost),
        CreatedAtUtc = t.CreatedAtUtc,
        ShippedAtUtc = t.ShippedAtUtc,
        ReceivedAtUtc = t.ReceivedAtUtc,
        Items = t.Items.Select(i => new StockTransferItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductSku = i.Product?.Sku ?? string.Empty,
            ProductName = i.Product?.Name ?? string.Empty,
            QuantityRequested = i.QuantityRequested,
            QuantityShipped = i.QuantityShipped,
            QuantityReceived = i.QuantityReceived,
            UnitCost = i.UnitCost
        }).ToList()
    };
}
