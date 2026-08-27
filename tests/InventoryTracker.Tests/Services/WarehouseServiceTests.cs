// tests/InventoryTracker.Tests/Services/WarehouseServiceTests.cs
// Unit tests for WarehouseService facility registration, capacity calculations, and bin coordinates.
// Connects to: src/InventoryTracker.Api/Services/WarehouseService.cs
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

public class WarehouseServiceTests
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
    public async Task CreateWarehouseAsync_ValidData_CreatesFacility()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_CreateWarehouse_Valid");
        var loggerMock = new Mock<ILogger<WarehouseService>>();
        var service = new WarehouseService(context, loggerMock.Object);

        var dto = new CreateWarehouseDto
        {
            Code = "WH-NORTH",
            Name = "Chicago Logistics Hub",
            Address = "100 Industrial Parkway",
            City = "Chicago",
            State = "IL",
            PostalCode = "60601",
            CapacityUnits = 15000
        };

        // Act
        var result = await service.CreateWarehouseAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("WH-NORTH", result.Code);
        Assert.Equal("Chicago Logistics Hub", result.Name);
        Assert.Equal(15000, result.CapacityUnits);
        Assert.True(result.IsActive);

        var saved = await context.Warehouses.FirstOrDefaultAsync(w => w.Code == "WH-NORTH");
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task CreateWarehouseAsync_DuplicateCode_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_CreateWarehouse_Duplicate");
        context.Warehouses.Add(new Warehouse
        {
            Code = "WH-SOUTH",
            Name = "Miami Hub",
            CapacityUnits = 5000
        });
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<WarehouseService>>();
        var service = new WarehouseService(context, loggerMock.Object);

        var dto = new CreateWarehouseDto
        {
            Code = "wh-south", // Case insensitive match
            Name = "Miami Hub Duplicate",
            CapacityUnits = 10000
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateWarehouseAsync(dto));
    }

    [Fact]
    public async Task SetBinLocationAsync_UpdatesCoordinateSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_SetBinLocation");
        var category = new Category { Name = "General" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var warehouse = new Warehouse { Code = "WH-1", Name = "Facility 1" };
        var product = new Product { Sku = "SKU-BIN-01", Name = "Bin Item", CategoryId = category.Id };
        context.Warehouses.Add(warehouse);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<WarehouseService>>();
        var service = new WarehouseService(context, loggerMock.Object);

        // Act
        var result = await service.SetBinLocationAsync(warehouse.Id, product.Id, "A-04-12");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("A-04-12", result.BinLocation);

        var savedStock = await context.WarehouseStocks.FirstOrDefaultAsync(ws => ws.WarehouseId == warehouse.Id && ws.ProductId == product.Id);
        Assert.NotNull(savedStock);
        Assert.Equal("A-04-12", savedStock.BinLocation);
    }
}
