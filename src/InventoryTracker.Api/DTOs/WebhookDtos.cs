// src/InventoryTracker.Api/DTOs/WebhookDtos.cs
// Data Transfer Objects for webhook subscriptions, delivery logs, and event payloads.
// Connects to: src/InventoryTracker.Api/Services/IWebhookService.cs, src/InventoryTracker.Api/Controllers/WebhooksController.cs
// Created: 2026-08-27

using System.ComponentModel.DataAnnotations;
using InventoryTracker.Api.Models;

namespace InventoryTracker.Api.DTOs;

/// <summary>
/// Data contract returned for registered webhook subscriptions.
/// </summary>
public class WebhookSubscriptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public string SubscribedEvents { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int FailureCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastTriggeredAtUtc { get; set; }
}

/// <summary>
/// Request payload to register a new webhook subscription.
/// </summary>
public class CreateWebhookSubscriptionDto
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "TargetUrl is required.")]
    [Url(ErrorMessage = "TargetUrl must be a valid URL.")]
    [StringLength(300, ErrorMessage = "TargetUrl cannot exceed 300 characters.")]
    public string TargetUrl { get; set; } = string.Empty;

    [StringLength(100, MinimumLength = 8, ErrorMessage = "SecretKey must be at least 8 characters.")]
    public string? SecretKey { get; set; }

    public string SubscribedEvents { get; set; } = "*";
}

/// <summary>
/// Request payload to update an existing webhook subscription.
/// </summary>
public class UpdateWebhookSubscriptionDto
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "TargetUrl is required.")]
    [Url(ErrorMessage = "TargetUrl must be a valid URL.")]
    [StringLength(300, ErrorMessage = "TargetUrl cannot exceed 300 characters.")]
    public string TargetUrl { get; set; } = string.Empty;

    public string SubscribedEvents { get; set; } = "*";

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Data contract returned for webhook delivery attempt audit logs.
/// </summary>
public class WebhookDeliveryLogDto
{
    public int Id { get; set; }
    public int WebhookSubscriptionId { get; set; }
    public WebhookEventType EventType { get; set; }
    public string EventTypeName => EventType.ToString();
    public string PayloadJson { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
    public DateTime TimestampUtc { get; set; }
}

/// <summary>
/// Standard envelope delivered to webhook receiver endpoints.
/// </summary>
public class WebhookEventPayload<T>
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public T Data { get; set; } = default!;
}

/// <summary>
/// Result returned when sending a test ping to a webhook subscription.
/// </summary>
public class WebhookTestResultDto
{
    public int SubscriptionId { get; set; }
    public string TargetUrl { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public long DurationMs { get; set; }
}
