// src/InventoryTracker.Api/Models/WebhookSubscription.cs
// Represents an external webhook listener endpoint registered for event broadcasts.
// Connects to: src/InventoryTracker.Api/Models/WebhookDeliveryLog.cs, src/InventoryTracker.Api/Data/InventoryDbContext.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// Domain entity representing an outbound webhook HTTP listener subscription.
/// </summary>
public class WebhookSubscription
{
    /// <summary>
    /// Unique database primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Descriptive name of the receiving service (e.g. Slack Operations Alert, NetSuite ERP Sync).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Target HTTPS URL endpoint receiving webhook POST payloads.
    /// </summary>
    public string TargetUrl { get; set; } = string.Empty;

    /// <summary>
    /// Shared secret key used to compute HMAC-SHA256 request signatures.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated list of subscribed WebhookEventType names or "*" for all.
    /// </summary>
    public string SubscribedEvents { get; set; } = "*";

    /// <summary>
    /// Indicates whether the subscription is actively dispatched.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Consecutive delivery failure counter.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Timestamp when subscription was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp of most recent dispatch attempt.
    /// </summary>
    public DateTime? LastTriggeredAtUtc { get; set; }

    /// <summary>
    /// Navigation collection of delivery attempts for this subscription.
    /// </summary>
    public ICollection<WebhookDeliveryLog> DeliveryLogs { get; set; } = new List<WebhookDeliveryLog>();
}
