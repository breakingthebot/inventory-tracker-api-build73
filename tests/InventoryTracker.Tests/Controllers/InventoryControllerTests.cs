// tests/InventoryTracker.Tests/Controllers/InventoryControllerTests.cs
// Unit tests for InventoryController REST endpoints handling adjustments, restock, and dispatch.
// Connects to: src/InventoryTracker.Api/Controllers/InventoryController.cs
// Created: 2026-08-26

using InventoryTracker.Api.Controllers;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryTracker.Tests.Controllers;

public class InventoryControllerTests
{
    [Fact]
    public async Task Restock_ValidRequest_ReturnsOkWithTransaction()
    {
        // Arrange
        var mockService = new Mock<IInventoryService>();
        var request = new RestockRequestDto
        {
            ProductId = 1,
            Quantity = 20,
            UnitCost = 15m,
            PurchaseOrderNumber = "PO-100"
        };
        var expectedTx = new TransactionDto
        {
            Id = 55,
            ProductId = 1,
            Type = TransactionType.StockIn,
            QuantityChange = 20,
            QuantityAfter = 70,
            ReferenceNumber = "PO-100"
        };

        mockService.Setup(s => s.RestockAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTx);

        var controller = new InventoryController(mockService.Object);

        // Act
        var result = await controller.Restock(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<TransactionDto>>(okResult.Value);
        Assert.True(envelope.Success);
        Assert.Equal(55, envelope.Data?.Id);
        Assert.Equal(20, envelope.Data?.QuantityChange);
    }

    [Fact]
    public async Task Dispatch_ValidRequest_ReturnsOkWithTransaction()
    {
        // Arrange
        var mockService = new Mock<IInventoryService>();
        var request = new DispatchRequestDto
        {
            ProductId = 1,
            Quantity = 5,
            SalesOrderNumber = "SO-200"
        };
        var expectedTx = new TransactionDto
        {
            Id = 56,
            ProductId = 1,
            Type = TransactionType.StockOut,
            QuantityChange = -5,
            QuantityAfter = 45,
            ReferenceNumber = "SO-200"
        };

        mockService.Setup(s => s.DispatchAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTx);

        var controller = new InventoryController(mockService.Object);

        // Act
        var result = await controller.Dispatch(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<TransactionDto>>(okResult.Value);
        Assert.True(envelope.Success);
        Assert.Equal(-5, envelope.Data?.QuantityChange);
    }
}
