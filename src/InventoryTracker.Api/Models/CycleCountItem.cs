// src/InventoryTracker.Api/Models/CycleCountItem.cs
// Line item record representing the physical count and calculated variance for a specific product.
// Connects to: src/InventoryTracker.Api/Models/CycleCount.cs, src/InventoryTracker.Api/Models/Product.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// Line-level physical inventory audit record capturing system snapshot vs blind count quantity.
/// </summary>
public class CycleCountItem
{
    /// <summary>
    /// Unique database primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key referencing the parent cycle count session.
    /// </summary>
    public int CycleCountId { get; set; }

    /// <summary>
    /// Navigation reference to the parent cycle count session.
    /// </summary>
    public CycleCount? CycleCount { get; set; }

    /// <summary>
    /// Foreign key referencing the product being counted.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Navigation reference to the product entity.
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Snapshot of on-hand inventory quantity in the warehouse at the moment the audit began.
    /// </summary>
    public int SystemQuantity { get; set; }

    /// <summary>
    /// Actual physical quantity counted on the warehouse floor (null until recorded).
    /// </summary>
    public int? CountedQuantity { get; set; }

    /// <summary>
    /// Calculated unit variance (CountedQuantity - SystemQuantity).
    /// </summary>
    public int VarianceQuantity => CountedQuantity.HasValue ? CountedQuantity.Value - SystemQuantity : 0;

    /// <summary>
    /// Unit cost snapshot used for financial valuation calculations.
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Calculated dollar variance cost (VarianceQuantity * UnitCost).
    /// </summary>
    public decimal VarianceCost => VarianceQuantity * UnitCost;

    /// <summary>
    /// Username of the warehouse clerk who entered the count.
    /// </summary>
    public string? CountedBy { get; set; }

    /// <summary>
    /// Timestamp when the count was entered.
    /// </summary>
    public DateTime? CountedAtUtc { get; set; }

    /// <summary>
    /// Indicates whether this line item discrepancy has been reconciled into active inventory.
    /// </summary>
    public bool IsReconciled { get; set; }

    /// <summary>
    /// Line-level notes (e.g. damaged packaging, misplaced bin, missing tags).
    /// </summary>
    public string? Notes { get; set; }
}
