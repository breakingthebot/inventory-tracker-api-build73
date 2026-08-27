// src/InventoryTracker.Api/Services/IInventoryService.cs
// Defines service interface contracts for stock movement, restock, dispatch, and adjustment operations.
// Connects to: src/InventoryTracker.Api/Services/InventoryService.cs, src/InventoryTracker.Api/Controllers/InventoryController.cs
// Created: 2026-08-26

using InventoryTracker.Api.DTOs;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service contract for stock adjustments, restock intake, dispatch fulfillment, and audit queries.
/// </summary>
public interface IInventoryService
{
    Task<TransactionDto> AdjustStockAsync(StockAdjustmentDto dto, CancellationToken cancellationToken = default);
    Task<TransactionDto> RestockAsync(RestockRequestDto dto, CancellationToken cancellationToken = default);
    Task<TransactionDto> DispatchAsync(DispatchRequestDto dto, CancellationToken cancellationToken = default);
    Task<PagedResult<TransactionDto>> GetTransactionsAsync(TransactionFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TransactionDto>> GetProductTransactionsAsync(int productId, int limit = 50, CancellationToken cancellationToken = default);
}
