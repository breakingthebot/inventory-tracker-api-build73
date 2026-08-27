// tests/InventoryTracker.Tests/Controllers/CycleCountsControllerTests.cs
// Unit tests for CycleCountsController REST action endpoints and variance reports.
// Connects to: src/InventoryTracker.Api/Controllers/CycleCountsController.cs
// Created: 2026-08-27

using InventoryTracker.Api.Controllers;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryTracker.Tests.Controllers;

public class CycleCountsControllerTests
{
    [Fact]
    public async Task CreateCycleCount_ValidDto_ReturnsCreatedAtAction()
    {
        // Arrange
        var mockService = new Mock<ICycleCountService>();
        var createDto = new CreateCycleCountDto { WarehouseId = 1, Scope = "FullWarehouse" };
        var createdDto = new CycleCountDto
        {
            Id = 1,
            CountNumber = "CC-20260827-001",
            WarehouseId = 1,
            Status = CycleCountStatus.InProgress
        };

        mockService.Setup(s => s.CreateCycleCountAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        var controller = new CycleCountsController(mockService.Object);

        // Act
        var result = await controller.CreateCycleCount(createDto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var envelope = Assert.IsType<ApiResponse<CycleCountDto>>(createdResult.Value);
        Assert.True(envelope.Success);
        Assert.Equal("CC-20260827-001", envelope.Data?.CountNumber);
    }

    [Fact]
    public async Task GetVarianceReport_ExistingId_ReturnsOkWithReport()
    {
        // Arrange
        var mockService = new Mock<ICycleCountService>();
        var reportDto = new CycleCountVarianceReportDto
        {
            CycleCountId = 1,
            CountNumber = "CC-20260827-001",
            TotalLinesAudited = 10,
            TotalLinesWithDiscrepancy = 1,
            NetUnitVariance = -2,
            InventoryAccuracyRate = 90.00m
        };

        mockService.Setup(s => s.GetVarianceReportAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reportDto);

        var controller = new CycleCountsController(mockService.Object);

        // Act
        var result = await controller.GetVarianceReport(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<CycleCountVarianceReportDto>>(okResult.Value);
        Assert.True(envelope.Success);
        Assert.Equal(90.00m, envelope.Data?.InventoryAccuracyRate);
    }
}
