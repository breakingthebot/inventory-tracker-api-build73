// src/InventoryTracker.Api/Models/SalesOrderItem.cs
// Domain entity representing an individual product line item in a customer sales order.
// Connects to: src/InventoryTracker.Api/Models/SalesOrder.cs, src/InventoryTracker.Api/Models/Product.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// Line item within a customer sales order.
/// </summary>
public class SalesOrderItem
{
    /// <summary>
    /// Unique database primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key referencing the parent sales order.
    /// </summary>
    public int SalesOrderId { get; set; }

    /// <summary>
    /// Navigation reference to the parent sales order.
    /// </summary>
    public SalesOrder? SalesOrder { get; set; }

    /// <summary>
    /// Foreign key referencing the product item.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Navigation reference to the product entity.
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Ordered unit quantity.
    /// </summary>
    public int QuantityOrdered { get; set; }

    /// <summary>
    /// Quantity physically picked from warehouse storage bins.
    /// </summary>
    public int QuantityPicked { get; set; }

    /// <summary>
    /// Unit sale price agreed upon in USD.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Calculated line total price in USD (QuantityOrdered * UnitPrice).
    /// </summary>
    public decimal TotalPrice => Math.Round(QuantityOrdered * UnitPrice, 2);

    /// <summary>
    /// Historical snapshot of unit acquisition cost at the time of order placement in USD.
    /// </summary>
    public decimal UnitCostSnapshot { get; set; }

    /// <summary>
    /// Snapshot of warehouse bin coordinates for pick sheet routing (e.g. A-01-02).
    /// </summary>
    public string? BinLocationSnapshot { get; set; }
}
