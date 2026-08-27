// src/InventoryTracker.Api/Services/AnalyticsService.cs
// Implementation of aggregated inventory analytics, valuation computation, and health reporting.
// Connects to: src/InventoryTracker.Api/Data/InventoryDbContext.cs, src/InventoryTracker.Api/DTOs/AnalyticsDtos.cs
// Created: 2026-08-26

using System.Diagnostics;
using InventoryTracker.Api.Data;
using InventoryTracker.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service calculating inventory financial valuation, category rollups, and system health status.
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private static readonly DateTime _startTimeUtc = DateTime.UtcNow;
    private readonly InventoryDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public AnalyticsService(InventoryDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<InventorySummaryDto> GetInventorySummaryAsync(CancellationToken cancellationToken = default)
    {
        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .ToListAsync(cancellationToken);

        var totalProducts = products.Count;
        var activeProducts = products.Count(p => p.IsActive);
        var totalUnitsInStock = products.Sum(p => p.QuantityInStock);
        var totalValuation = products.Sum(p => p.QuantityInStock * p.UnitCost);
        var totalRetailValue = products.Sum(p => p.QuantityInStock * p.UnitPrice);
        var lowStockCount = products.Count(p => p.IsActive && p.QuantityInStock <= p.ReorderThreshold && p.QuantityInStock > 0);
        var outOfStockCount = products.Count(p => p.IsActive && p.QuantityInStock == 0);

        var categoryBreakdown = products
            .GroupBy(p => new { p.CategoryId, Name = p.Category?.Name ?? "Uncategorized" })
            .Select(g => new CategorySummaryDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                ProductCount = g.Count(),
                TotalUnits = g.Sum(p => p.QuantityInStock),
                Valuation = g.Sum(p => p.QuantityInStock * p.UnitCost)
            })
            .OrderByDescending(c => c.Valuation)
            .ToList();

        return new InventorySummaryDto
        {
            TotalProducts = totalProducts,
            ActiveProducts = activeProducts,
            TotalUnitsInStock = totalUnitsInStock,
            TotalInventoryValuation = Math.Round(totalValuation, 2),
            TotalRetailValue = Math.Round(totalRetailValue, 2),
            LowStockProductsCount = lowStockCount,
            OutOfStockProductsCount = outOfStockCount,
            CategoryBreakdown = categoryBreakdown,
            GeneratedAtUtc = DateTime.UtcNow
        };
    }

    public async Task<HealthStatusDto> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        var dbStatus = "Connected";
        try
        {
            await _context.Database.CanConnectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            dbStatus = $"Error: {ex.Message}";
        }

        return new HealthStatusDto
        {
            Status = dbStatus == "Connected" ? "Healthy" : "Degraded",
            Service = "InventoryTracker.Api",
            Version = "1.0.0",
            Environment = _environment.EnvironmentName,
            DatabaseStatus = dbStatus,
            Uptime = DateTime.UtcNow - _startTimeUtc,
            TimestampUtc = DateTime.UtcNow
        };
    }
}
