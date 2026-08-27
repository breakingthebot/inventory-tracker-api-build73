// src/InventoryTracker.Api/Services/IPurchaseOrderService.cs
// Defines service contracts for purchase order management, auto-reorder analysis, and receiving intake.
// Connects to: src/InventoryTracker.Api/Services/PurchaseOrderService.cs, src/InventoryTracker.Api/Controllers/PurchaseOrdersController.cs
// Created: 2026-08-27

using InventoryTracker.Api.DTOs;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service contract for purchase order replenishment workflows and automated low-stock reorder suggestions.
/// </summary>
public interface IPurchaseOrderService
{
    Task<IReadOnlyList<AutoReorderSuggestionDto>> GetAutoReorderSuggestionsAsync(CancellationToken cancellationToken = default);
    Task<AutoGeneratePoResultDto> AutoGeneratePurchaseOrdersAsync(int defaultDestinationWarehouseId, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto> SubmitPurchaseOrderAsync(int poId, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto> ReceivePurchaseOrderAsync(int poId, ReceivePurchaseOrderDto dto, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto> CancelPurchaseOrderAsync(int poId, string reason, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<PurchaseOrderDto>> GetPurchaseOrdersAsync(PurchaseOrderFilterDto filter, CancellationToken cancellationToken = default);
}
