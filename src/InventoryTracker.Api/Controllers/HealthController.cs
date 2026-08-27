// src/InventoryTracker.Api/Controllers/HealthController.cs
// Provides health check endpoints for container orchestrators, load balancers, and uptime probes.
// Connects to: src/InventoryTracker.Api/Services/IAnalyticsService.cs, src/InventoryTracker.Api/DTOs/AnalyticsDtos.cs
// Created: 2026-08-26

using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryTracker.Api.Controllers;

/// <summary>
/// Health check and diagnostic status controller.
/// </summary>
[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public HealthController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Gets service health, version, uptime, and database connectivity.
    /// </summary>
    /// <returns>System health status payload.</returns>
    [HttpGet]
    [HttpGet("/api/v1/health")]
    [ProducesResponseType(typeof(ApiResponse<HealthStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        var status = await _analyticsService.GetHealthStatusAsync(cancellationToken);
        return Ok(ApiResponse<HealthStatusDto>.Ok(status, "Inventory Tracker service is healthy."));
    }
}
