// tests/InventoryTracker.Tests/Services/AnalyticsServiceTests.cs
// Unit tests for AnalyticsService valuation computations, category metrics, and health diagnostics.
// Connects to: src/InventoryTracker.Api/Services/AnalyticsService.cs
// Created: 2026-08-26

using InventoryTracker.Api.Data;
using InventoryTracker.Api.Models;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace InventoryTracker.Tests.Services;

public class AnalyticsServiceTests
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
    public async Task GetInventorySummaryAsync_CalculatesValuationAndStockMetricsAccurately()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_AnalyticsSummary");
        var catElectronics = new Category { Name = "Electronics" };
        var catOffice = new Category { Name = "Office" };
        context.Categories.AddRange(catElectronics, catOffice);
        await context.SaveChangesAsync();

        context.Products.AddRange(
            new Product
            {
                Sku = "ELEC-1",
                Name = "Monitor",
                CategoryId = catElectronics.Id,
                UnitPrice = 300m,
                UnitCost = 200m,
                QuantityInStock = 10, // Cost valuation: 2000, Retail: 3000
                ReorderThreshold = 5,
                IsActive = true
            },
            new Product
            {
                Sku = "ELEC-2",
                Name = "Mouse",
                CategoryId = catElectronics.Id,
                UnitPrice = 50m,
                UnitCost = 25m,
                QuantityInStock = 2, // Low stock (<= 5). Cost valuation: 50, Retail: 100
                ReorderThreshold = 5,
                IsActive = true
            },
            new Product
            {
                Sku = "OFF-1",
                Name = "Notebook",
                CategoryId = catOffice.Id,
                UnitPrice = 5m,
                UnitCost = 2m,
                QuantityInStock = 0, // Out of stock. Cost: 0, Retail: 0
                ReorderThreshold = 10,
                IsActive = true
            }
        );
        await context.SaveChangesAsync();

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns("Testing");

        var service = new AnalyticsService(context, envMock.Object);

        // Act
        var summary = await service.GetInventorySummaryAsync();

        // Assert
        Assert.Equal(3, summary.TotalProducts);
        Assert.Equal(3, summary.ActiveProducts);
        Assert.Equal(12, summary.TotalUnitsInStock); // 10 + 2 + 0
        Assert.Equal(2050.00m, summary.TotalInventoryValuation); // (10*200) + (2*25) + 0 = 2050
        Assert.Equal(3100.00m, summary.TotalRetailValue); // (10*300) + (2*50) + 0 = 3100
        Assert.Equal(1, summary.LowStockProductsCount); // ELEC-2
        Assert.Equal(1, summary.OutOfStockProductsCount); // OFF-1
        Assert.Equal(2, summary.CategoryBreakdown.Count);
    }

    [Fact]
    public async Task GetHealthStatusAsync_ReturnsHealthyStatus()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_HealthStatus");
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns("Production");

        var service = new AnalyticsService(context, envMock.Object);

        // Act
        var health = await service.GetHealthStatusAsync();

        // Assert
        Assert.Equal("Healthy", health.Status);
        Assert.Equal("Connected", health.DatabaseStatus);
        Assert.Equal("Production", health.Environment);
    }
}
