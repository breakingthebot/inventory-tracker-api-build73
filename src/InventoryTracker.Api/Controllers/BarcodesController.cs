// src/InventoryTracker.Api/Controllers/BarcodesController.cs
// REST controller for generating printable Code 128 / QR barcodes and resolving handheld mobile scanner lookups.
// Connects to: src/InventoryTracker.Api/Services/IBarcodeService.cs, src/InventoryTracker.Api/DTOs/BarcodeDtos.cs
// Created: 2026-08-27

using System.Text;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryTracker.Api.Controllers;

/// <summary>
/// Manages barcode generation, vector SVG rendering, and mobile handheld scanner product resolution.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class BarcodesController : ControllerBase
{
    private readonly IBarcodeService _barcodeService;

    public BarcodesController(IBarcodeService barcodeService)
    {
        _barcodeService = barcodeService;
    }

    /// <summary>
    /// Generates a Code 128 linear vector barcode for a product SKU as JSON metadata containing SVG markup.
    /// </summary>
    [HttpGet("sku/{sku}")]
    [ProducesResponseType(typeof(ApiResponse<BarcodeResponseDto>), StatusCodes.Status200OK)]
    public IActionResult GetBarcodeBySku(string sku, [FromQuery] int height = 80, [FromQuery] int barWidth = 2)
    {
        var barcode = _barcodeService.GenerateCode128Barcode(sku, height, barWidth);
        return Ok(ApiResponse<BarcodeResponseDto>.Ok(barcode, "Barcode SVG generated successfully."));
    }

    /// <summary>
    /// Renders a raw printable SVG image file for a linear barcode (direct image stream).
    /// </summary>
    [HttpGet("sku/{sku}/image")]
    [Produces("image/svg+xml")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public IActionResult GetBarcodeImageBySku(string sku, [FromQuery] int height = 80, [FromQuery] int barWidth = 2)
    {
        var barcode = _barcodeService.GenerateCode128Barcode(sku, height, barWidth);
        var bytes = Encoding.UTF8.GetBytes(barcode.SvgContent);
        return File(bytes, "image/svg+xml", $"{sku}-barcode.svg");
    }

    /// <summary>
    /// Generates a 2D QR matrix barcode for mobile scanner terminals.
    /// </summary>
    [HttpGet("qr/{sku}")]
    [ProducesResponseType(typeof(ApiResponse<BarcodeResponseDto>), StatusCodes.Status200OK)]
    public IActionResult GetQrCodeBySku(string sku, [FromQuery] int size = 200)
    {
        var qr = _barcodeService.GenerateQrCode(sku, size);
        return Ok(ApiResponse<BarcodeResponseDto>.Ok(qr, "QR Code SVG generated successfully."));
    }

    /// <summary>
    /// Resolves a barcode scan string into full product details, stock levels, and warehouse bin locations.
    /// </summary>
    [HttpGet("scan/{scannedCode}")]
    [ProducesResponseType(typeof(ApiResponse<ProductScannerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ScanBarcode(string scannedCode, CancellationToken cancellationToken)
    {
        var result = await _barcodeService.ScanBarcodeAsync(scannedCode, cancellationToken);
        if (result == null)
        {
            return NotFound(ApiResponse<object>.Fail($"No product found matching scanned code '{scannedCode}'."));
        }

        return Ok(ApiResponse<ProductScannerDto>.Ok(result, "Scanned product and warehouse location resolved."));
    }
}
