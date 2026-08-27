// tests/InventoryTracker.Tests/Controllers/SalesOrdersControllerTests.cs
// Unit tests for SalesOrdersController REST action endpoints and fulfillment pipeline.
// Connects to: src/InventoryTracker.Api/Controllers/SalesOrdersController.cs
// Created: 2026-08-27

using InventoryTracker.Api.Controllers;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryTracker.Tests.Controllers;

public class SalesOrdersControllerTests
{
    [Fact]
    public async Task CreateSalesOrder_ValidDto_ReturnsCreatedAtAction()
    {
        // Arrange
        var mockService = new Mock<ISalesOrderService>();
        var createDto = new CreateSalesOrderDto
        {
            CustomerId = 1,
            WarehouseId = 1,
            Items = new List<CreateSalesOrderItemDto> { new() { ProductId = 1, QuantityOrdered = 2 } }
        };
        var orderDto = new SalesOrderDto
        {
            Id = 1,
            OrderNumber = "SO-20260827-0001",
            Status = SalesOrderStatus.Draft,
            TotalAmount = 200m
        };

        mockService.Setup(s => s.CreateSalesOrderAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orderDto);

        var controller = new SalesOrdersController(mockService.Object);

        // Act
        var result = await controller.CreateSalesOrder(createDto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var envelope = Assert.IsType<ApiResponse<SalesOrderDto>>(createdResult.Value);
        Assert.True(envelope.Success);
        Assert.Equal("SO-20260827-0001", envelope.Data?.OrderNumber);
    }

    [Fact]
    public async Task AllocateOrder_ExistingId_ReturnsOkWithAllocatedOrder()
    {
        // Arrange
        var mockService = new Mock<ISalesOrderService>();
        var allocatedDto = new SalesOrderDto
        {
            Id = 1,
            OrderNumber = "SO-20260827-0001",
            Status = SalesOrderStatus.Allocated
        };

        mockService.Setup(s => s.AllocateOrderAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocatedDto);

        var controller = new SalesOrdersController(mockService.Object);

        // Act
        var result = await controller.AllocateOrder(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<SalesOrderDto>>(okResult.Value);
        Assert.True(envelope.Success);
        Assert.Equal(SalesOrderStatus.Allocated, envelope.Data?.Status);
    }
}
