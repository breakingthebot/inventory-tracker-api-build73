// src/InventoryTracker.Api/Models/SalesOrderStatus.cs
// Defines lifecycle workflow stages for customer sales orders in the pick-pack-ship pipeline.
// Connects to: src/InventoryTracker.Api/Models/SalesOrder.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// Lifecycle status of a customer sales order in the fulfillment pipeline.
/// </summary>
public enum SalesOrderStatus
{
    /// <summary>
    /// Order created and drafted; inventory has not been reserved yet.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Inventory stock reserved and committed at the fulfillment warehouse facility.
    /// </summary>
    Allocated = 1,

    /// <summary>
    /// Line items physically retrieved from warehouse aisle/rack/shelf bins.
    /// </summary>
    Picked = 2,

    /// <summary>
    /// Items packed into shipping cartons with tracking numbers generated.
    /// </summary>
    Packed = 3,

    /// <summary>
    /// Order handed over to shipping carrier; physical on-hand stock deducted and reserved stock cleared.
    /// </summary>
    Shipped = 4,

    /// <summary>
    /// Carrier confirmed delivery to destination customer address.
    /// </summary>
    Delivered = 5,

    /// <summary>
    /// Order cancelled before shipment; all reserved inventory stock released.
    /// </summary>
    Cancelled = 6
}
