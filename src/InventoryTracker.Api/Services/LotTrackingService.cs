// src/InventoryTracker.Api/Services/LotTrackingService.cs
// Implementation of batch lot tracking, FEFO dispatching algorithms, expiration monitoring, and transaction auditing.
// Connects to: src/InventoryTracker.Api/Data/InventoryDbContext.cs, src/InventoryTracker.Api/Models/ProductLot.cs
// Created: 2026-08-27

using InventoryTracker.Api.Data;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service providing batch lot tracking, First-Expired First-Out (FEFO) picking, and expiration risk reports.
/// </summary>
public class LotTrackingService : ILotTrackingService
{
    private readonly InventoryDbContext _context;
    private readonly IWebhookService _webhookService;
    private readonly ILogger<LotTrackingService> _logger;

    public LotTrackingService(InventoryDbContext context, IWebhookService webhookService, ILogger<LotTrackingService> logger)
    {
        _context = context;
        _webhookService = webhookService;
        _logger = logger;
    }

    public async Task<ProductLotDto> CreateLotAsync(CreateProductLotDto dto, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync(new object[] { dto.ProductId }, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID {dto.ProductId} was not found.");
        }

        var warehouse = await _context.Warehouses.FindAsync(new object[] { dto.WarehouseId }, cancellationToken);
        if (warehouse == null)
        {
            throw new KeyNotFoundException($"Warehouse with ID {dto.WarehouseId} was not found.");
        }

        var lotNumber = dto.LotNumber.Trim().ToUpperInvariant();
        var existingLot = await _context.ProductLots
            .AnyAsync(l => l.ProductId == dto.ProductId && l.WarehouseId == dto.WarehouseId && l.LotNumber == lotNumber, cancellationToken);

        if (existingLot)
        {
            throw new InvalidOperationException($"Lot '{lotNumber}' already exists for this product at the specified warehouse facility.");
        }

        var lot = new ProductLot
        {
            ProductId = dto.ProductId,
            WarehouseId = dto.WarehouseId,
            LotNumber = lotNumber,
            QuantityInitial = dto.Quantity,
            QuantityOnHand = dto.Quantity,
            QuantityReserved = 0,
            ManufactureDateUtc = dto.ManufactureDateUtc,
            ExpirationDateUtc = dto.ExpirationDateUtc,
            Status = dto.Status,
            ReceivedAtUtc = DateTime.UtcNow,
            Notes = dto.Notes?.Trim()
        };

        // Enable lot tracking on product
        product.IsLotTracked = true;

        await _context.ProductLots.AddAsync(lot, cancellationToken);

        // Synchronize warehouse stock
        var whStock = await _context.WarehouseStocks
            .FirstOrDefaultAsync(ws => ws.WarehouseId == dto.WarehouseId && ws.ProductId == dto.ProductId, cancellationToken);

        if (whStock == null)
        {
            whStock = new WarehouseStock
            {
                WarehouseId = dto.WarehouseId,
                ProductId = dto.ProductId,
                QuantityOnHand = dto.Quantity,
                QuantityReserved = 0,
                UpdatedAtUtc = DateTime.UtcNow
            };
            await _context.WarehouseStocks.AddAsync(whStock, cancellationToken);
        }
        else
        {
            whStock.QuantityOnHand += dto.Quantity;
            whStock.UpdatedAtUtc = DateTime.UtcNow;
        }

        product.QuantityInStock += dto.Quantity;

        // Record stock in transaction
        var transaction = new InventoryTransaction
        {
            ProductId = dto.ProductId,
            Type = TransactionType.StockIn,
            QuantityChange = dto.Quantity,
            QuantityAfter = product.QuantityInStock,
            UnitCost = product.UnitCost,
            ReferenceNumber = $"LOT-RECEIPT-{lotNumber}",
            Reason = $"Initial lot receipt {lotNumber}",
            TimestampUtc = DateTime.UtcNow
        };
        await _context.InventoryTransactions.AddAsync(transaction, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Created ProductLot {LotNumber} (Qty: {Qty}) for Product {Sku} at {WhCode}",
            lot.LotNumber, lot.QuantityOnHand, product.Sku, warehouse.Code);

        return MapToDto(lot, product, warehouse);
    }

