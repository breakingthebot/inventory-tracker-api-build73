// tests/InventoryTracker.Tests/Controllers/BulkControllerTests.cs
// Unit tests for BulkController CSV import, export, and template downloads.
// Connects to: src/InventoryTracker.Api/Controllers/BulkController.cs
// Created: 2026-08-27

using InventoryTracker.Api.Controllers;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryTracker.Tests.Controllers;

public class BulkControllerTests
{
    [Fact]
    public void DownloadTemplate_ReturnsFileContentResultWithCsv()
    {
        // Arrange
        var mockService = new Mock<IBulkDataService>();
        mockService.Setup(s => s.GetProductCsvTemplate())
            .Returns("Sku,Name,Category,UnitPrice\n");

        var controller = new BulkController(mockService.Object);

        // Act
        var result = controller.DownloadTemplate();

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Equal("inventory-import-template.csv", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task ExportProducts_ReturnsCsvFileResult()
    {
        // Arrange
        var mockService = new Mock<IBulkDataService>();
        mockService.Setup(s => s.ExportProductsToCsvAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Sku,Name,Category\nSKU-1,Item 1,Electronics\n");

        var controller = new BulkController(mockService.Object);

        // Act
        var result = await controller.ExportProducts(CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Contains("inventory-catalog-export", fileResult.FileDownloadName);
    }
}
