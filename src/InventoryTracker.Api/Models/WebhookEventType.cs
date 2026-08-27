// src/InventoryTracker.Api/Models/WebhookEventType.cs
// Defines inventory domain event types triggerable for webhook broadcasts.
// Connects to: src/InventoryTracker.Api/Models/WebhookSubscription.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// Domain event categories that trigger outbound webhook notifications.
/// </summary>
public enum WebhookEventType
{
    /// <summary>
    /// Fired when product stock drops at or below reorder threshold.
    /// </summary>
    StockLow = 0,

    /// <summary>
    /// Fired when product stock reaches zero on-hand units.
    /// </summary>
    StockOut = 1,

    /// <summary>
    /// Fired when an inter-warehouse transfer order departs the source facility.
    /// </summary>
    TransferShipped = 2,

    /// <summary>
    /// Fired when an inter-warehouse transfer order is received at destination.
    /// </summary>
    TransferReceived = 3,

    /// <summary>
    /// Fired when a purchase order is completely fulfilled by vendor.
    /// </summary>
    PurchaseOrderFulfilled = 4,

    /// <summary>
    /// Fired when manual inventory variance adjustments occur.
    /// </summary>
    StockAdjusted = 5
}
