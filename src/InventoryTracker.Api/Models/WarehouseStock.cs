// src/InventoryTracker.Api/Models/WarehouseStock.cs
// Represents inventory stock quantity and bin location for a specific product within a specific warehouse.
// Connects to: src/InventoryTracker.Api/Models/Warehouse.cs, src/InventoryTracker.Api/Models/Product.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// Domain entity mapping product inventory on-hand balances, reservations, and bin locations per warehouse.
/// </summary>
public class WarehouseStock
{
    /// <summary>
    /// Unique database primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key referencing the warehouse facility.
    /// </summary>
    public int WarehouseId { get; set; }

    /// <summary>
    /// Navigation reference to the warehouse.
    /// </summary>
    public Warehouse? Warehouse { get; set; }

    /// <summary>
    /// Foreign key referencing the product.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Navigation reference to the product.
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Total physical on-hand quantity in the warehouse.
    /// </summary>
    public int QuantityOnHand { get; set; }

    /// <summary>
    /// Quantity reserved for pending outbound orders or in-progress transfers.
    /// </summary>
    public int QuantityReserved { get; set; }

    /// <summary>
    /// Quantity available for new orders and allocation (QuantityOnHand - QuantityReserved).
    /// </summary>
    public int AvailableQuantity => Math.Max(0, QuantityOnHand - QuantityReserved);

    /// <summary>
    /// Specific aisle/rack/shelf bin coordinate in the warehouse (e.g. A-03-B2).
    /// </summary>
    public string BinLocation { get; set; } = "UNASSIGNED";

    /// <summary>
    /// Timestamp when this stock record was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
