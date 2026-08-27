// src/InventoryTracker.Api/Controllers/BulkController.cs
// REST controller for streaming CSV catalog import, validation error reporting, and export downloads.
// Connects to: src/InventoryTracker.Api/Services/IBulkDataService.cs, src/InventoryTracker.Api/DTOs/BulkDtos.cs
// Created: 2026-08-27

using System.Text;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryTracker.Api.Controllers;

/// <summary>
/// Manages bulk catalog spreadsheet imports, row validation, and CSV export downloads.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class BulkController : ControllerBase
{
    private readonly IBulkDataService _bulkService;

    public BulkController(IBulkDataService bulkService)
    {
        _bulkService = bulkService;
    }

    /// <summary>
    /// Imports a CSV spreadsheet of products, executing row-by-row validation and batch upserting.
    /// Accepts raw CSV text in request body or uploaded file.
    /// </summary>
    [HttpPost("import/products")]
    [Consumes("text/plain", "text/csv", "application/octet-stream")]
    [ProducesResponseType(typeof(ApiResponse<BulkImportResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportProducts(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var csvContent = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(csvContent))
        {
            return BadRequest(ApiResponse<object>.Fail("Request body must contain non-empty CSV text."));
        }

        var result = await _bulkService.ImportProductsFromCsvAsync(csvContent, cancellationToken);
        var message = $"CSV Import finished: {result.RowsInserted} inserted, {result.RowsUpdated} updated, {result.RowsFailed} failed.";
        return Ok(ApiResponse<BulkImportResultDto>.Ok(result, message));
    }

    /// <summary>
    /// Exports all product catalog items as a downloadable CSV spreadsheet.
    /// </summary>
    [HttpGet("export/products")]
    [Produces("text/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportProducts(CancellationToken cancellationToken)
    {
        var csv = await _bulkService.ExportProductsToCsvAsync(cancellationToken);
        var bytes = Encoding.UTF8.GetBytes(csv);
        var filename = $"inventory-catalog-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        return File(bytes, "text/csv", filename);
    }

    /// <summary>
    /// Downloads a blank starter CSV template with example rows for catalog onboarding.
    /// </summary>
    [HttpGet("export/template")]
    [Produces("text/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public IActionResult DownloadTemplate()
    {
        var csv = _bulkService.GetProductCsvTemplate();
        var bytes = Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", "inventory-import-template.csv");
    }
}
