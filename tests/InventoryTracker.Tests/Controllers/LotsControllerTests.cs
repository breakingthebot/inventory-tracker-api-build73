// tests/InventoryTracker.Tests/Controllers/LotsControllerTests.cs
// Unit tests for LotsController REST action endpoints and FEFO plan computation.
// Connects to: src/InventoryTracker.Api/Controllers/LotsController.cs
// Created: 2026-08-27

using InventoryTracker.Api.Controllers;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryTracker.Tests.Controllers;

public class LotsControllerTests
{
    [Fact]
    public async Task GetExpiringLots_ReturnsOkWithExpiringReport()
    {
        // Arrange
        var mockService = new Mock<ILotTrackingService>();
        var summaryDto = new ExpiringLotsSummaryDto
        {
            TotalExpiringLotsCount = 2,
            TotalExpiringUnits = 50,
            EstimatedAtRiskValuation = 500m
        };

        mockService.Setup(s => s.GetExpiringLotsAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaryDto);

        var controller = new LotsController(mockService.Object);

        // Act
        var result = await controller.GetExpiringLots(30, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<ExpiringLotsSummaryDto>>(okResult.Value);
        Assert.True(envelope.Success);
        Assert.Equal(2, envelope.Data?.TotalExpiringLotsCount);
    }

    [Fact]
    public async Task GetFefoPlan_ValidParameters_ReturnsOkWithPlan()
    {
        // Arrange
        var mockService = new Mock<ILotTrackingService>();
        var planDto = new FefoAllocationPlanDto
        {
            ProductId = 1,
            ProductSku = "SKU-01",
            WarehouseId = 1,
            RequestedQuantity = 10,
            TotalAllocatedQuantity = 10
        };

        mockService.Setup(s => s.GetFefoAllocationPlanAsync(1, 10, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(planDto);

        var controller = new LotsController(mockService.Object);

        // Act
        var result = await controller.GetFefoPlan(1, 10, 1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<FefoAllocationPlanDto>>(okResult.Value);
        Assert.True(envelope.Success);
        Assert.True(envelope.Data?.IsFullyAllocated);
    }
}
