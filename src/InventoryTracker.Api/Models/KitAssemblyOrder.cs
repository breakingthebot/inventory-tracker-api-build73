// src/InventoryTracker.Api/Models/KitAssemblyOrder.cs
// Audit entity logging the execution of kit assembly operations and associated labor/material costs.
// Connects to: src/InventoryTracker.Api/Models/Product.cs, src/InventoryTracker.Api/Models/Warehouse.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// Domain entity logging an executed kit assembly batch order.
/// </summary>
public class KitAssemblyOrder
{
    /// <summary>
    /// Unique database primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique assembly batch tracking number (e.g. ASM-2026-001).
    /// </summary>
    public string AssemblyNumber { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key referencing the assembled parent kit product.
    /// </summary>
    public int KitProductId { get; set; }

    /// <summary>
    /// Navigation reference to the assembled parent kit product.
    /// </summary>
    public Product? KitProduct { get; set; }

    /// <summary>
    /// Foreign key referencing the physical warehouse facility where assembly occurred.
    /// </summary>
    public int WarehouseId { get; set; }

    /// <summary>
    /// Navigation reference to the physical warehouse facility.
    /// </summary>
    public Warehouse? Warehouse { get; set; }

    /// <summary>
    /// Number of finished parent kits produced in this assembly run.
    /// </summary>
    public int QuantityAssembled { get; set; }

    /// <summary>
    /// Direct labor / overhead cost allocated to this assembly run.
    /// </summary>
    public decimal LaborCost { get; set; }

    /// <summary>
    /// Total computed acquisition cost per finished unit (Components + Labor).
    /// </summary>
    public decimal TotalUnitCost { get; set; }

    /// <summary>
    /// Operator username who executed the assembly.
    /// </summary>
    public string AssembledBy { get; set; } = string.Empty;

    /// <summary>
    /// Assembly completion timestamp in UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional production or quality notes.
    /// </summary>
    public string? Notes { get; set; }
}