    public async Task<ProductLotDto?> GetLotByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var lot = await _context.ProductLots
            .Include(l => l.Product)
            .Include(l => l.Warehouse)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        return lot == null ? null : MapToDto(lot, lot.Product!, lot.Warehouse!);
    }

    public async Task<PagedResult<ProductLotDto>> GetLotsAsync(LotFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.ProductLots
            .Include(l => l.Product)
            .Include(l => l.Warehouse)
            .AsNoTracking()
            .AsQueryable();

        if (filter.ProductId.HasValue)
        {
            query = query.Where(l => l.ProductId == filter.ProductId.Value);
        }

        if (filter.WarehouseId.HasValue)
        {
            query = query.Where(l => l.WarehouseId == filter.WarehouseId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(l => l.Status == filter.Status.Value);
        }

        if (filter.ExpiredOnly == true)
        {
            var now = DateTime.UtcNow;
            query = query.Where(l => l.ExpirationDateUtc != null && l.ExpirationDateUtc < now);
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var lots = await query
            .OrderBy(l => l.ExpirationDateUtc ?? DateTime.MaxValue)
            .ThenBy(l => l.ReceivedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = lots.Select(l => MapToDto(l, l.Product!, l.Warehouse!)).ToList();
        return PagedResult<ProductLotDto>.Create(dtos, totalItems, pageNumber, pageSize);
    }

    public async Task<ProductLotDto?> UpdateLotAsync(int id, UpdateProductLotDto dto, CancellationToken cancellationToken = default)
    {
        var lot = await _context.ProductLots
            .Include(l => l.Product)
            .Include(l => l.Warehouse)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (lot == null)
        {
            return null;
        }

        lot.Status = dto.Status;
        if (dto.ExpirationDateUtc.HasValue)
        {
            lot.ExpirationDateUtc = dto.ExpirationDateUtc.Value;
        }
        if (dto.Notes != null)
        {
            lot.Notes = dto.Notes.Trim();
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated lot {LotId} to Status {Status}", lot.Id, lot.Status);

        return MapToDto(lot, lot.Product!, lot.Warehouse!);
    }

    public async Task<ExpiringLotsSummaryDto> GetExpiringLotsAsync(int daysThreshold = 30, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(Math.Max(1, daysThreshold));

        var lots = await _context.ProductLots
            .Include(l => l.Product)
            .Include(l => l.Warehouse)
            .AsNoTracking()
            .Where(l => l.Status == LotStatus.Active && l.QuantityOnHand > 0 && l.ExpirationDateUtc != null && l.ExpirationDateUtc <= cutoff)
            .OrderBy(l => l.ExpirationDateUtc)
            .ToListAsync(cancellationToken);

        var totalUnits = lots.Sum(l => l.QuantityOnHand);
        var atRiskValue = lots.Sum(l => l.QuantityOnHand * (l.Product?.UnitCost ?? 0m));

        return new ExpiringLotsSummaryDto
        {
            TotalExpiringLotsCount = lots.Count,
            TotalExpiringUnits = totalUnits,
            EstimatedAtRiskValuation = Math.Round(atRiskValue, 2),
            ExpiringLots = lots.Select(l => MapToDto(l, l.Product!, l.Warehouse!)).ToList()
        };
    }

    public async Task<FefoAllocationPlanDto> GetFefoAllocationPlanAsync(int productId, int requestedQuantity, int warehouseId, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync(new object[] { productId }, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID {productId} was not found.");
        }

        var warehouse = await _context.Warehouses.FindAsync(new object[] { warehouseId }, cancellationToken);
        if (warehouse == null)
        {
            throw new KeyNotFoundException($"Warehouse with ID {warehouseId} was not found.");
        }

        // FEFO: Pick Active non-expired lots ordered by earliest expiration date (nulls last), then oldest received
        var now = DateTime.UtcNow;
        var availableLots = await _context.ProductLots
            .Where(l => l.ProductId == productId && l.WarehouseId == warehouseId && l.Status == LotStatus.Active && l.QuantityOnHand > l.QuantityReserved)
            .Where(l => l.ExpirationDateUtc == null || l.ExpirationDateUtc > now)
            .OrderBy(l => l.ExpirationDateUtc ?? DateTime.MaxValue)
            .ThenBy(l => l.ReceivedAtUtc)
            .ToListAsync(cancellationToken);

        var allocations = new List<FefoLotAllocationItemDto>();
        var remainingNeeded = requestedQuantity;
        var totalAllocated = 0;

        foreach (var lot in availableLots)
        {
            if (remainingNeeded <= 0) break;

            var uncommitted = lot.AvailableQuantity;
            var pickQty = Math.Min(remainingNeeded, uncommitted);

            if (pickQty > 0)
            {
                allocations.Add(new FefoLotAllocationItemDto
                {
                    LotId = lot.Id,
                    LotNumber = lot.LotNumber,
                    ExpirationDateUtc = lot.ExpirationDateUtc,
                    DaysUntilExpiration = lot.ExpirationDateUtc.HasValue ? (int)(lot.ExpirationDateUtc.Value - now).TotalDays : null,
                    AvailableInLot = uncommitted,
                    QuantityToPick = pickQty
                });

                totalAllocated += pickQty;
                remainingNeeded -= pickQty;
            }
        }

        return new FefoAllocationPlanDto
        {
            ProductId = product.Id,
            ProductSku = product.Sku,
            ProductName = product.Name,
            WarehouseId = warehouse.Id,
            WarehouseCode = warehouse.Code,
            RequestedQuantity = requestedQuantity,
            TotalAllocatedQuantity = totalAllocated,
            Allocations = allocations
        };
    }

    public async Task<DispatchFefoResultDto> DispatchFefoAsync(DispatchFefoRequestDto dto, CancellationToken cancellationToken = default)
    {
        var plan = await GetFefoAllocationPlanAsync(dto.ProductId, dto.Quantity, dto.WarehouseId, cancellationToken);

        if (!plan.IsFullyAllocated)
        {
            throw new InvalidOperationException($"Insufficient uncommitted inventory in active lots to fulfill {dto.Quantity} units (Available: {plan.TotalAllocatedQuantity}).");
        }

        var product = await _context.Products.FindAsync(new object[] { dto.ProductId }, cancellationToken);
        var whStock = await _context.WarehouseStocks
            .FirstOrDefaultAsync(ws => ws.WarehouseId == dto.WarehouseId && ws.ProductId == dto.ProductId, cancellationToken);

        if (whStock == null || whStock.QuantityOnHand < dto.Quantity)
        {
            throw new InvalidOperationException("Warehouse stock level is insufficient.");
        }

        // Apply lot deductions
        foreach (var allocation in plan.Allocations)
        {
            var lot = await _context.ProductLots.FindAsync(new object[] { allocation.LotId }, cancellationToken);
            if (lot != null)
            {
                lot.QuantityOnHand -= allocation.QuantityToPick;
                if (lot.QuantityOnHand <= 0)
                {
                    lot.QuantityOnHand = 0;
                    lot.Status = LotStatus.Depleted;
                }
            }
        }

        // Apply warehouse and product deductions
        whStock.QuantityOnHand -= dto.Quantity;
        product!.QuantityInStock -= dto.Quantity;

        var transaction = new InventoryTransaction
        {
            ProductId = product.Id,
            Type = TransactionType.StockOut,
            QuantityChange = -dto.Quantity,
            QuantityAfter = product.QuantityInStock,
            UnitCost = product.UnitCost,
            ReferenceNumber = dto.ReferenceNumber.Trim(),
            Reason = $"FEFO Dispatch: {dto.Reason ?? "Customer Order"}",
            TimestampUtc = DateTime.UtcNow
        };
        await _context.InventoryTransactions.AddAsync(transaction, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        // Check for low stock notification
        if (product.QuantityInStock <= product.ReorderThreshold)
        {
            await _webhookService.PublishEventAsync(WebhookEventType.StockLow, new
            {
                ProductId = product.Id,
                Sku = product.Sku,
                Name = product.Name,
                StockOnHand = product.QuantityInStock,
                ReorderThreshold = product.ReorderThreshold
            }, cancellationToken);
        }

        _logger.LogInformation("Executed FEFO Dispatch for {Qty} units of {Sku} at WH {WhId} (Ref: {Ref})",
            dto.Quantity, product.Sku, dto.WarehouseId, dto.ReferenceNumber);

        return new DispatchFefoResultDto
        {
            ProductId = product.Id,
            ProductSku = product.Sku,
            WarehouseId = dto.WarehouseId,
            TotalDispatchedQuantity = dto.Quantity,
            ReferenceNumber = dto.ReferenceNumber,
            DispatchedLots = plan.Allocations
        };
    }

    private static ProductLotDto MapToDto(ProductLot l, Product p, Warehouse w) => new()
    {
        Id = l.Id,
        ProductId = l.ProductId,
        ProductSku = p.Sku,
        ProductName = p.Name,
        WarehouseId = l.WarehouseId,
        WarehouseCode = w.Code,
        WarehouseName = w.Name,
        LotNumber = l.LotNumber,
        QuantityInitial = l.QuantityInitial,
        QuantityOnHand = l.QuantityOnHand,
        QuantityReserved = l.QuantityReserved,
        AvailableQuantity = l.AvailableQuantity,
        ManufactureDateUtc = l.ManufactureDateUtc,
        ExpirationDateUtc = l.ExpirationDateUtc,
        Status = l.Status,
        ReceivedAtUtc = l.ReceivedAtUtc,
        Notes = l.Notes
    };
}
