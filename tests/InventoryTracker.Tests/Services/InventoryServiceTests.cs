// tests/InventoryTracker.Tests/Services/InventoryServiceTests.cs
// Unit tests for InventoryService stock adjustments, restock intake, dispatch fulfillment, and audit logging.
// Connects to: src/InventoryTracker.Api/Services/InventoryService.cs
// Created: 2026-08-26

using InventoryTracker.Api.Data;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using InventoryTracker.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryTracker.Tests.Services;

public class InventoryServiceTests
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
    public async Task RestockAsync_IncreasesStockAndCreatesStockInTransaction()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_Restock_Success");
        var category = new Category { Name = "General" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Sku = "RESTOCK-01",
            Name = "Restock Test Item",
            CategoryId = category.Id,
            UnitPrice = 20m,
            UnitCost = 10m,
            QuantityInStock = 10
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<InventoryService>>();
        var service = new InventoryService(context, loggerMock.Object);

        var request = new RestockRequestDto
        {
            ProductId = product.Id,
            Quantity = 40,
            UnitCost = 12m,
            PurchaseOrderNumber = "PO-2026-99",
            Notes = "Supplier replenishment"
        };

        // Act
        var result = await service.RestockAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TransactionType.StockIn, result.Type);
        Assert.Equal(40, result.QuantityChange);
        Assert.Equal(50, result.QuantityAfter);

        var updatedProduct = await context.Products.FindAsync(product.Id);
        Assert.NotNull(updatedProduct);
        Assert.Equal(50, updatedProduct.QuantityInStock);
    }

    [Fact]
    public async Task DispatchAsync_SufficientStock_DecrementsStockAndLogsStockOut()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_Dispatch_Success");
        var category = new Category { Name = "General" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Sku = "DISPATCH-01",
            Name = "Dispatch Test Item",
            CategoryId = category.Id,
            UnitPrice = 50m,
            UnitCost = 25m,
            QuantityInStock = 25
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<InventoryService>>();
        var service = new InventoryService(context, loggerMock.Object);

        var request = new DispatchRequestDto
        {
            ProductId = product.Id,
            Quantity = 10,
            SalesOrderNumber = "SO-4001",
            Notes = "Customer fulfillment"
        };

        // Act
        var result = await service.DispatchAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TransactionType.StockOut, result.Type);
        Assert.Equal(-10, result.QuantityChange);
        Assert.Equal(15, result.QuantityAfter);

        var updatedProduct = await context.Products.FindAsync(product.Id);
        Assert.NotNull(updatedProduct);
        Assert.Equal(15, updatedProduct.QuantityInStock);
    }

    [Fact]
    public async Task DispatchAsync_InsufficientStock_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_Dispatch_Insufficient");
        var category = new Category { Name = "General" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Sku = "LOW-AVAIL-01",
            Name = "Low Availability Item",
            CategoryId = category.Id,
            QuantityInStock = 5
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<InventoryService>>();
        var service = new InventoryService(context, loggerMock.Object);

        var request = new DispatchRequestDto
        {
            ProductId = product.Id,
            Quantity = 20, // Exceeds available 5
            SalesOrderNumber = "SO-FAIL"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DispatchAsync(request));
    }

    [Fact]
    public async Task AdjustStockAsync_NegativeExcessAdjustment_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_AdjustStock_NegativeExcess");
        var category = new Category { Name = "General" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Sku = "ADJUST-01",
            Name = "Adjustment Item",
            CategoryId = category.Id,
            QuantityInStock = 8
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<InventoryService>>();
        var service = new InventoryService(context, loggerMock.Object);

        var request = new StockAdjustmentDto
        {
            ProductId = product.Id,
            QuantityChange = -15, // Would cause negative stock (-7)
            Reason = "Shrinkage correction"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AdjustStockAsync(request));
    }
}
