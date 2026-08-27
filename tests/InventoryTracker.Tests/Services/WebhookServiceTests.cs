// tests/InventoryTracker.Tests/Services/WebhookServiceTests.cs
// Unit tests for WebhookService HMAC-SHA256 signature calculations, subscription lifecycle, and event filtering.
// Connects to: src/InventoryTracker.Api/Services/WebhookService.cs
// Created: 2026-08-27

using System.Net;
using InventoryTracker.Api.Data;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using InventoryTracker.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace InventoryTracker.Tests.Services;

public class WebhookServiceTests
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
    public void ComputeHmacSha256_ValidPayloadAndSecret_ReturnsConsistentHexHash()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_Webhook_Hmac");
        var httpClient = new HttpClient();
        var loggerMock = new Mock<ILogger<WebhookService>>();
        var service = new WebhookService(context, httpClient, loggerMock.Object);

        var payload = "{\"event\":\"StockLow\",\"sku\":\"ELEC-MON-4K27\"}";
        var secret = "super_secret_test_key_12345";

        // Act
        var hash1 = service.ComputeHmacSha256(payload, secret);
        var hash2 = service.ComputeHmacSha256(payload, secret);

        // Assert
        Assert.NotEmpty(hash1);
        Assert.Equal(64, hash1.Length); // 256 bits = 64 hex characters
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_ValidDto_SavesSubscription()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_Webhook_Create");
        var httpClient = new HttpClient();
        var loggerMock = new Mock<ILogger<WebhookService>>();
        var service = new WebhookService(context, httpClient, loggerMock.Object);

        var dto = new CreateWebhookSubscriptionDto
        {
            Name = "Slack ERP Alert Channel",
            TargetUrl = "https://hooks.slack.com/services/T00/B00/X00",
            SecretKey = "custom_shared_secret_key",
            SubscribedEvents = "StockLow,StockOut"
        };

        // Act
        var result = await service.CreateSubscriptionAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Slack ERP Alert Channel", result.Name);
        Assert.Equal("https://hooks.slack.com/services/T00/B00/X00", result.TargetUrl);
        Assert.Equal("StockLow,StockOut", result.SubscribedEvents);
        Assert.True(result.IsActive);

        var saved = await context.WebhookSubscriptions.FirstOrDefaultAsync(s => s.Name == "Slack ERP Alert Channel");
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task PublishEventAsync_DispatchesHttpRequestAndLogsDelivery()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("TestDb_Webhook_Publish");

        var sub = new WebhookSubscription
        {
            Name = "Receiver Hook",
            TargetUrl = "https://webhook.site/test-endpoint",
            SecretKey = "test_secret_key_123",
            SubscribedEvents = "*",
            IsActive = true
        };
        context.WebhookSubscriptions.Add(sub);
        await context.SaveChangesAsync();

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"status\":\"received\"}")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var loggerMock = new Mock<ILogger<WebhookService>>();
        var service = new WebhookService(context, httpClient, loggerMock.Object);

        // Act
        await service.PublishEventAsync(WebhookEventType.StockLow, new { Sku = "ELEC-LOW-01", StockOnHand = 3 });

        // Assert
        var deliveryLog = await context.WebhookDeliveryLogs.FirstOrDefaultAsync();
        Assert.NotNull(deliveryLog);
        Assert.Equal(200, deliveryLog.ResponseStatusCode);
        Assert.True(deliveryLog.IsSuccess);
        Assert.Equal(WebhookEventType.StockLow, deliveryLog.EventType);
    }
}
