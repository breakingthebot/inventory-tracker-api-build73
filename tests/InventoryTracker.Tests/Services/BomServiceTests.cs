// tests/InventoryTracker.Tests/Services/BomServiceTests.cs
// Unit tests for BomService cost roll-ups, max yield analytics, kit assembly deductions, and disassembly.
// Connects to: src/InventoryTracker.Api/Services/BomService.cs
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

public class BomServiceTests
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
    public async Task GetProductBomAsync_CalculatesRolledUpCostAndMaxAssemblableKits()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_BOM_CostRollup");
        var webhookMock = new Mock<IWebhookService>();
        var loggerMock = new Mock<ILogger<BomService>>();
        var service = new BomService(context, webhookMock.Object, loggerMock.Object);

        var parentKit = new Product { Sku = "KIT-BUNDLE-1", Name = "Deluxe Kit", UnitPrice = 300m, UnitCost = 0m, IsBundleOrKit = true };
        var compA = new Product { Sku = "COMP-A", Name = "Component A", UnitCost = 50m, QuantityInStock = 20 };
        var compB = new Product { Sku = "COMP-B", Name = "Component B", UnitCost = 15m, QuantityInStock = 30 };

        context.Products.AddRange(parentKit, compA, compB);
        await context.SaveChangesAsync();

        // Kit requires 1x Comp A and 2x Comp B
        context.BillOfMaterials.Add(new BillOfMaterials { ParentProductId = parentKit.Id, ComponentProductId = compA.Id, QuantityRequired = 1 });
        context.BillOfMaterials.Add(new BillOfMaterials { ParentProductId = parentKit.Id, ComponentProductId = compB.Id, QuantityRequired = 2 });
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetProductBomAsync(parentKit.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(80m, result.RolledUpMaterialCost); // 1*50 + 2*15 = 80
        Assert.Equal(15, result.MaxAssemblableKits); // min(20/1, 30/2) = 15
        Assert.Equal("COMP-B", result.LimitingComponentSku);
    }

    [Fact]
    public async Task AssembleKitAsync_DeductsComponentsAndReceivesFinishedKit()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_BOM_Assemble");
        var webhookMock = new Mock<IWebhookService>();
        var loggerMock = new Mock<ILogger<BomService>>();
        var service = new BomService(context, webhookMock.Object, loggerMock.Object);

        var warehouse = new Warehouse { Code = "WH-BOM", Name = "BOM Facility" };
        var parentKit = new Product { Sku = "KIT-DESK-01", Name = "Desk Kit", UnitPrice = 250m, UnitCost = 0m, QuantityInStock = 0, IsBundleOrKit = true };
        var comp1 = new Product { Sku = "COMP-MON", Name = "Monitor", UnitCost = 100m, QuantityInStock = 10 };
        var comp2 = new Product { Sku = "COMP-HUB", Name = "Hub", UnitCost = 30m, QuantityInStock = 10 };

        context.Warehouses.Add(warehouse);
        context.Products.AddRange(parentKit, comp1, comp2);
        await context.SaveChangesAsync();

        context.WarehouseStocks.Add(new WarehouseStock { WarehouseId = warehouse.Id, ProductId = comp1.Id, QuantityOnHand = 10 });
        context.WarehouseStocks.Add(new WarehouseStock { WarehouseId = warehouse.Id, ProductId = comp2.Id, QuantityOnHand = 10 });
        context.BillOfMaterials.Add(new BillOfMaterials { ParentProductId = parentKit.Id, ComponentProductId = comp1.Id, QuantityRequired = 1 });
        context.BillOfMaterials.Add(new BillOfMaterials { ParentProductId = parentKit.Id, ComponentProductId = comp2.Id, QuantityRequired = 1 });
        await context.SaveChangesAsync();

        var request = new AssembleKitRequestDto
        {
            KitProductId = parentKit.Id,
            WarehouseId = warehouse.Id,
            Quantity = 3,
            LaborCost = 45m,
            AssembledBy = "lead_tech"
        };

        // Act
        var result = await service.AssembleKitAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.QuantityAssembled);
        Assert.Equal(145m, result.RolledUpUnitCost); // ((100*3 + 30*3) + 45) / 3 = (390 + 45)/3 = 145

        var comp1Stock = await context.WarehouseStocks.FirstOrDefaultAsync(ws => ws.WarehouseId == warehouse.Id && ws.ProductId == comp1.Id);
        Assert.NotNull(comp1Stock);
        Assert.Equal(7, comp1Stock.QuantityOnHand); // 10 - 3 = 7

        var kitStock = await context.WarehouseStocks.FirstOrDefaultAsync(ws => ws.WarehouseId == warehouse.Id && ws.ProductId == parentKit.Id);
        Assert.NotNull(kitStock);
        Assert.Equal(3, kitStock.QuantityOnHand);
    }
}
