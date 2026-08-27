// src/InventoryTracker.Api/Models/WebhookDeliveryLog.cs
// Audit entity recording individual webhook dispatch attempts, HTTP status codes, and payloads.
// Connects to: src/InventoryTracker.Api/Models/WebhookSubscription.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// Domain entity logging outbound webhook HTTP delivery attempts and responses.
/// </summary>
public class WebhookDeliveryLog
{
    /// <summary>
    /// Unique database primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key referencing the webhook subscription.
    /// </summary>
    public int WebhookSubscriptionId { get; set; }

    /// <summary>
    /// Navigation reference to the parent subscription.
    /// </summary>
    public WebhookSubscription? WebhookSubscription { get; set; }

    /// <summary>
    /// Domain event type dispatched.
    /// </summary>
    public WebhookEventType EventType { get; set; }

    /// <summary>
    /// Serialized JSON payload delivered in the request body.
    /// </summary>
    public string PayloadJson { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code returned by target server (e.g. 200, 500, 0 if network error).
    /// </summary>
    public int ResponseStatusCode { get; set; }

    /// <summary>
    /// Indicates whether the target endpoint accepted the event (2xx code).
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message captured if delivery failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Execution duration in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Dispatch timestamp in UTC.
    /// </summary>
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
