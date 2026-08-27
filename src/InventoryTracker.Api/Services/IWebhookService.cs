// src/InventoryTracker.Api/Services/IWebhookService.cs
// Defines service contracts for webhook subscription management, HMAC signing, and event delivery.
// Connects to: src/InventoryTracker.Api/Services/WebhookService.cs, src/InventoryTracker.Api/Controllers/WebhooksController.cs
// Created: 2026-08-27

using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service contract for outbound webhook subscription management, HMAC signature computation, and event publishing.
/// </summary>
public interface IWebhookService
{
    Task<IReadOnlyList<WebhookSubscriptionDto>> GetSubscriptionsAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<WebhookSubscriptionDto?> GetSubscriptionByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WebhookSubscriptionDto> CreateSubscriptionAsync(CreateWebhookSubscriptionDto dto, CancellationToken cancellationToken = default);
    Task<WebhookSubscriptionDto?> UpdateSubscriptionAsync(int id, UpdateWebhookSubscriptionDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteSubscriptionAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WebhookDeliveryLogDto>> GetDeliveryLogsAsync(int subscriptionId, int limit = 50, CancellationToken cancellationToken = default);
    Task<WebhookTestResultDto> TestWebhookAsync(int subscriptionId, CancellationToken cancellationToken = default);
    Task PublishEventAsync(WebhookEventType eventType, object eventData, CancellationToken cancellationToken = default);
    string ComputeHmacSha256(string payload, string secretKey);
}
