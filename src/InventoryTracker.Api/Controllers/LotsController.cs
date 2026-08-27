// src/InventoryTracker.Api/Controllers/LotsController.cs
// REST controller for batch lot tracking, FEFO dispatching, quarantine controls, and expiration risk reports.
// Connects to: src/InventoryTracker.Api/Services/ILotTrackingService.cs, src/InventoryTracker.Api/DTOs/LotDtos.cs
// Created: 2026-08-27

using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryTracker.Api.Controllers;

/// <summary>
/// Manages product batch lots, expiration date monitoring, quarantine controls, and FEFO picking workflows.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class LotsController : ControllerBase
{
    private readonly ILotTrackingService _lotService;

    public LotsController(ILotTrackingService lotService)
    {
        _lotService = lotService;
    }

    /// <summary>
    /// Retrieves a paginated list of product lots filtered by product, warehouse facility, status, or expiration.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProductLotDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLots([FromQuery] LotFilterDto filter, CancellationToken cancellationToken)
    {
        var result = await _lotService.GetLotsAsync(filter, cancellationToken);
        return Ok(ApiResponse<PagedResult<ProductLotDto>>.Ok(result));
    }

    /// <summary>
    /// Retrieves details of a specific batch lot by its database ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProductLotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLotById(int id, CancellationToken cancellationToken)
    {
        var lot = await _lotService.GetLotByIdAsync(id, cancellationToken);
        if (lot == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Product lot with ID {id} was not found."));
        }

        return Ok(ApiResponse<ProductLotDto>.Ok(lot));
    }

    /// <summary>
    /// Registers a new batch lot and receives opening physical stock into the warehouse.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProductLotDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLot([FromBody] CreateProductLotDto dto, CancellationToken cancellationToken)
    {
        var created = await _lotService.CreateLotAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetLotById), new { id = created.Id },
            ApiResponse<ProductLotDto>.Ok(created, "Product lot registered and received into warehouse stock."));
    }

    /// <summary>
    /// Updates a lot's operational status (e.g. Quarantine, Active, Expired) or expiration date.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProductLotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLot(int id, [FromBody] UpdateProductLotDto dto, CancellationToken cancellationToken)
    {
        var updated = await _lotService.UpdateLotAsync(id, dto, cancellationToken);
        if (updated == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Product lot with ID {id} was not found."));
        }

        return Ok(ApiResponse<ProductLotDto>.Ok(updated, "Product lot updated successfully."));
    }

    /// <summary>
    /// Generates an expiration risk report identifying all active lots expiring within a specified day threshold.
    /// </summary>
    [HttpGet("expiring")]
    [ProducesResponseType(typeof(ApiResponse<ExpiringLotsSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpiringLots([FromQuery] int daysThreshold = 30, CancellationToken cancellationToken = default)
    {
        var summary = await _lotService.GetExpiringLotsAsync(daysThreshold, cancellationToken);
        return Ok(ApiResponse<ExpiringLotsSummaryDto>.Ok(summary, $"Found {summary.TotalExpiringLotsCount} lots expiring within {daysThreshold} days."));
    }

    /// <summary>
    /// Computes a First-Expired, First-Out (FEFO) allocation plan without executing stock movement.
    /// </summary>
    [HttpGet("fefo-plan")]
    [ProducesResponseType(typeof(ApiResponse<FefoAllocationPlanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFefoPlan([FromQuery] int productId, [FromQuery] int quantity, [FromQuery] int warehouseId, CancellationToken cancellationToken)
    {
        if (productId <= 0 || quantity <= 0 || warehouseId <= 0)
        {
            return BadRequest(ApiResponse<object>.Fail("productId, quantity, and warehouseId must all be positive integers."));
        }

        var plan = await _lotService.GetFefoAllocationPlanAsync(productId, quantity, warehouseId, cancellationToken);
        return Ok(ApiResponse<FefoAllocationPlanDto>.Ok(plan, "FEFO allocation plan computed successfully."));
    }

    /// <summary>
    /// Executes a FEFO batch dispatch deducting inventory from oldest / soonest-expiring lots.
    /// </summary>
    [HttpPost("dispatch-fefo")]
    [ProducesResponseType(typeof(ApiResponse<DispatchFefoResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DispatchFefo([FromBody] DispatchFefoRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _lotService.DispatchFefoAsync(dto, cancellationToken);
        return Ok(ApiResponse<DispatchFefoResultDto>.Ok(result, "FEFO dispatch completed successfully."));
    }
}
