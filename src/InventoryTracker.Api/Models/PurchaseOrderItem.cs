// src/InventoryTracker.Api/Models/PurchaseOrderItem.cs
// Represents an individual product line item, contracted quantity, and received progress on a purchase order.
// Connects to: src/InventoryTracker.Api/Models/PurchaseOrder.cs, src/InventoryTracker.Api/Models/Product.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// Domain entity specifying a product item line on a purchase order.
/// </summary>
public class PurchaseOrderItem
{
    /// <summary>
    /// Unique database primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key referencing the parent purchase order.
    /// </summary>
    public int PurchaseOrderId { get; set; }

    /// <summary>
    /// Navigation reference to the purchase order.
    /// </summary>
    public PurchaseOrder? PurchaseOrder { get; set; }

    /// <summary>
    /// Foreign key referencing the ordered product.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Navigation reference to the product entity.
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Total units requested from the supplier.
    /// </summary>
    public int QuantityOrdered { get; set; }

    /// <summary>
    /// Total units physically received at the warehouse dock to date.
    /// </summary>
    public int QuantityReceived { get; set; }

    /// <summary>
    /// Contracted vendor unit cost in USD.
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Computed remaining backorder quantity yet to be received.
    /// </summary>
    public int RemainingQuantity => Math.Max(0, QuantityOrdered - QuantityReceived);
}
