// src/InventoryTracker.Api/Models/ProductLot.cs
// Represents a specific manufacturing batch or lot of a product with expiration and quarantine tracking.
// Connects to: src/InventoryTracker.Api/Models/Product.cs, src/InventoryTracker.Api/Models/Warehouse.cs, src/InventoryTracker.Api/Models/LotStatus.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// Domain entity representing an individual production lot or batch with expiration and location balance.
/// </summary>
public class ProductLot
{
    /// <summary>
    /// Unique database primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key referencing the product.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Navigation reference to the product entity.
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Foreign key referencing the physical warehouse facility.
    /// </summary>
    public int WarehouseId { get; set; }

    /// <summary>
    /// Navigation reference to the physical warehouse facility.
    /// </summary>
    public Warehouse? Warehouse { get; set; }

    /// <summary>
    /// Supplier or internal batch lot reference code (e.g. LOT-2026-08A).
    /// </summary>
    public string LotNumber { get; set; } = string.Empty;

    /// <summary>
    /// Initial quantity received into this lot.
    /// </summary>
    public int QuantityInitial { get; set; }

    /// <summary>
    /// Current physical units remaining in this lot.
    /// </summary>
    public int QuantityOnHand { get; set; }

    /// <summary>
    /// Units allocated to open orders.
    /// </summary>
    public int QuantityReserved { get; set; }

    /// <summary>
    /// Computed uncommitted units available for dispatch.
    /// </summary>
    public int AvailableQuantity => Math.Max(0, QuantityOnHand - QuantityReserved);

    /// <summary>
    /// Production or manufacturing timestamp in UTC.
    /// </summary>
    public DateTime? ManufactureDateUtc { get; set; }

    /// <summary>
    /// Expiration date in UTC.
    /// </summary>
    public DateTime? ExpirationDateUtc { get; set; }

    /// <summary>
    /// Operational status of the lot.
    /// </summary>
    public LotStatus Status { get; set; } = LotStatus.Active;

    /// <summary>
    /// Timestamp when this lot was initially received into inventory.
    /// </summary>
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Lot-specific inspection or vendor notes.
    /// </summary>
    public string? Notes { get; set; }
}
