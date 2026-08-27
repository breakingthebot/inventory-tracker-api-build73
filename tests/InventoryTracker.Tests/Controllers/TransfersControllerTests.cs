// tests/InventoryTracker.Tests/Controllers/TransfersControllerTests.cs
// Unit tests for TransfersController REST action endpoints and status responses.
// Connects to: src/InventoryTracker.Api/Controllers/TransfersController.cs
// Created: 2026-08-27

using InventoryTracker.Api.Controllers;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryTracker.Tests.Controllers;

public class TransfersControllerTests
{
    [Fact]
    public async Task CreateTransfer_ValidPayload_ReturnsCreatedAtAction()
    {
        // Arrange
        var mockService = new Mock<ITransferService>();
        var createDto = new CreateStockTransferDto
        {
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            Items = new List<CreateStockTransferItemDto> { new() { ProductId = 1, Quantity = 10 } }
        };
        var transferDto = new StockTransferDto
        {
            Id = 100,
            TransferNumber = "TRF-2026-0100",
            SourceWarehouseId = 1,
            DestinationWarehouseId = 2,
            Status = StockTransferStatus.Pending
        };

        mockService.Setup(s => s.CreateTransferAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transferDto);

        var controller = new TransfersController(mockService.Object);

        // Act
        var result = await controller.CreateTransfer(createDto, CancellationToken.None);

        // Assert
        var createdAtResult = Assert.IsType<CreatedAtActionResult>(result);
        var envelope = Assert.IsType<ApiResponse<StockTransferDto>>(createdAtResult.Value);
        Assert.True(envelope.Success);
        Assert.Equal(100, envelope.Data?.Id);
    }

    [Fact]
    public async Task ShipTransfer_ExistingTransfer_ReturnsOk()
    {
        // Arrange
        var mockService = new Mock<ITransferService>();
        var shipDto = new ShipTransferDto { TrackingNumber = "TRACK-999" };
        var transferDto = new StockTransferDto
        {
            Id = 100,
            TransferNumber = "TRF-2026-0100",
            Status = StockTransferStatus.InTransit,
            TrackingNumber = "TRACK-999"
        };

        mockService.Setup(s => s.ShipTransferAsync(100, shipDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transferDto);

        var controller = new TransfersController(mockService.Object);

        // Act
        var result = await controller.ShipTransfer(100, shipDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<StockTransferDto>>(okResult.Value);
        Assert.True(envelope.Success);
        Assert.Equal(StockTransferStatus.InTransit, envelope.Data?.Status);
    }
}
