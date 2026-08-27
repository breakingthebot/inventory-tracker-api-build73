// src/InventoryTracker.Api/Services/ILotTrackingService.cs
// Defines service contracts for batch lot tracking, FEFO dispatch planning, and expiration monitoring.
// Connects to: src/InventoryTracker.Api/Services/LotTrackingService.cs, src/InventoryTracker.Api/Controllers/LotsController.cs
// Created: 2026-08-27

using InventoryTracker.Api.DTOs;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service contract for batch lot tracking, expiration monitoring, and FEFO inventory allocation.
/// </summary>
public interface ILotTrackingService
{
    Task<ProductLotDto> CreateLotAsync(CreateProductLotDto dto, CancellationToken cancellationToken = default);
    Task<ProductLotDto?> GetLotByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<ProductLotDto>> GetLotsAsync(LotFilterDto filter, CancellationToken cancellationToken = default);
    Task<ProductLotDto?> UpdateLotAsync(int id, UpdateProductLotDto dto, CancellationToken cancellationToken = default);
    Task<ExpiringLotsSummaryDto> GetExpiringLotsAsync(int daysThreshold = 30, CancellationToken cancellationToken = default);
    Task<FefoAllocationPlanDto> GetFefoAllocationPlanAsync(int productId, int requestedQuantity, int warehouseId, CancellationToken cancellationToken = default);
    Task<DispatchFefoResultDto> DispatchFefoAsync(DispatchFefoRequestDto dto, CancellationToken cancellationToken = default);
}
