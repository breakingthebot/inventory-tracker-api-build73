// src/InventoryTracker.Api/Services/IWarehouseService.cs
// Defines service contracts for physical warehouse facility management and location stock balances.
// Connects to: src/InventoryTracker.Api/Services/WarehouseService.cs, src/InventoryTracker.Api/Controllers/WarehousesController.cs
// Created: 2026-08-27

using InventoryTracker.Api.DTOs;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service contract for warehouse facility operations and stock allocation per facility.
/// </summary>
public interface IWarehouseService
{
    Task<IReadOnlyList<WarehouseDto>> GetWarehousesAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<WarehouseDto?> GetWarehouseByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WarehouseDto?> GetWarehouseByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto dto, CancellationToken cancellationToken = default);
    Task<WarehouseDto?> UpdateWarehouseAsync(int id, UpdateWarehouseDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseStockDto>> GetWarehouseStockAsync(int warehouseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseStockDto>> GetProductWarehouseStockAsync(int productId, CancellationToken cancellationToken = default);
    Task<WarehouseStockDto?> SetBinLocationAsync(int warehouseId, int productId, string binLocation, CancellationToken cancellationToken = default);
}
