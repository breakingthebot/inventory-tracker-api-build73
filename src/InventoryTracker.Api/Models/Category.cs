// src/InventoryTracker.Api/Models/Category.cs
// Represents a product category entity in the inventory system.
// Connects to: src/InventoryTracker.Api/Models/Product.cs, src/InventoryTracker.Api/Data/InventoryDbContext.cs
// Created: 2026-08-26

namespace InventoryTracker.Api.Models;

/// <summary>
/// Domain entity representing a product category classification.
/// </summary>
public class Category
{
    /// <summary>
    /// Unique database identifier for the category.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique name of the category (e.g. Electronics, Office Supplies).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of items included in this category.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Timestamp when the category record was established.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation collection of products belonging to this category.
    /// </summary>
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
