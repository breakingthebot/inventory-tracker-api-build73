// src/InventoryTracker.Api/Services/IBulkDataService.cs
// Defines service contracts for CSV catalog import, row-by-row batch upserts, and catalog exports.
// Connects to: src/InventoryTracker.Api/Services/BulkDataService.cs, src/InventoryTracker.Api/Controllers/BulkController.cs
// Created: 2026-08-27

using InventoryTracker.Api.DTOs;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service contract for bulk spreadsheet data operations and catalog import/export.
/// </summary>
public interface IBulkDataService
{
    Task<BulkImportResultDto> ImportProductsFromCsvAsync(string csvContent, CancellationToken cancellationToken = default);
    Task<string> ExportProductsToCsvAsync(CancellationToken cancellationToken = default);
    string GetProductCsvTemplate();
}
