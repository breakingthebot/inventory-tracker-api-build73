// tests/InventoryTracker.Tests/Services/PurchaseOrderServiceTests.cs
// Unit tests for PurchaseOrderService auto-reorder recommendations, automated batch generation, submission, and receiving.
// Connects to: src/InventoryTracker.Api/Services/PurchaseOrderService.cs
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

public class PurchaseOrderServiceTests
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
    public async Task GetAutoReorderSuggestionsAsync_IdentifiesLowStockItemsAccurately()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_PoSuggestions");
        var supplier = new Supplier { Code = "SUP-1", Name = "Supplier 1", Email = "sup1@test.com", LeadTimeDays = 5 };
        var category = new Category { Name = "Category 1" };
        context.Suppliers.Add(supplier);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        context.Products.AddRange(
            new Product
            {
                Sku = "LOW-01",
                Name = "Low Item",
                CategoryId = category.Id,
                PrimarySupplierId = supplier.Id,
                QuantityInStock = 4,
                ReorderThreshold = 10,
                ReorderQuantity = 50,
                UnitCost = 15m,
                IsActive = true
            },
            new Product
            {
                Sku = "OUT-01",
                Name = "Out Item",
                CategoryId = category.Id,
                PrimarySupplierId = supplier.Id,
                QuantityInStock = 0,
                ReorderThreshold = 5,
                ReorderQuantity = 30,
                UnitCost = 25m,
                IsActive = true
            },
            new Product
            {
                Sku = "HEALTHY-01",
                Name = "Healthy Item",
                CategoryId = category.Id,
                QuantityInStock = 100,
                ReorderThreshold = 10,
                ReorderQuantity = 50,
                IsActive = true
            }
        );
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<PurchaseOrderService>>();
        var service = new PurchaseOrderService(context, loggerMock.Object);

        // Act
        var suggestions = await service.GetAutoReorderSuggestionsAsync();

        // Assert
        Assert.Equal(2, suggestions.Count);
        Assert.Contains(suggestions, s => s.ProductSku == "LOW-01" && s.RecommendedOrderQuantity == 50);
        Assert.Contains(suggestions, s => s.ProductSku == "OUT-01" && s.RecommendedOrderQuantity == 30);
    }

    [Fact]
    public async Task AutoGeneratePurchaseOrdersAsync_GroupsBySupplierAndCreatesDraftOrders()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_AutoGeneratePo");
        var supplierA = new Supplier { Code = "SUP-A", Name = "Supplier A", Email = "a@test.com", LeadTimeDays = 4, IsActive = true };
        var supplierB = new Supplier { Code = "SUP-B", Name = "Supplier B", Email = "b@test.com", LeadTimeDays = 7, IsActive = true };
        var warehouse = new Warehouse { Code = "WH-DEST", Name = "Dest Warehouse", IsActive = true };
        var category = new Category { Name = "General" };
        context.Suppliers.AddRange(supplierA, supplierB);
        context.Warehouses.Add(warehouse);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        context.Products.AddRange(
            new Product { Sku = "PROD-A1", Name = "Item A1", CategoryId = category.Id, PrimarySupplierId = supplierA.Id, QuantityInStock = 2, ReorderThreshold = 10, ReorderQuantity = 40, UnitCost = 10m, IsActive = true },
            new Product { Sku = "PROD-A2", Name = "Item A2", CategoryId = category.Id, PrimarySupplierId = supplierA.Id, QuantityInStock = 1, ReorderThreshold = 5, ReorderQuantity = 20, UnitCost = 15m, IsActive = true },
            new Product { Sku = "PROD-B1", Name = "Item B1", CategoryId = category.Id, PrimarySupplierId = supplierB.Id, QuantityInStock = 0, ReorderThreshold = 15, ReorderQuantity = 100, UnitCost = 5m, IsActive = true }
        );
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<PurchaseOrderService>>();
        var service = new PurchaseOrderService(context, loggerMock.Object);

        // Act
        var result = await service.AutoGeneratePurchaseOrdersAsync(warehouse.Id);

        // Assert
        Assert.Equal(2, result.TotalPurchaseOrdersCreated); // 1 for Supplier A (2 items), 1 for Supplier B (1 item)
        Assert.Equal(3, result.TotalDistinctItemsReordered);
        Assert.Equal(160, result.TotalUnitsReordered); // 40 + 20 + 100

        var createdPos = await context.PurchaseOrders.Include(p => p.Items).ToListAsync();
        Assert.Equal(2, createdPos.Count);
        Assert.All(createdPos, p => Assert.Equal(PurchaseOrderStatus.Draft, p.Status));
        Assert.All(createdPos, p => Assert.True(p.IsAutoGenerated));
    }

    [Fact]
    public async Task ReceivePurchaseOrderAsync_IncrementsStockAndSetsFulfilledStatus()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_ReceivePo");
        var supplier = new Supplier { Code = "SUP-REC", Name = "Supplier Rec", Email = "rec@test.com", IsActive = true };
        var warehouse = new Warehouse { Code = "WH-REC", Name = "Warehouse Rec", IsActive = true };
        var category = new Category { Name = "General" };
        context.Suppliers.Add(supplier);
        context.Warehouses.Add(warehouse);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Sku = "PROD-REC-PO",
            Name = "PO Product",
            CategoryId = category.Id,
            PrimarySupplierId = supplier.Id,
            QuantityInStock = 10,
            UnitCost = 20.00m,
            IsActive = true
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var po = new PurchaseOrder
        {
            OrderNumber = "PO-REC-001",
            SupplierId = supplier.Id,
            DestinationWarehouseId = warehouse.Id,
            Status = PurchaseOrderStatus.Submitted,
            Items = new List<PurchaseOrderItem>
            {
                new() { ProductId = product.Id, QuantityOrdered = 50, QuantityReceived = 0, UnitCost = 20.00m }
            }
        };
        context.PurchaseOrders.Add(po);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<PurchaseOrderService>>();
        var service = new PurchaseOrderService(context, loggerMock.Object);

        var poItem = po.Items.First();
        var receiveDto = new ReceivePurchaseOrderDto
        {
            ReceivedItems = new List<ReceivePoItemDto>
            {
                new() { PurchaseOrderItemId = poItem.Id, QuantityReceived = 50, ActualUnitCost = 20.00m }
            },
            ReceivedBy = "dock_inspector"
        };

        // Act
        var result = await service.ReceivePurchaseOrderAsync(po.Id, receiveDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PurchaseOrderStatus.Fulfilled, result.Status);
        Assert.NotNull(result.CompletedAtUtc);

        var updatedProduct = await context.Products.FindAsync(product.Id);
        Assert.NotNull(updatedProduct);
        Assert.Equal(60, updatedProduct.QuantityInStock); // 10 + 50 = 60

        var warehouseStock = await context.WarehouseStocks.FirstOrDefaultAsync(ws => ws.WarehouseId == warehouse.Id && ws.ProductId == product.Id);
        Assert.NotNull(warehouseStock);
        Assert.Equal(50, warehouseStock.QuantityOnHand);

        var tx = await context.InventoryTransactions.FirstOrDefaultAsync(t => t.ReferenceNumber == "PO-REC-001");
        Assert.NotNull(tx);
        Assert.Equal(TransactionType.StockIn, tx.Type);
        Assert.Equal(50, tx.QuantityChange);
    }
}
