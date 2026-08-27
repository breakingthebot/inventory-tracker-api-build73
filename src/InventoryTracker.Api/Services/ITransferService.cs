// src/InventoryTracker.Api/Services/ITransferService.cs
// Defines service interface contracts for inter-warehouse stock transfer orchestration.
// Connects to: src/InventoryTracker.Api/Services/TransferService.cs, src/InventoryTracker.Api/Controllers/TransfersController.cs
// Created: 2026-08-27

using InventoryTracker.Api.DTOs;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service contract for creating, shipping, receiving, and cancelling inter-warehouse stock transfers.
/// </summary>
public interface ITransferService
{
    Task<StockTransferDto> CreateTransferAsync(CreateStockTransferDto dto, CancellationToken cancellationToken = default);
    Task<StockTransferDto> ShipTransferAsync(int transferId, ShipTransferDto dto, CancellationToken cancellationToken = default);
    Task<StockTransferDto> ReceiveTransferAsync(int transferId, ReceiveTransferDto dto, CancellationToken cancellationToken = default);
    Task<StockTransferDto> CancelTransferAsync(int transferId, string reason, CancellationToken cancellationToken = default);
    Task<StockTransferDto?> GetTransferByIdAsync(int transferId, CancellationToken cancellationToken = default);
    Task<PagedResult<StockTransferDto>> GetTransfersAsync(TransferFilterDto filter, CancellationToken cancellationToken = default);
}
