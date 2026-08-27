// tests/InventoryTracker.Tests/Services/CycleCountServiceTests.cs
// Unit tests for CycleCountService snapshot generation, blind count entry, variance analytics, and reconciliation.
// Connects to: src/InventoryTracker.Api/Services/CycleCountService.cs
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

public class CycleCountServiceTests
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
    public async Task CreateCycleCountAsync_SnapshotsExistingWarehouseStock()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_CC_Create");
        var webhookMock = new Mock<IWebhookService>();
        var loggerMock = new Mock<ILogger<CycleCountService>>();
        var service = new CycleCountService(context, webhookMock.Object, loggerMock.Object);

        var category = new Category { Name = "General" };
        var warehouse = new Warehouse { Code = "WH-AUDIT-1", Name = "Audit WH" };
        context.Categories.Add(category);
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var product = new Product { Sku = "PROD-AUDIT-1", Name = "Audited Item", CategoryId = category.Id, UnitPrice = 50m, UnitCost = 25m, QuantityInStock = 100 };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        context.WarehouseStocks.Add(new WarehouseStock
        {
            WarehouseId = warehouse.Id,
            ProductId = product.Id,
            QuantityOnHand = 100
        });
        await context.SaveChangesAsync();

        var dto = new CreateCycleCountDto
        {
            WarehouseId = warehouse.Id,
            Scope = "FullWarehouse",
            InitiatedBy = "lead_auditor"
        };

        // Act
        var result = await service.CreateCycleCountAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("CC-", result.CountNumber);
        Assert.Equal(CycleCountStatus.InProgress, result.Status);
        Assert.Single(result.Items);
        Assert.Equal(100, result.Items[0].SystemQuantity);
        Assert.Null(result.Items[0].CountedQuantity);
    }

    [Fact]
    public async Task ReconcileCycleCountAsync_AppliesVarianceAdjustmentsToStock()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_CC_Reconcile");
        var webhookMock = new Mock<IWebhookService>();
        var loggerMock = new Mock<ILogger<CycleCountService>>();
        var service = new CycleCountService(context, webhookMock.Object, loggerMock.Object);

        var category = new Category { Name = "General" };
        var warehouse = new Warehouse { Code = "WH-RECON", Name = "Recon WH" };
        context.Categories.Add(category);
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var product = new Product { Sku = "PROD-RECON", Name = "Discrepancy Item", CategoryId = category.Id, UnitPrice = 100m, UnitCost = 40m, QuantityInStock = 50 };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var whStock = new WarehouseStock
        {
            WarehouseId = warehouse.Id,
            ProductId = product.Id,
            QuantityOnHand = 50
        };
        context.WarehouseStocks.Add(whStock);
        await context.SaveChangesAsync();

        var cycleCount = new CycleCount
        {
            CountNumber = "CC-2026-TEST-01",
            WarehouseId = warehouse.Id,
            Status = CycleCountStatus.UnderReview,
            InitiatedBy = "auditor"
        };
        var item = new CycleCountItem
        {
            ProductId = product.Id,
            SystemQuantity = 50,
            CountedQuantity = 48, // 2 units missing (-2 variance)
            UnitCost = 40m,
            CountedBy = "clerk",
            IsReconciled = false
        };
        cycleCount.Items.Add(item);
        context.CycleCounts.Add(cycleCount);
        await context.SaveChangesAsync();

        var reconcileDto = new ReconcileCycleCountDto
        {
            ApprovedBy = "warehouse_manager",
            Notes = "Shrinkage approved after re-count"
        };

        // Act
        var result = await service.ReconcileCycleCountAsync(cycleCount.Id, reconcileDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CycleCountStatus.Reconciled, result.Status);
        Assert.Equal("warehouse_manager", result.ReviewedBy);
        Assert.Equal(-2, result.TotalVarianceUnits);
        Assert.Equal(-80m, result.TotalVarianceCost);

        var updatedWhStock = await context.WarehouseStocks.FirstOrDefaultAsync(ws => ws.WarehouseId == warehouse.Id && ws.ProductId == product.Id);
        Assert.NotNull(updatedWhStock);
        Assert.Equal(48, updatedWhStock.QuantityOnHand);

        var updatedProduct = await context.Products.FindAsync(product.Id);
        Assert.Equal(48, updatedProduct!.QuantityInStock);

        var adjustmentTx = await context.InventoryTransactions.FirstOrDefaultAsync(t => t.ReferenceNumber == "CC-RECON-CC-2026-TEST-01");
        Assert.NotNull(adjustmentTx);
        Assert.Equal(-2, adjustmentTx.QuantityChange);
    }
}
