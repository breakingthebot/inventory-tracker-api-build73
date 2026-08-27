// src/InventoryTracker.Api/Services/IBomService.cs
// Defines service contracts for Bill of Materials (BOM) recipes, kit assembly yields, and disassembly operations.
// Connects to: src/InventoryTracker.Api/Services/BomService.cs, src/InventoryTracker.Api/Controllers/BomController.cs
// Created: 2026-08-27

using InventoryTracker.Api.DTOs;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service contract for BOM component recipes, cost roll-ups, max assemblable yield analytics, and assembly execution.
/// </summary>
public interface IBomService
{
    Task<BomComponentDto> AddBomComponentAsync(CreateBomComponentDto dto, CancellationToken cancellationToken = default);
    Task<bool> RemoveBomComponentAsync(int parentProductId, int componentProductId, CancellationToken cancellationToken = default);
    Task<ProductBomDetailsDto> GetProductBomAsync(int parentProductId, int? warehouseId = null, CancellationToken cancellationToken = default);
    Task<AssembleKitResultDto> AssembleKitAsync(AssembleKitRequestDto dto, CancellationToken cancellationToken = default);
    Task<DisassembleKitResultDto> DisassembleKitAsync(DisassembleKitRequestDto dto, CancellationToken cancellationToken = default);
}
