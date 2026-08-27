// tests/InventoryTracker.Tests/Services/BulkDataServiceTests.cs
// Unit tests for BulkDataService CSV parsing, row-level validation, batch upserts, and export streaming.
// Connects to: src/InventoryTracker.Api/Services/BulkDataService.cs
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

public class BulkDataServiceTests
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
    public async Task ImportProductsFromCsvAsync_ValidCsv_InsertsAndUpdatesProducts()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_BulkImport_Success");
        var category = new Category { Name = "Hardware" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        // Existing product to be updated
        context.Products.Add(new Product
        {
            Sku = "SKU-EXISTING",
            Name = "Old Name",
            CategoryId = category.Id,
            UnitPrice = 10m,
            UnitCost = 5m,
            QuantityInStock = 20,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<BulkDataService>>();
        var service = new BulkDataService(context, loggerMock.Object);

        var csv = @"Sku,Name,Category,Description,UnitPrice,UnitCost,QuantityInStock,ReorderThreshold,ReorderQuantity,UnitOfMeasure,PrimarySupplierCode
SKU-EXISTING,Updated New Name,Hardware,Updated description,15.50,7.20,30,10,50,pcs,
SKU-NEW-01,Brand New Item,Office Supplies,New stationery,8.00,3.50,100,25,100,box,";

        // Act
        var result = await service.ImportProductsFromCsvAsync(csv);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalRowsRead);
        Assert.Equal(1, result.RowsInserted);
        Assert.Equal(1, result.RowsUpdated);
        Assert.Equal(0, result.RowsFailed);
        Assert.False(result.HasErrors);

        var updated = await context.Products.FirstOrDefaultAsync(p => p.Sku == "SKU-EXISTING");
        Assert.NotNull(updated);
        Assert.Equal("Updated New Name", updated.Name);
        Assert.Equal(15.50m, updated.UnitPrice);

        var inserted = await context.Products.FirstOrDefaultAsync(p => p.Sku == "SKU-NEW-01");
        Assert.NotNull(inserted);
        Assert.Equal("Brand New Item", inserted.Name);
    }

    [Fact]
    public async Task ImportProductsFromCsvAsync_InvalidRows_CapturesRowLevelErrors()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_BulkImport_Errors");
        var loggerMock = new Mock<ILogger<BulkDataService>>();
        var service = new BulkDataService(context, loggerMock.Object);

        var csv = @"Sku,Name,Category,UnitPrice,UnitCost,QuantityInStock
,Missing Sku Item,General,10.00,5.00,10
SKU-BAD-PRICE,Bad Price Item,General,INVALID_NUMBER,5.00,10
SKU-GOOD,Good Item,General,12.00,6.00,20";

        // Act
        var result = await service.ImportProductsFromCsvAsync(csv);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalRowsRead);
        Assert.Equal(1, result.RowsInserted);
        Assert.Equal(2, result.RowsFailed);
        Assert.True(result.HasErrors);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task ExportProductsToCsvAsync_ReturnsCompleteCsvOutput()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_BulkExport");
        var category = new Category { Name = "Tools" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        context.Products.Add(new Product
        {
            Sku = "TOOL-HAMMER-01",
            Name = "Claw Hammer 16oz",
            CategoryId = category.Id,
            UnitPrice = 24.99m,
            UnitCost = 11.50m,
            QuantityInStock = 50,
            ReorderThreshold = 10,
            ReorderQuantity = 25,
            UnitOfMeasure = "pcs",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<BulkDataService>>();
        var service = new BulkDataService(context, loggerMock.Object);

        // Act
        var csv = await service.ExportProductsToCsvAsync();

        // Assert
        Assert.NotEmpty(csv);
        Assert.Contains("Sku,Name,Category", csv);
        Assert.Contains("TOOL-HAMMER-01", csv);
        Assert.Contains("Claw Hammer 16oz", csv);
    }
}
