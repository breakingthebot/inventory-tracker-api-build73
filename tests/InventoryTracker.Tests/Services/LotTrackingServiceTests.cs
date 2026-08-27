// tests/InventoryTracker.Tests/Services/LotTrackingServiceTests.cs
// Unit tests for LotTrackingService FEFO allocation algorithms, expiration warnings, and stock synchronization.
// Connects to: src/InventoryTracker.Api/Services/LotTrackingService.cs
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

public class LotTrackingServiceTests
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
    public async Task GetFefoAllocationPlanAsync_PrioritizesEarliestExpiringLotsFirst()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_Lot_FefoPlan");
        var webhookMock = new Mock<IWebhookService>();
        var loggerMock = new Mock<ILogger<LotTrackingService>>();
        var service = new LotTrackingService(context, webhookMock.Object, loggerMock.Object);

        var product = new Product { Sku = "SKU-LOT-01", Name = "Lot Product", UnitPrice = 10m, UnitCost = 5m, QuantityInStock = 50 };
        var warehouse = new Warehouse { Code = "WH-01", Name = "Warehouse 1" };
        context.Products.Add(product);
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        // Lot A expires in 10 days (15 units)
        context.ProductLots.Add(new ProductLot
        {
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            LotNumber = "LOT-A",
            QuantityInitial = 15,
            QuantityOnHand = 15,
            ExpirationDateUtc = DateTime.UtcNow.AddDays(10),
            Status = LotStatus.Active,
            ReceivedAtUtc = DateTime.UtcNow.AddDays(-10)
        });

        // Lot B expires in 30 days (20 units)
        context.ProductLots.Add(new ProductLot
        {
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            LotNumber = "LOT-B",
            QuantityInitial = 20,
            QuantityOnHand = 20,
            ExpirationDateUtc = DateTime.UtcNow.AddDays(30),
            Status = LotStatus.Active,
            ReceivedAtUtc = DateTime.UtcNow.AddDays(-5)
        });

        await context.SaveChangesAsync();

        // Act - Request 20 units (should take all 15 from Lot A, and 5 from Lot B)
        var plan = await service.GetFefoAllocationPlanAsync(product.Id, 20, warehouse.Id);

        // Assert
        Assert.NotNull(plan);
        Assert.True(plan.IsFullyAllocated);
        Assert.Equal(20, plan.TotalAllocatedQuantity);
        Assert.Equal(2, plan.Allocations.Count);

        Assert.Equal("LOT-A", plan.Allocations[0].LotNumber);
        Assert.Equal(15, plan.Allocations[0].QuantityToPick);

        Assert.Equal("LOT-B", plan.Allocations[1].LotNumber);
        Assert.Equal(5, plan.Allocations[1].QuantityToPick);
    }

    [Fact]
    public async Task DispatchFefoAsync_ExecutesDeductionsAndUpdatesLotBalances()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_Lot_DispatchFefo");
        var webhookMock = new Mock<IWebhookService>();
        var loggerMock = new Mock<ILogger<LotTrackingService>>();
        var service = new LotTrackingService(context, webhookMock.Object, loggerMock.Object);

        var product = new Product { Sku = "SKU-LOT-DISP", Name = "Lot Product", UnitPrice = 20m, UnitCost = 10m, QuantityInStock = 30 };
        var warehouse = new Warehouse { Code = "WH-01", Name = "Warehouse 1" };
        context.Products.Add(product);
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        context.WarehouseStocks.Add(new WarehouseStock
        {
            WarehouseId = warehouse.Id,
            ProductId = product.Id,
            QuantityOnHand = 30
        });

        context.ProductLots.Add(new ProductLot
        {
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            LotNumber = "LOT-EXP-1",
            QuantityInitial = 10,
            QuantityOnHand = 10,
            ExpirationDateUtc = DateTime.UtcNow.AddDays(5),
            Status = LotStatus.Active
        });

        context.ProductLots.Add(new ProductLot
        {
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            LotNumber = "LOT-EXP-2",
            QuantityInitial = 20,
            QuantityOnHand = 20,
            ExpirationDateUtc = DateTime.UtcNow.AddDays(60),
            Status = LotStatus.Active
        });

        await context.SaveChangesAsync();

        var request = new DispatchFefoRequestDto
        {
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            Quantity = 12,
            ReferenceNumber = "SO-2026-001",
            Reason = "Customer Order"
        };

        // Act
        var result = await service.DispatchFefoAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(12, result.TotalDispatchedQuantity);

        var lot1 = await context.ProductLots.FirstOrDefaultAsync(l => l.LotNumber == "LOT-EXP-1");
        Assert.NotNull(lot1);
        Assert.Equal(0, lot1.QuantityOnHand);
        Assert.Equal(LotStatus.Depleted, lot1.Status);

        var lot2 = await context.ProductLots.FirstOrDefaultAsync(l => l.LotNumber == "LOT-EXP-2");
        Assert.NotNull(lot2);
        Assert.Equal(18, lot2.QuantityOnHand);

        var updatedProduct = await context.Products.FindAsync(product.Id);
        Assert.Equal(18, updatedProduct!.QuantityInStock);
    }
}
