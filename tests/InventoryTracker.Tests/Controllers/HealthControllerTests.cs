// tests/InventoryTracker.Tests/Controllers/HealthControllerTests.cs
// Unit tests for HealthController health check endpoints and response verification.
// Connects to: src/InventoryTracker.Api/Controllers/HealthController.cs
// Created: 2026-08-26

using InventoryTracker.Api.Controllers;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryTracker.Tests.Controllers;

public class HealthControllerTests
{
    [Fact]
    public async Task GetHealth_ReturnsOkWithHealthyPayload()
    {
        // Arrange
        var mockAnalytics = new Mock<IAnalyticsService>();
        var healthDto = new HealthStatusDto
        {
            Status = "Healthy",
            Service = "InventoryTracker.Api",
            DatabaseStatus = "Connected"
        };

        mockAnalytics.Setup(a => a.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthDto);

        var controller = new HealthController(mockAnalytics.Object);

        // Act
        var result = await controller.GetHealth(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<HealthStatusDto>>(okResult.Value);
        Assert.True(envelope.Success);
        Assert.Equal("Healthy", envelope.Data?.Status);
        Assert.Equal("Connected", envelope.Data?.DatabaseStatus);
    }
}
