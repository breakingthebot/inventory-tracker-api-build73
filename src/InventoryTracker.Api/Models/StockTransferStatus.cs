// src/InventoryTracker.Api/Models/StockTransferStatus.cs
// Defines the lifecycle stages of an inter-warehouse stock transfer order.
// Connects to: src/InventoryTracker.Api/Models/StockTransfer.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// Specifies the current execution status of an inter-warehouse stock transfer.
/// </summary>
public enum StockTransferStatus
{
    /// <summary>
    /// Transfer order drafted but not yet submitted.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Transfer submitted and inventory reserved at source warehouse.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Inventory picked, packed, and physically departed source warehouse.
    /// </summary>
    InTransit = 2,

    /// <summary>
    /// Inbound goods verified and received at destination warehouse.
    /// </summary>
    Received = 3,

    /// <summary>
    /// Transfer cancelled before shipment; reservations released.
    /// </summary>
    Cancelled = 4
}
