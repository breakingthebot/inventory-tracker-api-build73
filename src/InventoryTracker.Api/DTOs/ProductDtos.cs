// src/InventoryTracker.Api/DTOs/ProductDtos.cs
// Data Transfer Objects for product retrieval, creation, updating, and filtering.
// Connects to: src/InventoryTracker.Api/Models/Product.cs, src/InventoryTracker.Api/Controllers/ProductsController.cs
// Created: 2026-08-26

using System.ComponentModel.DataAnnotations;

namespace InventoryTracker.Api.DTOs;

/// <summary>
/// Data contract returned when reading product information.
/// </summary>
public class ProductDto
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public int QuantityInStock { get; set; }
    public int ReorderThreshold { get; set; }
    public int ReorderQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = "pcs";
    public bool IsActive { get; set; }
    public bool IsLowStock => QuantityInStock <= ReorderThreshold;
    public decimal TotalValuation => QuantityInStock * UnitCost;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

/// <summary>
/// Request payload for creating a new product record.
/// </summary>
public class CreateProductDto
{
    [Required(ErrorMessage = "SKU is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "SKU must be between 3 and 50 characters.")]
    public string Sku { get; set; } = string.Empty;

    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 150 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "CategoryId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "CategoryId must be a valid positive identifier.")]
    public int CategoryId { get; set; }

    [Range(0.01, 1000000.00, ErrorMessage = "UnitPrice must be between 0.01 and 1,000,000.00.")]
    public decimal UnitPrice { get; set; }

    [Range(0.00, 1000000.00, ErrorMessage = "UnitCost must be non-negative.")]
    public decimal UnitCost { get; set; }

    [Range(0, 1000000, ErrorMessage = "InitialQuantity cannot be negative.")]
    public int InitialQuantity { get; set; } = 0;

    [Range(0, 100000, ErrorMessage = "ReorderThreshold cannot be negative.")]
    public int ReorderThreshold { get; set; } = 10;

    [Range(1, 100000, ErrorMessage = "ReorderQuantity must be at least 1.")]
    public int ReorderQuantity { get; set; } = 50;

    [StringLength(20, ErrorMessage = "UnitOfMeasure cannot exceed 20 characters.")]
    public string UnitOfMeasure { get; set; } = "pcs";
}

/// <summary>
/// Request payload for updating an existing product record.
/// </summary>
public class UpdateProductDto
{
    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 150 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "CategoryId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "CategoryId must be a valid positive identifier.")]
    public int CategoryId { get; set; }

    [Range(0.01, 1000000.00, ErrorMessage = "UnitPrice must be between 0.01 and 1,000,000.00.")]
    public decimal UnitPrice { get; set; }

    [Range(0.00, 1000000.00, ErrorMessage = "UnitCost must be non-negative.")]
    public decimal UnitCost { get; set; }

    [Range(0, 100000, ErrorMessage = "ReorderThreshold cannot be negative.")]
    public int ReorderThreshold { get; set; }

    [Range(1, 100000, ErrorMessage = "ReorderQuantity must be at least 1.")]
    public int ReorderQuantity { get; set; }

    [StringLength(20, ErrorMessage = "UnitOfMeasure cannot exceed 20 characters.")]
    public string UnitOfMeasure { get; set; } = "pcs";

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Query filter parameters for searching and paginating products.
/// </summary>
public class ProductFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public bool? LowStockOnly { get; set; }
    public bool? InStockOnly { get; set; }
    public string? SortBy { get; set; } = "name";
    public bool SortDescending { get; set; } = false;
}
