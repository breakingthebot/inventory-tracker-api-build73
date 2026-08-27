// tests/InventoryTracker.Tests/Controllers/BarcodesControllerTests.cs
// Unit tests for BarcodesController REST endpoints and scanner resolution.
// Connects to: src/InventoryTracker.Api/Controllers/BarcodesController.cs
// Created: 2026-08-27

using InventoryTracker.Api.Controllers;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryTracker.Tests.Controllers;

public class BarcodesControllerTests
{
    [Fact]
    public void GetBarcodeBySku_ValidSku_ReturnsOkWithSvg()
    {
        // Arrange
        var mockService = new Mock<IBarcodeService>();
        var dto = new BarcodeResponseDto
        {
            Value = "SKU-TEST-01",
            Symbology = BarcodeSymbology.Code128,
            SvgContent = "<svg></svg>"
        };

        mockService.Setup(s => s.GenerateCode128Barcode("SKU-TEST-01", 80, 2))
            .Returns(dto);

        var controller = new BarcodesController(mockService.Object);

        // Act
        var result = controller.GetBarcodeBySku("SKU-TEST-01");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<BarcodeResponseDto>>(okResult.Value);
        Assert.True(envelope.Success);
        Assert.Equal("SKU-TEST-01", envelope.Data?.Value);
    }

    [Fact]
    public async Task ScanBarcode_ValidCode_ReturnsOkWithScannerDto()
    {
        // Arrange
        var mockService = new Mock<IBarcodeService>();
        var scannerDto = new ProductScannerDto
        {
            ProductId = 1,
            Sku = "SKU-SCAN-01",
            Name = "Scanned Item",
            TotalStockOnHand = 20
        };

        mockService.Setup(s => s.ScanBarcodeAsync("SKU-SCAN-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(scannerDto);

        var controller = new BarcodesController(mockService.Object);

        // Act
        var result = await controller.ScanBarcode("SKU-SCAN-01", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<ProductScannerDto>>(okResult.Value);
        Assert.True(envelope.Success);
        Assert.Equal(20, envelope.Data?.TotalStockOnHand);
    }
}
