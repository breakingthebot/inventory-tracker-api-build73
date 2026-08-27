// src/InventoryTracker.Api/DTOs/BulkDtos.cs
// Data Transfer Objects for streaming CSV catalog import, row-level validation errors, and bulk export.
// Connects to: src/InventoryTracker.Api/Services/IBulkDataService.cs, src/InventoryTracker.Api/Controllers/BulkController.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.DTOs;

/// <summary>
/// Execution summary returned after processing a CSV bulk catalog import.
/// </summary>
public class BulkImportResultDto
{
    public int TotalRowsRead { get; set; }
    public int RowsInserted { get; set; }
    public int RowsUpdated { get; set; }
    public int RowsFailed { get; set; }
    public bool HasErrors => RowsFailed > 0;
    public List<BulkImportRowErrorDto> Errors { get; set; } = new();
    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Detailed error record for a specific failed CSV row.
/// </summary>
public class BulkImportRowErrorDto
{
    public int RowNumber { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string RawRowData { get; set; } = string.Empty;
}

/// <summary>
/// Flat model representation for exporting and importing CSV records.
/// </summary>
public class ProductCsvRecord
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public int QuantityInStock { get; set; }
    public int ReorderThreshold { get; set; }
    public int ReorderQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = "pcs";
    public string? PrimarySupplierCode { get; set; }
}
