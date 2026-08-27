// src/InventoryTracker.Api/Services/IAnalyticsService.cs
// Defines service contracts for inventory valuation, category rollups, and health monitoring.
// Connects to: src/InventoryTracker.Api/Services/AnalyticsService.cs, src/InventoryTracker.Api/Controllers/AnalyticsController.cs
// Created: 2026-08-26

using InventoryTracker.Api.DTOs;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service contract for aggregated inventory valuation and category metrics.
/// </summary>
public interface IAnalyticsService
{
    Task<InventorySummaryDto> GetInventorySummaryAsync(CancellationToken cancellationToken = default);
    Task<HealthStatusDto> GetHealthStatusAsync(CancellationToken cancellationToken = default);
}
