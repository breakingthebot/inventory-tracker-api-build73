// src/InventoryTracker.Api/Models/SalesOrder.cs
// Domain entity representing a customer sales order header and fulfillment tracking state machine.
// Connects to: src/InventoryTracker.Api/Models/Customer.cs, src/InventoryTracker.Api/Models/Warehouse.cs, src/InventoryTracker.Api/Models/SalesOrderItem.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// Domain entity representing a customer sales order in the fulfillment pipeline.
/// </summary>
public class SalesOrder
{
    /// <summary>
    /// Unique database primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique human-readable sales order number (e.g. SO-2026-0001).
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key referencing the purchasing customer account.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Navigation reference to the customer account.
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// Foreign key referencing the warehouse fulfillment facility.
    /// </summary>
    public int WarehouseId { get; set; }

    /// <summary>
    /// Navigation reference to the fulfillment facility.
    /// </summary>
    public Warehouse? Warehouse { get; set; }

    /// <summary>
    /// Current workflow stage in the fulfillment pipeline.
    /// </summary>
    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Draft;

    /// <summary>
    /// Order line items subtotal in USD.
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Outbound shipping and freight fee in USD.
    /// </summary>
    public decimal ShippingFee { get; set; }

    /// <summary>
    /// Sales tax amount in USD.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Total invoiced order amount (Subtotal + ShippingFee + TaxAmount) in USD.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Shipping carrier name (e.g. FedEx Ground, UPS 2nd Day Air, DHL Express).
    /// </summary>
    public string? ShippingCarrier { get; set; }

    /// <summary>
    /// Carrier tracking identification number.
    /// </summary>
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// Timestamp when sales order was created.
    /// </summary>
    public DateTime OrderDateUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when warehouse inventory stock was reserved/allocated.
    /// </summary>
    public DateTime? AllocatedAtUtc { get; set; }

    /// <summary>
    /// Timestamp when picking from bins was confirmed.
    /// </summary>
    public DateTime? PickedAtUtc { get; set; }

    /// <summary>
    /// Timestamp when packing cartons was completed.
    /// </summary>
    public DateTime? PackedAtUtc { get; set; }

    /// <summary>
    /// Timestamp when carrier shipment was dispatched.
    /// </summary>
    public DateTime? ShippedAtUtc { get; set; }

    /// <summary>
    /// Additional customer delivery notes or special handling instructions.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Collection of line items included in this sales order.
    /// </summary>
    public ICollection<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();
}
