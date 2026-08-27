// src/InventoryTracker.Api/DTOs/AnalyticsDtos.cs
// Data contracts for inventory valuation, stock summaries, and category metrics.
// Connects to: src/InventoryTracker.Api/Services/IAnalyticsService.cs, src/InventoryTracker.Api/Controllers/AnalyticsController.cs
// Created: 2026-08-26

namespace InventoryTracker.Api.DTOs;

/// <summary>
/// Aggregated inventory snapshot metrics.
/// </summary>
public class InventorySummaryDto
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int TotalUnitsInStock { get; set; }
    public decimal TotalInventoryValuation { get; set; }
    public decimal TotalRetailValue { get; set; }
    public decimal PotentialGrossMargin => TotalRetailValue - TotalInventoryValuation;
    public int LowStockProductsCount { get; set; }
    public int OutOfStockProductsCount { get; set; }
    public List<CategorySummaryDto> CategoryBreakdown { get; set; } = new();
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Breakdown metrics per product category.
/// </summary>
public class CategorySummaryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public int TotalUnits { get; set; }
    public decimal Valuation { get; set; }
}

/// <summary>
/// System health and database connectivity status.
/// </summary>
public class HealthStatusDto
{
    public string Status { get; set; } = "Healthy";
    public string Service { get; set; } = "InventoryTracker.Api";
    public string Version { get; set; } = "1.0.0";
    public string Environment { get; set; } = "Development";
    public string DatabaseStatus { get; set; } = "Connected";
    public TimeSpan Uptime { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
