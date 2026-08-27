// src/InventoryTracker.Api/Controllers/AnalyticsController.cs
// REST controller providing aggregated inventory analytics, valuation rollups, and category statistics.
// Connects to: src/InventoryTracker.Api/Services/IAnalyticsService.cs, src/InventoryTracker.Api/DTOs/AnalyticsDtos.cs
// Created: 2026-08-26

using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryTracker.Api.Controllers;

/// <summary>
/// Provides high-level business intelligence, inventory valuation, and category breakdown reports.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Calculates real-time total inventory valuation, gross margin potential, and category distributions.
    /// </summary>
    [HttpGet("summary")]
    [HttpGet("/api/v1/inventory/summary")]
    [ProducesResponseType(typeof(ApiResponse<InventorySummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await _analyticsService.GetInventorySummaryAsync(cancellationToken);
        return Ok(ApiResponse<InventorySummaryDto>.Ok(summary, "Inventory valuation summary computed successfully."));
    }
}
