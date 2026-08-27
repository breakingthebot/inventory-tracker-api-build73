// tests/InventoryTracker.Tests/Controllers/WebhooksControllerTests.cs
// Unit tests for WebhooksController REST endpoints and test ping actions.
// Connects to: src/InventoryTracker.Api/Controllers/WebhooksController.cs
// Created: 2026-08-27

using InventoryTracker.Api.Controllers;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryTracker.Tests.Controllers;

public class WebhooksControllerTests
{
    [Fact]
    public async Task CreateSubscription_ValidPayload_ReturnsCreatedAtAction()
    {
        // Arrange
        var mockService = new Mock<IWebhookService>();
        var createDto = new CreateWebhookSubscriptionDto
        {
            Name = "ERP Integration",
            TargetUrl = "https://erp.enterprise.com/webhooks"
        };
        var subDto = new WebhookSubscriptionDto
        {
            Id = 1,
            Name = "ERP Integration",
            TargetUrl = "https://erp.enterprise.com/webhooks",
            IsActive = true
        };

        mockService.Setup(s => s.CreateSubscriptionAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subDto);

        var controller = new WebhooksController(mockService.Object);

        // Act
        var result = await controller.CreateSubscription(createDto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var envelope = Assert.IsType<ApiResponse<WebhookSubscriptionDto>>(createdResult.Value);
        Assert.True(envelope.Success);
        Assert.Equal("ERP Integration", envelope.Data?.Name);
    }

    [Fact]
    public async Task TestWebhook_ExistingSubscription_ReturnsOkWithTestResult()
    {
        // Arrange
        var mockService = new Mock<IWebhookService>();
        var testResult = new WebhookTestResultDto
        {
            SubscriptionId = 1,
            TargetUrl = "https://test.com/hook",
            Success = true,
            StatusCode = 200,
            Message = "Webhook responded successfully."
        };

        mockService.Setup(s => s.TestWebhookAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testResult);

        var controller = new WebhooksController(mockService.Object);

        // Act
        var result = await controller.TestWebhook(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<WebhookTestResultDto>>(okResult.Value);
        Assert.True(envelope.Success);
        Assert.True(envelope.Data?.Success);
        Assert.Equal(200, envelope.Data?.StatusCode);
    }
}
