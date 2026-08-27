// src/InventoryTracker.Api/Services/CycleCountService.cs
// Implementation of cycle counting snapshot generation, blind count entry, variance analytics, and reconciliation.
// Connects to: src/InventoryTracker.Api/Data/InventoryDbContext.cs, src/InventoryTracker.Api/Models/CycleCount.cs
// Created: 2026-08-27

using InventoryTracker.Api.Data;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service executing physical inventory audits, blind counting, variance reporting, and ledger adjustments.
/// </summary>
public class CycleCountService : ICycleCountService
{
    private readonly InventoryDbContext _context;
    private readonly IWebhookService _webhookService;
    private readonly ILogger<CycleCountService> _logger;

    public CycleCountService(InventoryDbContext context, IWebhookService webhookService, ILogger<CycleCountService> logger)
    {
        _context = context;
        _webhookService = webhookService;
        _logger = logger;
    }

    public async Task<CycleCountDto> CreateCycleCountAsync(CreateCycleCountDto dto, CancellationToken cancellationToken = default)
    {
        var warehouse = await _context.Warehouses.FindAsync(new object[] { dto.WarehouseId }, cancellationToken);
        if (warehouse == null)
        {
            throw new KeyNotFoundException($"Warehouse with ID {dto.WarehouseId} was not found.");
        }

        var stockQuery = _context.WarehouseStocks
            .Include(ws => ws.Product)
            .ThenInclude(p => p!.Category)
            .Where(ws => ws.WarehouseId == dto.WarehouseId);

        if (dto.CategoryId.HasValue)
        {
            stockQuery = stockQuery.Where(ws => ws.Product!.CategoryId == dto.CategoryId.Value);
        }

        var stocks = await stockQuery.ToListAsync(cancellationToken);
        if (stocks.Count == 0)
        {
            throw new InvalidOperationException("No stocked inventory items found matching the specified warehouse and category scope.");
        }

        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var countSequence = await _context.CycleCounts.CountAsync(cc => cc.CreatedAtUtc.Date == DateTime.UtcNow.Date, cancellationToken) + 1;
        var countNumber = $"CC-{today}-{countSequence:D3}";

        var cycleCount = new CycleCount
        {
            CountNumber = countNumber,
            WarehouseId = dto.WarehouseId,
            Status = CycleCountStatus.InProgress,
            Scope = dto.Scope.Trim(),
            InitiatedBy = string.IsNullOrWhiteSpace(dto.InitiatedBy) ? "system" : dto.InitiatedBy.Trim(),
            TotalItemsCounted = 0,
            TotalVarianceUnits = 0,
            TotalVarianceCost = 0m,
            CreatedAtUtc = DateTime.UtcNow,
            Notes = dto.Notes?.Trim()
        };

        foreach (var ws in stocks)
        {
            cycleCount.Items.Add(new CycleCountItem
            {
                ProductId = ws.ProductId,
                SystemQuantity = ws.QuantityOnHand,
                CountedQuantity = null,
                UnitCost = ws.Product?.UnitCost ?? 0m,
                IsReconciled = false
            });
        }

        await _context.CycleCounts.AddAsync(cycleCount, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Initiated Cycle Count {CountNumber} at Warehouse {WarehouseCode} with {ItemCount} items",
            cycleCount.CountNumber, warehouse.Code, cycleCount.Items.Count);

        return (await GetCycleCountByIdAsync(cycleCount.Id, cancellationToken))!;
    }

    public async Task<IReadOnlyList<CycleCountDto>> GetCycleCountsAsync(CycleCountStatus? status = null, int? warehouseId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.CycleCounts
            .Include(cc => cc.Warehouse)
            .Include(cc => cc.Items)
            .ThenInclude(cci => cci.Product)
            .ThenInclude(p => p!.Category)
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(cc => cc.Status == status.Value);
        }

        if (warehouseId.HasValue)
        {
            query = query.Where(cc => cc.WarehouseId == warehouseId.Value);
        }

