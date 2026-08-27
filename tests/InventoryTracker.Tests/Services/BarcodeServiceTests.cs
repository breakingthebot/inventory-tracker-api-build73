// tests/InventoryTracker.Tests/Services/BarcodeServiceTests.cs
// Unit tests for BarcodeService SVG rendering, Code 128 bit patterns, QR matrix creation, and scanner lookups.
// Connects to: src/InventoryTracker.Api/Services/BarcodeService.cs
// Created: 2026-08-27

using InventoryTracker.Api.Data;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using InventoryTracker.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryTracker.Tests.Services;

public class BarcodeServiceTests
{
    private static InventoryDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new InventoryDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public void GenerateCode128Barcode_ValidSku_ReturnsSvgMarkup()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_Barcode_Svg");
        var loggerMock = new Mock<ILogger<BarcodeService>>();
        var service = new BarcodeService(context, loggerMock.Object);

        // Act
        var result = service.GenerateCode128Barcode("ELEC-MON-4K27");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ELEC-MON-4K27", result.Value);
        Assert.Equal(BarcodeSymbology.Code128, result.Symbology);
        Assert.Contains("<svg", result.SvgContent);
        Assert.Contains("</svg>", result.SvgContent);
        Assert.Contains("ELEC-MON-4K27", result.SvgContent);
    }

    [Fact]
    public void GenerateQrCode_ValidValue_ReturnsValidQrSvg()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_Barcode_Qr");
        var loggerMock = new Mock<ILogger<BarcodeService>>();
        var service = new BarcodeService(context, loggerMock.Object);

        // Act
        var result = service.GenerateQrCode("ELEC-MON-4K27");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(BarcodeSymbology.QrCode, result.Symbology);
        Assert.Contains("<svg", result.SvgContent);
        Assert.Contains("<rect", result.SvgContent);
    }

    [Fact]
    public async Task ScanBarcodeAsync_ExistingProductSku_ReturnsProductScannerDto()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_Barcode_Scan");
        var category = new Category { Name = "Electronics" };
        var warehouse = new Warehouse { Code = "WH-E", Name = "East" };
        context.Categories.Add(category);
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Sku = "SCAN-SKU-99",
            Name = "Scannable Widget",
            CategoryId = category.Id,
            UnitPrice = 99.99m,
            UnitCost = 45.00m,
            QuantityInStock = 25,
            ReorderThreshold = 10,
            IsActive = true
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        context.WarehouseStocks.Add(new WarehouseStock
        {
            WarehouseId = warehouse.Id,
            ProductId = product.Id,
            QuantityOnHand = 25,
            BinLocation = "A-05-12"
        });
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<BarcodeService>>();
        var service = new BarcodeService(context, loggerMock.Object);

        // Act
        var result = await service.ScanBarcodeAsync("scan-sku-99");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SCAN-SKU-99", result.Sku);
        Assert.Equal("Scannable Widget", result.Name);
        Assert.Equal(25, result.TotalStockOnHand);
        Assert.False(result.IsLowStock);
        Assert.Single(result.WarehouseLocations);
        Assert.Equal("A-05-12", result.WarehouseLocations[0].BinLocation);
    }

    [Fact]
    public async Task ScanBarcodeAsync_NonExistentSku_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_Barcode_Scan_NotFound");
        var loggerMock = new Mock<ILogger<BarcodeService>>();
        var service = new BarcodeService(context, loggerMock.Object);

        // Act
        var result = await service.ScanBarcodeAsync("DOES-NOT-EXIST");

        // Assert
        Assert.Null(result);
    }
}
