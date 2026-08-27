// src/InventoryTracker.Api/Services/IBarcodeService.cs
// Defines service contracts for barcode and QR code generation, label rendering, and handheld scanner resolution.
// Connects to: src/InventoryTracker.Api/Services/BarcodeService.cs, src/InventoryTracker.Api/Controllers/BarcodesController.cs
// Created: 2026-08-27

using InventoryTracker.Api.DTOs;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service contract for generating printable vector barcodes and resolving mobile scanner queries.
/// </summary>
public interface IBarcodeService
{
    BarcodeResponseDto GenerateCode128Barcode(string value, int height = 80, int barWidth = 2);
    BarcodeResponseDto GenerateQrCode(string value, int size = 200);
    Task<ProductScannerDto?> ScanBarcodeAsync(string scannedCode, CancellationToken cancellationToken = default);
}
