// tests/InventoryTracker.Tests/Services/SalesOrderServiceTests.cs
// Unit tests for SalesOrderService customer management, stock allocation, pick lists, and shipment deductions.
// Connects to: src/InventoryTracker.Api/Services/SalesOrderService.cs
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

public class SalesOrderServiceTests
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
    public async Task CreateAndAllocateSalesOrder_ReservesWarehouseStock()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_SO_Allocate");
        var webhookMock = new Mock<IWebhookService>();
        var loggerMock = new Mock<ILogger<SalesOrderService>>();
        var service = new SalesOrderService(context, webhookMock.Object, loggerMock.Object);

        var warehouse = new Warehouse { Code = "WH-SO", Name = "SO WH" };
        var category = new Category { Name = "General" };
        context.Warehouses.Add(warehouse);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product { Sku = "PROD-SO-1", Name = "Order Item", CategoryId = category.Id, UnitPrice = 100m, UnitCost = 40m, QuantityInStock = 50 };
        var customer = new Customer { CustomerCode = "CUST-001", CompanyName = "Acme Corp", Email = "acme@example.com", ShippingAddress = "123 Main St", ShippingCity = "Dallas", ShippingState = "TX", ShippingPostalCode = "75001" };
        context.Products.Add(product);
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var whStock = new WarehouseStock { WarehouseId = warehouse.Id, ProductId = product.Id, QuantityOnHand = 50, QuantityReserved = 0 };
        context.WarehouseStocks.Add(whStock);
        await context.SaveChangesAsync();

        var createDto = new CreateSalesOrderDto
        {
            CustomerId = customer.Id,
            WarehouseId = warehouse.Id,
            Items = new List<CreateSalesOrderItemDto>
            {
                new() { ProductId = product.Id, QuantityOrdered = 5, UnitPrice = 100m }
            }
        };

        // Act - Step 1: Create
        var orderDto = await service.CreateSalesOrderAsync(createDto);
        Assert.NotNull(orderDto);
        Assert.Equal(SalesOrderStatus.Draft, orderDto.Status);
        Assert.Equal(500m, orderDto.TotalAmount);

        // Act - Step 2: Allocate
        var allocatedOrder = await service.AllocateOrderAsync(orderDto.Id);

        // Assert
        Assert.NotNull(allocatedOrder);
        Assert.Equal(SalesOrderStatus.Allocated, allocatedOrder.Status);

        var updatedWhStock = await context.WarehouseStocks.FirstAsync(ws => ws.WarehouseId == warehouse.Id && ws.ProductId == product.Id);
        Assert.Equal(5, updatedWhStock.QuantityReserved);
        Assert.Equal(45, updatedWhStock.AvailableQuantity); // 50 - 5 = 45
    }

    [Fact]
    public async Task ShipOrderAsync_DeductsPhysicalStockAndReleasesReservation()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_SO_Ship");
        var webhookMock = new Mock<IWebhookService>();
        var loggerMock = new Mock<ILogger<SalesOrderService>>();
        var service = new SalesOrderService(context, webhookMock.Object, loggerMock.Object);

        var warehouse = new Warehouse { Code = "WH-SHIP", Name = "Shipping WH" };
        var category = new Category { Name = "General" };
        context.Warehouses.Add(warehouse);
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product { Sku = "PROD-SHIP-1", Name = "Ship Item", CategoryId = category.Id, UnitPrice = 80m, UnitCost = 35m, QuantityInStock = 20 };
        var customer = new Customer { CustomerCode = "CUST-002", CompanyName = "Beta Tech", Email = "beta@example.com", ShippingAddress = "456 Tech Blvd", ShippingCity = "Austin", ShippingState = "TX", ShippingPostalCode = "73301" };
        context.Products.Add(product);
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var whStock = new WarehouseStock { WarehouseId = warehouse.Id, ProductId = product.Id, QuantityOnHand = 20, QuantityReserved = 4 };
        context.WarehouseStocks.Add(whStock);
        await context.SaveChangesAsync();

        var order = new SalesOrder
        {
            OrderNumber = "SO-2026-TEST-SHIP",
            CustomerId = customer.Id,
            WarehouseId = warehouse.Id,
            Status = SalesOrderStatus.Packed,
            Subtotal = 320m,
            TotalAmount = 320m,
            ShippingCarrier = "FedEx Ground"
        };
        order.Items.Add(new SalesOrderItem
        {
            ProductId = product.Id,
            QuantityOrdered = 4,
            QuantityPicked = 4,
            UnitPrice = 80m,
            UnitCostSnapshot = 35m
        });
        context.SalesOrders.Add(order);
        await context.SaveChangesAsync();

        var shipDto = new ShipOrderDto
        {
            TrackingNumber = "FDX-9988776655",
            ShippingCarrier = "FedEx Ground"
        };

        // Act
        var result = await service.ShipOrderAsync(order.Id, shipDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SalesOrderStatus.Shipped, result.Status);
        Assert.Equal("FDX-9988776655", result.TrackingNumber);

        var updatedWhStock = await context.WarehouseStocks.FirstAsync(ws => ws.WarehouseId == warehouse.Id && ws.ProductId == product.Id);
        Assert.Equal(16, updatedWhStock.QuantityOnHand); // 20 - 4 = 16
        Assert.Equal(0, updatedWhStock.QuantityReserved); // 4 - 4 = 0

        var updatedProduct = await context.Products.FindAsync(product.Id);
        Assert.Equal(16, updatedProduct!.QuantityInStock);

        var tx = await context.InventoryTransactions.FirstOrDefaultAsync(t => t.ReferenceNumber == "SO-SO-2026-TEST-SHIP");
        Assert.NotNull(tx);
        Assert.Equal(-4, tx.QuantityChange);
    }
}
