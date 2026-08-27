// src/InventoryTracker.Api/Models/BillOfMaterials.cs
// Defines composite product recipe mappings linking a parent kit product to sub-component items.
// Connects to: src/InventoryTracker.Api/Models/Product.cs, src/InventoryTracker.Api/Data/InventoryDbContext.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// Domain entity defining an individual raw material or sub-component requirement within a product BOM recipe.
/// </summary>
public class BillOfMaterials
{
    /// <summary>
    /// Unique database primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key referencing the parent assembled bundle or finished kit.
    /// </summary>
    public int ParentProductId { get; set; }

    /// <summary>
    /// Navigation reference to the parent assembled product.
    /// </summary>
    public Product? ParentProduct { get; set; }

    /// <summary>
    /// Foreign key referencing the raw material or sub-component product.
    /// </summary>
    public int ComponentProductId { get; set; }

    /// <summary>
    /// Navigation reference to the sub-component product entity.
    /// </summary>
    public Product? ComponentProduct { get; set; }

    /// <summary>
    /// Multiplier quantity of this component consumed to build 1 unit of the parent product.
    /// </summary>
    public int QuantityRequired { get; set; } = 1;

    /// <summary>
    /// Expected production scrap allowance percentage (0.00 to 100.00).
    /// </summary>
    public decimal ScrapPercentage { get; set; } = 0m;

    /// <summary>
    /// Assembly instructions or component notes.
    /// </summary>
    public string? Notes { get; set; }
}
