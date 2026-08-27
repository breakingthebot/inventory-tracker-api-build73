// src/InventoryTracker.Api/Models/Product.cs
// Represents a trackable inventory item entity with stock balance and reorder rules.
// Connects to: src/InventoryTracker.Api/Models/Category.cs, src/InventoryTracker.Api/Models/WarehouseStock.cs
// Created: 2026-08-26

namespace InventoryTracker.Api.Models;

/// <summary>
/// Domain entity representing a product tracked in inventory.
/// </summary>
public class Product
{
    /// <summary>
    /// Unique database primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Stock Keeping Unit (SKU) — unique alphanumeric business identifier.
    /// </summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>
    /// Descriptive name of the product.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the product and specifications.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Foreign key identifier referencing the product's category.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Navigation reference to the associated category.
    /// </summary>
    public Category? Category { get; set; }

    /// <summary>
    /// Retail sale unit price in USD.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Acquisition cost per unit for valuation calculations in USD.
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Current on-hand quantity available across all warehouses.
    /// </summary>
    public int QuantityInStock { get; set; }

    /// <summary>
    /// Threshold quantity triggering low-stock replenishment warnings.
    /// </summary>
    public int ReorderThreshold { get; set; } = 10;

    /// <summary>
    /// Recommended quantity to order when replenishing stock.
    /// </summary>
    public int ReorderQuantity { get; set; } = 50;

    /// <summary>
    /// Unit of measure (e.g. "pcs", "box", "kg", "pack").
    /// </summary>
    public string UnitOfMeasure { get; set; } = "pcs";

    /// <summary>
    /// Flag indicating whether the product is actively stocked.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Timestamp when the product record was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the product record was last modified.
    /// </summary>
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>
    /// Navigation collection of historical stock movements for this product.
    /// </summary>
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();

    /// <summary>
    /// Navigation collection of stock distributions across physical warehouses.
    /// </summary>
    public ICollection<WarehouseStock> WarehouseStocks { get; set; } = new List<WarehouseStock>();

    /// <summary>
    /// Navigation collection of transfer order items involving this product.
    /// </summary>
    public ICollection<StockTransferItem> TransferItems { get; set; } = new List<StockTransferItem>();
}