        var list = await query.OrderByDescending(cc => cc.CreatedAtUtc).ToListAsync(cancellationToken);
        return list.Select(MapToDto).ToList();
    }

    public async Task<CycleCountDto?> GetCycleCountByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cycleCount = await _context.CycleCounts
            .Include(cc => cc.Warehouse)
            .Include(cc => cc.Items)
            .ThenInclude(cci => cci.Product)
            .ThenInclude(p => p!.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(cc => cc.Id == id, cancellationToken);

        return cycleCount == null ? null : MapToDto(cycleCount);
    }

    public async Task<CycleCountDto> RecordBatchCountAsync(int cycleCountId, RecordBatchCountDto dto, CancellationToken cancellationToken = default)
    {
        var cycleCount = await _context.CycleCounts
            .Include(cc => cc.Items)
            .FirstOrDefaultAsync(cc => cc.Id == cycleCountId, cancellationToken);

        if (cycleCount == null)
        {
            throw new KeyNotFoundException($"Cycle count session with ID {cycleCountId} was not found.");
        }

        if (cycleCount.Status != CycleCountStatus.InProgress && cycleCount.Status != CycleCountStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot record counts for a session in status '{cycleCount.Status}'.");
        }

        var countedBy = string.IsNullOrWhiteSpace(dto.CountedBy) ? "clerk" : dto.CountedBy.Trim();

        foreach (var countSubmission in dto.Counts)
        {
            var item = cycleCount.Items.FirstOrDefault(i => i.Id == countSubmission.ItemId);
            if (item != null)
            {
                item.CountedQuantity = countSubmission.CountedQuantity;
                item.CountedBy = countedBy;
                item.CountedAtUtc = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(countSubmission.Notes))
                {
                    item.Notes = countSubmission.Notes.Trim();
                }
            }
        }

        cycleCount.TotalItemsCounted = cycleCount.Items.Count(i => i.CountedQuantity.HasValue);
        cycleCount.Status = CycleCountStatus.InProgress;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Recorded {Count} item counts for Cycle Count {CountNumber}", dto.Counts.Count, cycleCount.CountNumber);

        return (await GetCycleCountByIdAsync(cycleCount.Id, cancellationToken))!;
    }

    public async Task<CycleCountDto> SubmitForReviewAsync(int cycleCountId, CancellationToken cancellationToken = default)
    {
        var cycleCount = await _context.CycleCounts
            .Include(cc => cc.Items)
            .FirstOrDefaultAsync(cc => cc.Id == cycleCountId, cancellationToken);

        if (cycleCount == null)
        {
            throw new KeyNotFoundException($"Cycle count session with ID {cycleCountId} was not found.");
        }

        var uncounted = cycleCount.Items.Count(i => !i.CountedQuantity.HasValue);
        if (uncounted > 0)
        {
            throw new InvalidOperationException($"Cannot submit for review: {uncounted} line items have not been counted yet.");
        }

        cycleCount.TotalVarianceUnits = cycleCount.Items.Sum(i => i.VarianceQuantity);
        cycleCount.TotalVarianceCost = cycleCount.Items.Sum(i => i.VarianceCost);
        cycleCount.Status = CycleCountStatus.UnderReview;
        cycleCount.CompletedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Cycle Count {CountNumber} submitted for review (Net Variance: {Units} units, ${Cost})",
            cycleCount.CountNumber, cycleCount.TotalVarianceUnits, cycleCount.TotalVarianceCost);

        return (await GetCycleCountByIdAsync(cycleCount.Id, cancellationToken))!;
    }

    public async Task<CycleCountVarianceReportDto> GetVarianceReportAsync(int cycleCountId, CancellationToken cancellationToken = default)
    {
        var cycleCount = await _context.CycleCounts
            .Include(cc => cc.Warehouse)
            .Include(cc => cc.Items)
            .ThenInclude(cci => cci.Product)
            .ThenInclude(p => p!.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(cc => cc.Id == cycleCountId, cancellationToken);

        if (cycleCount == null)
        {
            throw new KeyNotFoundException($"Cycle count session with ID {cycleCountId} was not found.");
        }

        var discrepancies = cycleCount.Items
            .Where(i => i.CountedQuantity.HasValue && i.VarianceQuantity != 0)
            .Select(MapItemToDto)
            .ToList();

        var totalAudited = cycleCount.Items.Count(i => i.CountedQuantity.HasValue);
        var totalWithDiscrepancy = discrepancies.Count;
        var accurateCount = totalAudited - totalWithDiscrepancy;
        var accuracyRate = totalAudited > 0 ? Math.Round((decimal)accurateCount / totalAudited * 100m, 2) : 100m;

        var netCostVariance = cycleCount.Items.Where(i => i.CountedQuantity.HasValue).Sum(i => i.VarianceCost);
        var absCostVariance = cycleCount.Items.Where(i => i.CountedQuantity.HasValue).Sum(i => Math.Abs(i.VarianceCost));

        return new CycleCountVarianceReportDto
        {
            CycleCountId = cycleCount.Id,
            CountNumber = cycleCount.CountNumber,
            WarehouseCode = cycleCount.Warehouse?.Code ?? string.Empty,
            TotalLinesAudited = totalAudited,
            TotalLinesWithDiscrepancy = totalWithDiscrepancy,
            NetUnitVariance = cycleCount.Items.Where(i => i.CountedQuantity.HasValue).Sum(i => i.VarianceQuantity),
            NetCostVariance = Math.Round(netCostVariance, 2),
            AbsoluteCostVariance = Math.Round(absCostVariance, 2),
            InventoryAccuracyRate = accuracyRate,
            Discrepancies = discrepancies
        };
    }

    public async Task<CycleCountDto> ReconcileCycleCountAsync(int cycleCountId, ReconcileCycleCountDto dto, CancellationToken cancellationToken = default)
    {
        var cycleCount = await _context.CycleCounts
            .Include(cc => cc.Warehouse)
            .Include(cc => cc.Items)
            .ThenInclude(cci => cci.Product)
            .FirstOrDefaultAsync(cc => cc.Id == cycleCountId, cancellationToken);

        if (cycleCount == null)
        {
            throw new KeyNotFoundException($"Cycle count session with ID {cycleCountId} was not found.");
        }

        if (cycleCount.Status != CycleCountStatus.UnderReview && cycleCount.Status != CycleCountStatus.InProgress)
        {
            throw new InvalidOperationException($"Cannot reconcile cycle count session in status '{cycleCount.Status}'.");
        }

        foreach (var item in cycleCount.Items)
        {
            if (item.CountedQuantity.HasValue && item.VarianceQuantity != 0 && !item.IsReconciled)
            {
                var whStock = await _context.WarehouseStocks
                    .FirstOrDefaultAsync(ws => ws.WarehouseId == cycleCount.WarehouseId && ws.ProductId == item.ProductId, cancellationToken);

                if (whStock != null)
                {
                    whStock.QuantityOnHand += item.VarianceQuantity;
                    whStock.UpdatedAtUtc = DateTime.UtcNow;
                }

                var product = await _context.Products.FindAsync(new object[] { item.ProductId }, cancellationToken);
                if (product != null)
                {
                    product.QuantityInStock += item.VarianceQuantity;
                }

                var transaction = new InventoryTransaction
                {
                    ProductId = item.ProductId,
                    Type = TransactionType.Adjustment,
                    QuantityChange = item.VarianceQuantity,
                    QuantityAfter = product?.QuantityInStock ?? 0,
                    UnitCost = item.UnitCost,
                    ReferenceNumber = $"CC-RECON-{cycleCount.CountNumber}",
                    Reason = $"Cycle Count Reconciliation: {item.Notes ?? "Physical count discrepancy"}",
                    TimestampUtc = DateTime.UtcNow
                };
                await _context.InventoryTransactions.AddAsync(transaction, cancellationToken);

                item.IsReconciled = true;
            }
        }

        cycleCount.TotalVarianceUnits = cycleCount.Items.Sum(i => i.VarianceQuantity);
        cycleCount.TotalVarianceCost = cycleCount.Items.Sum(i => i.VarianceCost);
        cycleCount.Status = CycleCountStatus.Reconciled;
        cycleCount.ReviewedBy = dto.ApprovedBy.Trim();
        cycleCount.ReconciledAtUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(dto.Notes))
        {
            cycleCount.Notes = string.IsNullOrWhiteSpace(cycleCount.Notes)
                ? dto.Notes.Trim()
                : $"{cycleCount.Notes}; Recon Notes: {dto.Notes.Trim()}";
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _webhookService.PublishEventAsync(WebhookEventType.StockAdjusted, new
        {
            CycleCountId = cycleCount.Id,
            CountNumber = cycleCount.CountNumber,
            WarehouseId = cycleCount.WarehouseId,
            WarehouseCode = cycleCount.Warehouse?.Code,
            TotalVarianceUnits = cycleCount.TotalVarianceUnits,
            TotalVarianceCost = cycleCount.TotalVarianceCost,
            ApprovedBy = dto.ApprovedBy
        }, cancellationToken);

        _logger.LogInformation("Reconciled Cycle Count {CountNumber} by {ApprovedBy}. Total net variance: {Units} units (${Cost})",
            cycleCount.CountNumber, dto.ApprovedBy, cycleCount.TotalVarianceUnits, cycleCount.TotalVarianceCost);

        return (await GetCycleCountByIdAsync(cycleCount.Id, cancellationToken))!;
    }

    public async Task<CycleCountDto> CancelCycleCountAsync(int cycleCountId, string reason, CancellationToken cancellationToken = default)
    {
        var cycleCount = await _context.CycleCounts.FindAsync(new object[] { cycleCountId }, cancellationToken);
        if (cycleCount == null)
        {
            throw new KeyNotFoundException($"Cycle count session with ID {cycleCountId} was not found.");
        }

        if (cycleCount.Status == CycleCountStatus.Reconciled)
        {
            throw new InvalidOperationException("Cannot cancel an already reconciled cycle count session.");
        }

        cycleCount.Status = CycleCountStatus.Cancelled;
        cycleCount.Notes = string.IsNullOrWhiteSpace(cycleCount.Notes)
            ? $"Cancelled: {reason}"
            : $"{cycleCount.Notes}; Cancelled: {reason}";

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Cancelled Cycle Count {CountNumber} (Reason: {Reason})", cycleCount.CountNumber, reason);

        return (await GetCycleCountByIdAsync(cycleCount.Id, cancellationToken))!;
    }

    private static CycleCountDto MapToDto(CycleCount cc) => new()
    {
        Id = cc.Id,
        CountNumber = cc.CountNumber,
        WarehouseId = cc.WarehouseId,
        WarehouseCode = cc.Warehouse?.Code ?? string.Empty,
        WarehouseName = cc.Warehouse?.Name ?? string.Empty,
        Status = cc.Status,
        Scope = cc.Scope,
        InitiatedBy = cc.InitiatedBy,
        ReviewedBy = cc.ReviewedBy,
        TotalItemsCounted = cc.TotalItemsCounted,
        TotalVarianceUnits = cc.TotalVarianceUnits,
        TotalVarianceCost = cc.TotalVarianceCost,
        CreatedAtUtc = cc.CreatedAtUtc,
        CompletedAtUtc = cc.CompletedAtUtc,
        ReconciledAtUtc = cc.ReconciledAtUtc,
        Notes = cc.Notes,
        Items = cc.Items.Select(MapItemToDto).ToList()
    };

    private static CycleCountItemDto MapItemToDto(CycleCountItem i) => new()
    {
        Id = i.Id,
        CycleCountId = i.CycleCountId,
        ProductId = i.ProductId,
        ProductSku = i.Product?.Sku ?? string.Empty,
        ProductName = i.Product?.Name ?? string.Empty,
        CategoryName = i.Product?.Category?.Name ?? string.Empty,
        BinLocation = "A-01",
        SystemQuantity = i.SystemQuantity,
        CountedQuantity = i.CountedQuantity,
        VarianceQuantity = i.VarianceQuantity,
        UnitCost = i.UnitCost,
        VarianceCost = i.VarianceCost,
        CountedBy = i.CountedBy,
        CountedAtUtc = i.CountedAtUtc,
        IsReconciled = i.IsReconciled,
        Notes = i.Notes
    };
}
