// tests/InventoryTracker.Tests/Services/TransferServiceTests.cs
// Unit tests for TransferService multi-stage transfer order creation, shipment dispatch, receiving, and cancellation.
// Connects to: src/InventoryTracker.Api/Services/TransferService.cs
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

public class TransferServiceTests
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
    public async Task CreateTransferAsync_ValidStock_ReservesSourceQuantityAndCreatesPendingTransfer()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_CreateTransfer_Success");
        var category = new Category { Name = "General" };
        context.Categories.Add(category);

        var whSource = new Warehouse { Code = "WH-SRC", Name = "Source Facility", IsActive = true };
        var whDest = new Warehouse { Code = "WH-DST", Name = "Dest Facility", IsActive = true };
        context.Warehouses.AddRange(whSource, whDest);

        var product = new Product
        {
            Sku = "TRF-PROD-01",
            Name = "Transfer Product",
            CategoryId = category.Id,
            UnitCost = 50m,
            QuantityInStock = 100,
            IsActive = true
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var sourceStock = new WarehouseStock
        {
            WarehouseId = whSource.Id,
            ProductId = product.Id,
            QuantityOnHand = 40,
            QuantityReserved = 0,
            BinLocation = "A-01"
        };
        context.WarehouseStocks.Add(sourceStock);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<TransferService>>();
        var service = new TransferService(context, loggerMock.Object);

        var dto = new CreateStockTransferDto
        {
            SourceWarehouseId = whSource.Id,
            DestinationWarehouseId = whDest.Id,
            RequestedBy = "dispatcher_bob",
            Notes = "Urgent stock rebalance",
            Items = new List<CreateStockTransferItemDto>
            {
                new() { ProductId = product.Id, Quantity = 15 }
            }
        };

        // Act
        var result = await service.CreateTransferAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StockTransferStatus.Pending, result.Status);
        Assert.Equal(15, result.TotalItemsRequested);

        // Verify stock reservation in source warehouse
        var updatedSourceStock = await context.WarehouseStocks.FindAsync(sourceStock.Id);
        Assert.NotNull(updatedSourceStock);
        Assert.Equal(40, updatedSourceStock.QuantityOnHand);
        Assert.Equal(15, updatedSourceStock.QuantityReserved);
        Assert.Equal(25, updatedSourceStock.AvailableQuantity);
    }

    [Fact]
    public async Task CreateTransferAsync_SameSourceAndDest_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_CreateTransfer_SameWarehouse");
        var wh = new Warehouse { Code = "WH-SAME", Name = "Same Facility", IsActive = true };
        context.Warehouses.Add(wh);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<TransferService>>();
        var service = new TransferService(context, loggerMock.Object);

        var dto = new CreateStockTransferDto
        {
            SourceWarehouseId = wh.Id,
            DestinationWarehouseId = wh.Id,
            Items = new List<CreateStockTransferItemDto> { new() { ProductId = 1, Quantity = 5 } }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateTransferAsync(dto));
    }

    [Fact]
    public async Task CreateTransferAsync_InsufficientAvailableStock_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_CreateTransfer_Insufficient");
        var category = new Category { Name = "General" };
        context.Categories.Add(category);

        var whSource = new Warehouse { Code = "WH-S", Name = "Source Facility", IsActive = true };
        var whDest = new Warehouse { Code = "WH-D", Name = "Dest Facility", IsActive = true };
        context.Warehouses.AddRange(whSource, whDest);

        var product = new Product { Sku = "LOW-AVAIL", Name = "Item", CategoryId = category.Id, IsActive = true };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Source has 10 on hand, but 8 are already reserved (available = 2)
        context.WarehouseStocks.Add(new WarehouseStock
        {
            WarehouseId = whSource.Id,
            ProductId = product.Id,
            QuantityOnHand = 10,
            QuantityReserved = 8
        });
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<TransferService>>();
        var service = new TransferService(context, loggerMock.Object);

        var dto = new CreateStockTransferDto
        {
            SourceWarehouseId = whSource.Id,
            DestinationWarehouseId = whDest.Id,
            Items = new List<CreateStockTransferItemDto> { new() { ProductId = product.Id, Quantity = 5 } } // Exceeds 2 available
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateTransferAsync(dto));
    }

    [Fact]
    public async Task ShipTransferAsync_DeductsSourceStockAndSetsInTransit()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_ShipTransfer");
        var category = new Category { Name = "General" };
        context.Categories.Add(category);

        var whSource = new Warehouse { Code = "WH-SHIP-SRC", Name = "Source", IsActive = true };
        var whDest = new Warehouse { Code = "WH-SHIP-DST", Name = "Dest", IsActive = true };
        context.Warehouses.AddRange(whSource, whDest);

        var product = new Product { Sku = "PROD-SHIP", Name = "Ship Item", CategoryId = category.Id, UnitCost = 20m, IsActive = true };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var sourceStock = new WarehouseStock
        {
            WarehouseId = whSource.Id,
            ProductId = product.Id,
            QuantityOnHand = 50,
            QuantityReserved = 20
        };
        context.WarehouseStocks.Add(sourceStock);

        var transfer = new StockTransfer
        {
            TransferNumber = "TRF-SHIP-001",
            SourceWarehouseId = whSource.Id,
            DestinationWarehouseId = whDest.Id,
            Status = StockTransferStatus.Pending,
            Items = new List<StockTransferItem>
            {
                new() { ProductId = product.Id, QuantityRequested = 20, UnitCost = 20m }
            }
        };
        context.StockTransfers.Add(transfer);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<TransferService>>();
        var service = new TransferService(context, loggerMock.Object);

        // Act
        var result = await service.ShipTransferAsync(transfer.Id, new ShipTransferDto { TrackingNumber = "TRACK-123" });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StockTransferStatus.InTransit, result.Status);
        Assert.Equal("TRACK-123", result.TrackingNumber);
        Assert.NotNull(result.ShippedAtUtc);

        var updatedStock = await context.WarehouseStocks.FindAsync(sourceStock.Id);
        Assert.NotNull(updatedStock);
        Assert.Equal(30, updatedStock.QuantityOnHand); // 50 - 20 = 30
        Assert.Equal(0, updatedStock.QuantityReserved); // Released reservation

        var tx = await context.InventoryTransactions.FirstOrDefaultAsync(t => t.ReferenceNumber == "TRF-SHIP-001");
        Assert.NotNull(tx);
        Assert.Equal(TransactionType.StockOut, tx.Type);
        Assert.Equal(-20, tx.QuantityChange);
    }

    [Fact]
    public async Task ReceiveTransferAsync_AddsDestinationStockAndSetsReceived()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_ReceiveTransfer");
        var category = new Category { Name = "General" };
        context.Categories.Add(category);

        var whSource = new Warehouse { Code = "WH-REC-SRC", Name = "Source", IsActive = true };
        var whDest = new Warehouse { Code = "WH-REC-DST", Name = "Dest", IsActive = true };
        context.Warehouses.AddRange(whSource, whDest);

        var product = new Product { Sku = "PROD-REC", Name = "Receive Item", CategoryId = category.Id, UnitCost = 15m, IsActive = true };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var transfer = new StockTransfer
        {
            TransferNumber = "TRF-REC-001",
            SourceWarehouseId = whSource.Id,
            DestinationWarehouseId = whDest.Id,
            Status = StockTransferStatus.InTransit,
            Items = new List<StockTransferItem>
            {
                new() { ProductId = product.Id, QuantityRequested = 10, QuantityShipped = 10, UnitCost = 15m }
            }
        };
        context.StockTransfers.Add(transfer);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<TransferService>>();
        var service = new TransferService(context, loggerMock.Object);

        // Act
        var result = await service.ReceiveTransferAsync(transfer.Id, new ReceiveTransferDto { Notes = "Verified dock intake" });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StockTransferStatus.Received, result.Status);
        Assert.NotNull(result.ReceivedAtUtc);

        var destStock = await context.WarehouseStocks.FirstOrDefaultAsync(ws => ws.WarehouseId == whDest.Id && ws.ProductId == product.Id);
        Assert.NotNull(destStock);
        Assert.Equal(10, destStock.QuantityOnHand);

        var tx = await context.InventoryTransactions.FirstOrDefaultAsync(t => t.ReferenceNumber == "TRF-REC-001");
        Assert.NotNull(tx);
        Assert.Equal(TransactionType.StockIn, tx.Type);
        Assert.Equal(10, tx.QuantityChange);
    }

    [Fact]
    public async Task CancelTransferAsync_ReleasesSourceReservation()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_CancelTransfer");
        var category = new Category { Name = "General" };
        context.Categories.Add(category);

        var whSource = new Warehouse { Code = "WH-CAN-SRC", Name = "Source", IsActive = true };
        var whDest = new Warehouse { Code = "WH-CAN-DST", Name = "Dest", IsActive = true };
        context.Warehouses.AddRange(whSource, whDest);

        var product = new Product { Sku = "PROD-CAN", Name = "Cancel Item", CategoryId = category.Id, IsActive = true };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var sourceStock = new WarehouseStock
        {
            WarehouseId = whSource.Id,
            ProductId = product.Id,
            QuantityOnHand = 50,
            QuantityReserved = 25
        };
        context.WarehouseStocks.Add(sourceStock);

        var transfer = new StockTransfer
        {
            TransferNumber = "TRF-CAN-001",
            SourceWarehouseId = whSource.Id,
            DestinationWarehouseId = whDest.Id,
            Status = StockTransferStatus.Pending,
            Items = new List<StockTransferItem>
            {
                new() { ProductId = product.Id, QuantityRequested = 25 }
            }
        };
        context.StockTransfers.Add(transfer);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<TransferService>>();
        var service = new TransferService(context, loggerMock.Object);

        // Act
        var result = await service.CancelTransferAsync(transfer.Id, "Order mistake");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(StockTransferStatus.Cancelled, result.Status);

        var updatedStock = await context.WarehouseStocks.FindAsync(sourceStock.Id);
        Assert.NotNull(updatedStock);
        Assert.Equal(0, updatedStock.QuantityReserved); // Released
        Assert.Equal(50, updatedStock.QuantityOnHand); // Unchanged
    }
}
