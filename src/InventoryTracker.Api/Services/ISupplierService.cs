// src/InventoryTracker.Api/Services/ISupplierService.cs
// Defines service contracts for vendor supplier management.
// Connects to: src/InventoryTracker.Api/Services/SupplierService.cs, src/InventoryTracker.Api/Controllers/SuppliersController.cs
// Created: 2026-08-27

using InventoryTracker.Api.DTOs;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service contract for supplier vendor operations and catalog sourcing queries.
/// </summary>
public interface ISupplierService
{
    Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<SupplierDto?> GetSupplierByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SupplierDto?> GetSupplierByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<SupplierDto> CreateSupplierAsync(CreateSupplierDto dto, CancellationToken cancellationToken = default);
    Task<SupplierDto?> UpdateSupplierAsync(int id, UpdateSupplierDto dto, CancellationToken cancellationToken = default);
}
