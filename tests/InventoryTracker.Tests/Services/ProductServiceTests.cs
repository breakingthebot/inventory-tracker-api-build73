// tests/InventoryTracker.Tests/Services/ProductServiceTests.cs
// Unit tests for ProductService catalog operations, SKU uniqueness, and search filtering.
// Connects to: src/InventoryTracker.Api/Services/ProductService.cs
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

public class ProductServiceTests
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
    public async Task CreateProductAsync_ValidPayload_PersistsProductAndInitialTransaction()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_CreateProduct_Valid");
        var category = new Category { Name = "Hardware", Description = "Tools" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<ProductService>>();
        var service = new ProductService(context, loggerMock.Object);

        var dto = new CreateProductDto
        {
            Sku = "TEST-SKU-001",
            Name = "Precision Screwdriver Set",
            Description = "6-piece magnetic set",
            CategoryId = category.Id,
            UnitPrice = 19.99m,
            UnitCost = 8.50m,
            InitialQuantity = 25,
            ReorderThreshold = 5,
            ReorderQuantity = 20,
            UnitOfMeasure = "set"
        };

        // Act
        var result = await service.CreateProductAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TEST-SKU-001", result.Sku);
        Assert.Equal(25, result.QuantityInStock);
        Assert.Equal(category.Name, result.CategoryName);

        var savedProduct = await context.Products.FirstOrDefaultAsync(p => p.Sku == "TEST-SKU-001");
        Assert.NotNull(savedProduct);
        Assert.Equal(25, savedProduct.QuantityInStock);

        var initialTx = await context.InventoryTransactions.FirstOrDefaultAsync(t => t.ProductId == savedProduct.Id);
        Assert.NotNull(initialTx);
        Assert.Equal(TransactionType.InitialStock, initialTx.Type);
        Assert.Equal(25, initialTx.QuantityChange);
    }

    [Fact]
    public async Task CreateProductAsync_DuplicateSku_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_CreateProduct_DuplicateSku");
        var category = new Category { Name = "General" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var existing = new Product
        {
            Sku = "DUP-SKU-100",
            Name = "Existing Product",
            CategoryId = category.Id,
            UnitPrice = 10m,
            UnitCost = 5m,
            QuantityInStock = 10
        };
        context.Products.Add(existing);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<ProductService>>();
        var service = new ProductService(context, loggerMock.Object);

        var dto = new CreateProductDto
        {
            Sku = "dup-sku-100", // Case-insensitive collision
            Name = "Duplicate Product",
            CategoryId = category.Id,
            UnitPrice = 15m,
            UnitCost = 7m
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateProductAsync(dto));
    }

    [Fact]
    public async Task GetProductsAsync_SearchKeyword_ReturnsMatchingItems()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_GetProducts_Search");
        var category = new Category { Name = "Office" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        context.Products.AddRange(
            new Product { Sku = "OFF-PAP-01", Name = "A4 Copy Paper", CategoryId = category.Id, UnitPrice = 5m, UnitCost = 2m, QuantityInStock = 50 },
            new Product { Sku = "OFF-PEN-01", Name = "Blue Ballpoint Pen", CategoryId = category.Id, UnitPrice = 1m, UnitCost = 0.5m, QuantityInStock = 100 },
            new Product { Sku = "OFF-STP-01", Name = "Heavy Duty Stapler", CategoryId = category.Id, UnitPrice = 12m, UnitCost = 6m, QuantityInStock = 15 }
        );
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<ProductService>>();
        var service = new ProductService(context, loggerMock.Object);

        // Act
        var result = await service.GetProductsAsync(new ProductFilterDto { Search = "paper" });

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("OFF-PAP-01", result.Items[0].Sku);
    }

    [Fact]
    public async Task GetLowStockProductsAsync_ReturnsOnlyItemsAtOrBelowThreshold()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_GetLowStock");
        var category = new Category { Name = "Retail" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        context.Products.AddRange(
            new Product { Sku = "LOW-01", Name = "Low Stock Item", CategoryId = category.Id, QuantityInStock = 3, ReorderThreshold = 10, IsActive = true },
            new Product { Sku = "NORM-01", Name = "Normal Stock Item", CategoryId = category.Id, QuantityInStock = 50, ReorderThreshold = 10, IsActive = true },
            new Product { Sku = "OUT-01", Name = "Out of Stock Item", CategoryId = category.Id, QuantityInStock = 0, ReorderThreshold = 5, IsActive = true }
        );
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<ProductService>>();
        var service = new ProductService(context, loggerMock.Object);

        // Act
        var lowStock = await service.GetLowStockProductsAsync();

        // Assert
        Assert.Equal(2, lowStock.Count);
        Assert.Contains(lowStock, p => p.Sku == "LOW-01");
        Assert.Contains(lowStock, p => p.Sku == "OUT-01");
    }
}
