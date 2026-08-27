// src/InventoryTracker.Api/Controllers/CycleCountsController.cs
// REST controller for cycle count session management, physical blind counts, variance reports, and supervisor reconciliation.
// Connects to: src/InventoryTracker.Api/Services/ICycleCountService.cs, src/InventoryTracker.Api/DTOs/CycleCountDtos.cs
// Created: 2026-08-27

using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryTracker.Api.Controllers;

/// <summary>
/// Manages physical inventory cycle counting audits, blind count submissions, variance analytics, and reconciliation approvals.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class CycleCountsController : ControllerBase
{
    private readonly ICycleCountService _cycleCountService;

    public CycleCountsController(ICycleCountService cycleCountService)
    {
        _cycleCountService = cycleCountService;
    }

    /// <summary>
    /// Retrieves all cycle count audit sessions filtered by workflow status or warehouse facility.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CycleCountDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCycleCounts([FromQuery] CycleCountStatus? status, [FromQuery] int? warehouseId, CancellationToken cancellationToken)
    {
        var list = await _cycleCountService.GetCycleCountsAsync(status, warehouseId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CycleCountDto>>.Ok(list, $"Retrieved {list.Count} cycle count audit sessions."));
    }

    /// <summary>
    /// Retrieves full details and line items of a specific cycle count session.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CycleCountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCycleCountById(int id, CancellationToken cancellationToken)
    {
        var session = await _cycleCountService.GetCycleCountByIdAsync(id, cancellationToken);
        if (session == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Cycle count session with ID {id} was not found."));
        }

        return Ok(ApiResponse<CycleCountDto>.Ok(session));
    }

    /// <summary>
    /// Initiates a new physical cycle count session, snapshotting current system stock levels for matching items.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CycleCountDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCycleCount([FromBody] CreateCycleCountDto dto, CancellationToken cancellationToken)
    {
        var created = await _cycleCountService.CreateCycleCountAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetCycleCountById), new { id = created.Id },
            ApiResponse<CycleCountDto>.Ok(created, "Cycle count audit session initiated successfully."));
    }

    /// <summary>
    /// Records physical blind counts for multiple line items in bulk.
    /// </summary>
    [HttpPost("{id:int}/record-counts")]
    [ProducesResponseType(typeof(ApiResponse<CycleCountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordBatchCount(int id, [FromBody] RecordBatchCountDto dto, CancellationToken cancellationToken)
    {
        var updated = await _cycleCountService.RecordBatchCountAsync(id, dto, cancellationToken);
        return Ok(ApiResponse<CycleCountDto>.Ok(updated, "Physical counts recorded successfully."));
    }

    /// <summary>
    /// Submits completed physical counts for warehouse supervisor review.
    /// </summary>
    [HttpPost("{id:int}/submit-review")]
    [ProducesResponseType(typeof(ApiResponse<CycleCountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitForReview(int id, CancellationToken cancellationToken)
    {
        var updated = await _cycleCountService.SubmitForReviewAsync(id, cancellationToken);
        return Ok(ApiResponse<CycleCountDto>.Ok(updated, "Cycle count submitted for supervisor review."));
    }

    /// <summary>
    /// Generates a comprehensive variance report comparing counted vs system stock quantities and financial impacts.
    /// </summary>
    [HttpGet("{id:int}/variance-report")]
    [ProducesResponseType(typeof(ApiResponse<CycleCountVarianceReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVarianceReport(int id, CancellationToken cancellationToken)
    {
        var report = await _cycleCountService.GetVarianceReportAsync(id, cancellationToken);
        return Ok(ApiResponse<CycleCountVarianceReportDto>.Ok(report));
    }

    /// <summary>
    /// Approves count discrepancies and automatically applies balancing ledger adjustments to inventory.
    /// </summary>
    [HttpPost("{id:int}/reconcile")]
    [ProducesResponseType(typeof(ApiResponse<CycleCountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReconcileCycleCount(int id, [FromBody] ReconcileCycleCountDto dto, CancellationToken cancellationToken)
    {
        var reconciled = await _cycleCountService.ReconcileCycleCountAsync(id, dto, cancellationToken);
        return Ok(ApiResponse<CycleCountDto>.Ok(reconciled, "Cycle count discrepancies reconciled into active inventory."));
    }

    /// <summary>
    /// Voids an open cycle count audit session without adjusting inventory.
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<CycleCountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelCycleCount(int id, [FromQuery] string reason = "Voided by operator", CancellationToken cancellationToken = default)
    {
        var cancelled = await _cycleCountService.CancelCycleCountAsync(id, reason, cancellationToken);
        return Ok(ApiResponse<CycleCountDto>.Ok(cancelled, "Cycle count audit session cancelled."));
    }
}
