// src/InventoryTracker.Api/Services/InventoryService.cs
// Implementation of stock movement transactions, balance updates, and transaction auditing.
// Connects to: src/InventoryTracker.Api/Data/InventoryDbContext.cs, src/InventoryTracker.Api/Models/InventoryTransaction.cs
// Created: 2026-08-26

using InventoryTracker.Api.Data;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service executing stock adjustments, restock intake, dispatch fulfillment, and transaction logs.
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(InventoryDbContext context, ILogger<InventoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TransactionDto> AdjustStockAsync(StockAdjustmentDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.QuantityChange == 0)
        {
            throw new ArgumentException("QuantityChange cannot be zero.");
        }

        var product = await _context.Products.FindAsync(new object[] { dto.ProductId }, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID {dto.ProductId} was not found.");
        }

        var newQuantity = product.QuantityInStock + dto.QuantityChange;
        if (newQuantity < 0)
        {
            throw new InvalidOperationException($"Cannot adjust stock by {dto.QuantityChange}. Resulting stock ({newQuantity}) would be negative for SKU '{product.Sku}'. Current on-hand is {product.QuantityInStock}.");
        }

        product.QuantityInStock = newQuantity;
        product.UpdatedAtUtc = DateTime.UtcNow;

        var transaction = new InventoryTransaction
        {
            ProductId = product.Id,
            Type = dto.QuantityChange > 0 ? TransactionType.Adjustment : TransactionType.Adjustment,
            QuantityChange = dto.QuantityChange,
            QuantityAfter = newQuantity,
            UnitCost = product.UnitCost,
            Reason = dto.Reason.Trim(),
            ReferenceNumber = dto.ReferenceNumber?.Trim(),
            PerformedBy = string.IsNullOrWhiteSpace(dto.PerformedBy) ? "system" : dto.PerformedBy.Trim(),
            TimestampUtc = DateTime.UtcNow
        };

        await _context.InventoryTransactions.AddAsync(transaction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Stock adjusted for {Sku}: Change={Change}, NewTotal={Total}, Reason={Reason}",
            product.Sku, dto.QuantityChange, newQuantity, dto.Reason);

        return MapTransactionToDto(transaction, product);
    }

    public async Task<TransactionDto> RestockAsync(RestockRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Quantity <= 0)
        {
            throw new ArgumentException("Restock quantity must be greater than zero.");
        }

        var product = await _context.Products.FindAsync(new object[] { dto.ProductId }, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID {dto.ProductId} was not found.");
        }

        // Calculate weighted average unit cost if new cost is supplied
        if (dto.UnitCost > 0)
        {
            var existingTotalCost = product.QuantityInStock * product.UnitCost;
            var incomingTotalCost = dto.Quantity * dto.UnitCost;
            var totalUnits = product.QuantityInStock + dto.Quantity;
            product.UnitCost = Math.Round((existingTotalCost + incomingTotalCost) / totalUnits, 2);
        }

        var newQuantity = product.QuantityInStock + dto.Quantity;
        product.QuantityInStock = newQuantity;
        product.UpdatedAtUtc = DateTime.UtcNow;

        var transaction = new InventoryTransaction
        {
            ProductId = product.Id,
            Type = TransactionType.StockIn,
            QuantityChange = dto.Quantity,
            QuantityAfter = newQuantity,
            UnitCost = dto.UnitCost > 0 ? dto.UnitCost : product.UnitCost,
            Reason = string.IsNullOrWhiteSpace(dto.Notes) ? "Inbound supplier replenishment" : dto.Notes.Trim(),
            ReferenceNumber = dto.PurchaseOrderNumber?.Trim(),
            PerformedBy = string.IsNullOrWhiteSpace(dto.PerformedBy) ? "warehouse" : dto.PerformedBy.Trim(),
            TimestampUtc = DateTime.UtcNow
        };

        await _context.InventoryTransactions.AddAsync(transaction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Restock processed for {Sku}: +{Quantity} units, PO={PO}, NewTotal={Total}",
            product.Sku, dto.Quantity, dto.PurchaseOrderNumber, newQuantity);

        return MapTransactionToDto(transaction, product);
    }

    public async Task<TransactionDto> DispatchAsync(DispatchRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Quantity <= 0)
        {
            throw new ArgumentException("Dispatch quantity must be greater than zero.");
        }

        var product = await _context.Products.FindAsync(new object[] { dto.ProductId }, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID {dto.ProductId} was not found.");
        }

        if (product.QuantityInStock < dto.Quantity)
        {
            throw new InvalidOperationException($"Insufficient inventory for '{product.Sku}'. Requested: {dto.Quantity}, Available: {product.QuantityInStock}.");
        }

        var newQuantity = product.QuantityInStock - dto.Quantity;
        product.QuantityInStock = newQuantity;
        product.UpdatedAtUtc = DateTime.UtcNow;

        var transaction = new InventoryTransaction
        {
            ProductId = product.Id,
            Type = TransactionType.StockOut,
            QuantityChange = -dto.Quantity,
            QuantityAfter = newQuantity,
            UnitCost = product.UnitCost,
            Reason = string.IsNullOrWhiteSpace(dto.Notes) ? "Outbound order fulfillment" : dto.Notes.Trim(),
            ReferenceNumber = dto.SalesOrderNumber?.Trim(),
            PerformedBy = string.IsNullOrWhiteSpace(dto.PerformedBy) ? "dispatch" : dto.PerformedBy.Trim(),
            TimestampUtc = DateTime.UtcNow
        };

        await _context.InventoryTransactions.AddAsync(transaction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Dispatch processed for {Sku}: -{Quantity} units, SO={SO}, RemainingTotal={Total}",
            product.Sku, dto.Quantity, dto.SalesOrderNumber, newQuantity);

        return MapTransactionToDto(transaction, product);
    }

    public async Task<PagedResult<TransactionDto>> GetTransactionsAsync(TransactionFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.InventoryTransactions
            .AsNoTracking()
            .Include(t => t.Product)
            .AsQueryable();

        if (filter.ProductId.HasValue)
        {
            query = query.Where(t => t.ProductId == filter.ProductId.Value);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(t => t.Type == filter.Type.Value);
        }

        if (filter.FromUtc.HasValue)
        {
            query = query.Where(t => t.TimestampUtc >= filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue)
        {
            query = query.Where(t => t.TimestampUtc <= filter.ToUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(t => t.TimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => MapTransactionToDto(t, t.Product!))
            .ToListAsync(cancellationToken);

        return PagedResult<TransactionDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<TransactionDto>> GetProductTransactionsAsync(int productId, int limit = 50, CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 200);
        return await _context.InventoryTransactions
            .AsNoTracking()
            .Include(t => t.Product)
            .Where(t => t.ProductId == productId)
            .OrderByDescending(t => t.TimestampUtc)
            .Take(safeLimit)
            .Select(t => MapTransactionToDto(t, t.Product!))
            .ToListAsync(cancellationToken);
    }

    private static TransactionDto MapTransactionToDto(InventoryTransaction t, Product p) => new()
    {
        Id = t.Id,
        ProductId = t.ProductId,
        ProductSku = p.Sku,
        ProductName = p.Name,
        Type = t.Type,
        QuantityChange = t.QuantityChange,
        QuantityAfter = t.QuantityAfter,
        UnitCost = t.UnitCost,
        Reason = t.Reason,
        ReferenceNumber = t.ReferenceNumber,
        PerformedBy = t.PerformedBy,
        TimestampUtc = t.TimestampUtc
    };
}
