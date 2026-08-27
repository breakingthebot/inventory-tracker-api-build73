// src/InventoryTracker.Api/DTOs/BarcodeDtos.cs
// Data Transfer Objects for barcode rendering formats, SVG outputs, and handheld scanner resolutions.
// Connects to: src/InventoryTracker.Api/Services/IBarcodeService.cs, src/InventoryTracker.Api/Controllers/BarcodesController.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.DTOs;

/// <summary>
/// Supported barcode symbologies.
/// </summary>
public enum BarcodeSymbology
{
    Code128 = 0,
    QrCode = 1
}

/// <summary>
/// Data contract returned when requesting a rendered barcode.
/// </summary>
public class BarcodeResponseDto
{
    public string Value { get; set; } = string.Empty;
    public BarcodeSymbology Symbology { get; set; }
    public string SymbologyName => Symbology.ToString();
    public string SvgContent { get; set; } = string.Empty;
    public string ContentType { get; set; } = "image/svg+xml";
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Instant product and warehouse lookup data contract returned when scanning a barcode with a mobile terminal.
/// </summary>
public class ProductScannerDto
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public int TotalStockOnHand { get; set; }
    public int ReorderThreshold { get; set; }
    public bool IsLowStock => TotalStockOnHand <= ReorderThreshold;
    public string UnitOfMeasure { get; set; } = "pcs";
    public string PrimarySupplierName { get; set; } = string.Empty;
    public List<WarehouseBinStockDto> WarehouseLocations { get; set; } = new();
}

/// <summary>
/// Warehouse bin location and stock breakdown for scanner display.
/// </summary>
public class WarehouseBinStockDto
{
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string BinLocation { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; }
    public int AvailableQuantity { get; set; }
}
