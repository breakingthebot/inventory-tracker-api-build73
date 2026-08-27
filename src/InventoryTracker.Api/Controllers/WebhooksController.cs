// src/InventoryTracker.Api/Controllers/WebhooksController.cs
// REST controller for webhook subscription management, delivery log audits, and endpoint test pings.
// Connects to: src/InventoryTracker.Api/Services/IWebhookService.cs, src/InventoryTracker.Api/DTOs/WebhookDtos.cs
// Created: 2026-08-27

using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryTracker.Api.Controllers;

/// <summary>
/// Manages outbound webhook event subscriptions, delivery audit history, and endpoint verification.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;

    public WebhooksController(IWebhookService webhookService)
    {
        _webhookService = webhookService;
    }

    /// <summary>
    /// Retrieves all registered webhook subscriptions.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WebhookSubscriptionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptions([FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var subs = await _webhookService.GetSubscriptionsAsync(activeOnly, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<WebhookSubscriptionDto>>.Ok(subs, $"Retrieved {subs.Count} webhook subscriptions."));
    }

    /// <summary>
    /// Retrieves a single webhook subscription by its ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<WebhookSubscriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscriptionById(int id, CancellationToken cancellationToken)
    {
        var sub = await _webhookService.GetSubscriptionByIdAsync(id, cancellationToken);
        if (sub == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Webhook subscription with ID {id} was not found."));
        }

        return Ok(ApiResponse<WebhookSubscriptionDto>.Ok(sub));
    }

    /// <summary>
    /// Registers a new webhook subscription for real-time inventory notifications.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<WebhookSubscriptionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateWebhookSubscriptionDto dto, CancellationToken cancellationToken)
    {
        var created = await _webhookService.CreateSubscriptionAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetSubscriptionById), new { id = created.Id },
            ApiResponse<WebhookSubscriptionDto>.Ok(created, "Webhook subscription registered successfully."));
    }

    /// <summary>
    /// Updates an existing webhook subscription URL or subscribed events.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<WebhookSubscriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSubscription(int id, [FromBody] UpdateWebhookSubscriptionDto dto, CancellationToken cancellationToken)
    {
        var updated = await _webhookService.UpdateSubscriptionAsync(id, dto, cancellationToken);
        if (updated == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Webhook subscription with ID {id} was not found."));
        }

        return Ok(ApiResponse<WebhookSubscriptionDto>.Ok(updated, "Webhook subscription updated successfully."));
    }

    /// <summary>
    /// Removes a webhook subscription.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSubscription(int id, CancellationToken cancellationToken)
    {
        var deleted = await _webhookService.DeleteSubscriptionAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(ApiResponse<object>.Fail($"Webhook subscription with ID {id} was not found."));
        }

        return Ok(ApiResponse<object>.Ok(new { id }, "Webhook subscription removed successfully."));
    }

    /// <summary>
    /// Retrieves recent delivery logs and HTTP response status codes for a specific webhook.
    /// </summary>
    [HttpGet("{id:int}/deliveries")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WebhookDeliveryLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeliveryLogs(int id, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var logs = await _webhookService.GetDeliveryLogsAsync(id, limit, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<WebhookDeliveryLogDto>>.Ok(logs, $"Retrieved {logs.Count} delivery log entries."));
    }

    /// <summary>
    /// Sends a live test ping payload with HMAC signature to verify target endpoint connectivity.
    /// </summary>
    [HttpPost("{id:int}/test")]
    [ProducesResponseType(typeof(ApiResponse<WebhookTestResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TestWebhook(int id, CancellationToken cancellationToken)
    {
        var result = await _webhookService.TestWebhookAsync(id, cancellationToken);
        return Ok(ApiResponse<WebhookTestResultDto>.Ok(result, result.Success ? "Webhook ping succeeded." : "Webhook ping encountered errors."));
    }
}
