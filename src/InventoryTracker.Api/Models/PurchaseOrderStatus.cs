// src/InventoryTracker.Api/Models/PurchaseOrderStatus.cs
// Defines the operational lifecycle states of a supplier purchase order.
// Connects to: src/InventoryTracker.Api/Models/PurchaseOrder.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// Specifies the processing stage of a procurement purchase order.
/// </summary>
public enum PurchaseOrderStatus
{
    /// <summary>
    /// Order drafted or auto-generated; awaiting procurement review.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Order officially submitted and transmitted to the vendor.
    /// </summary>
    Submitted = 1,

    /// <summary>
    /// Partial shipment received at warehouse dock; remaining balance backordered.
    /// </summary>
    PartiallyReceived = 2,

    /// <summary>
    /// All requested order quantities received and inventoried.
    /// </summary>
    Fulfilled = 3,

    /// <summary>
    /// Order voided or cancelled with the vendor.
    /// </summary>
    Cancelled = 4
}
