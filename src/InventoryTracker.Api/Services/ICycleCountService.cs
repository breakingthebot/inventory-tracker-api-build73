// src/InventoryTracker.Api/Services/ICycleCountService.cs
// Defines service contracts for inventory cycle counting, blind counts, variance analysis, and supervisor reconciliation.
// Connects to: src/InventoryTracker.Api/Services/CycleCountService.cs, src/InventoryTracker.Api/Controllers/CycleCountsController.cs
// Created: 2026-08-27

using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service contract for cycle counting sessions, blind count entry, variance analytics, and reconciliation.
/// </summary>
public interface ICycleCountService
{
    Task<CycleCountDto> CreateCycleCountAsync(CreateCycleCountDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CycleCountDto>> GetCycleCountsAsync(CycleCountStatus? status = null, int? warehouseId = null, CancellationToken cancellationToken = default);
    Task<CycleCountDto?> GetCycleCountByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CycleCountDto> RecordBatchCountAsync(int cycleCountId, RecordBatchCountDto dto, CancellationToken cancellationToken = default);
    Task<CycleCountDto> SubmitForReviewAsync(int cycleCountId, CancellationToken cancellationToken = default);
    Task<CycleCountVarianceReportDto> GetVarianceReportAsync(int cycleCountId, CancellationToken cancellationToken = default);
    Task<CycleCountDto> ReconcileCycleCountAsync(int cycleCountId, ReconcileCycleCountDto dto, CancellationToken cancellationToken = default);
    Task<CycleCountDto> CancelCycleCountAsync(int cycleCountId, string reason, CancellationToken cancellationToken = default);
}
