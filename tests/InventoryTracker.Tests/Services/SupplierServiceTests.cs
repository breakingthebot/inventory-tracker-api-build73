// tests/InventoryTracker.Tests/Services/SupplierServiceTests.cs
// Unit tests for SupplierService vendor registration, unique code validation, and directory updates.
// Connects to: src/InventoryTracker.Api/Services/SupplierService.cs
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

public class SupplierServiceTests
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
    public async Task CreateSupplierAsync_ValidPayload_CreatesSupplierRecord()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_CreateSupplier_Valid");
        var loggerMock = new Mock<ILogger<SupplierService>>();
        var service = new SupplierService(context, loggerMock.Object);

        var dto = new CreateSupplierDto
        {
            Code = "SUP-LOGIC",
            Name = "Logic Distribution Services",
            ContactName = "Alice Wang",
            Email = "orders@logicdistribution.com",
            Phone = "+1-555-0123",
            LeadTimeDays = 5,
            PaymentTerms = "Net 30"
        };

        // Act
        var result = await service.CreateSupplierAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SUP-LOGIC", result.Code);
        Assert.Equal("Logic Distribution Services", result.Name);
        Assert.Equal(5, result.LeadTimeDays);

        var saved = await context.Suppliers.FirstOrDefaultAsync(s => s.Code == "SUP-LOGIC");
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task CreateSupplierAsync_DuplicateCode_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_CreateSupplier_Duplicate");
        context.Suppliers.Add(new Supplier
        {
            Code = "SUP-DUP",
            Name = "Original Vendor",
            Email = "orig@vendor.com"
        });
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<SupplierService>>();
        var service = new SupplierService(context, loggerMock.Object);

        var dto = new CreateSupplierDto
        {
            Code = "sup-dup", // Case-insensitive duplicate
            Name = "Duplicate Vendor",
            Email = "dup@vendor.com"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateSupplierAsync(dto));
    }
}
