// src/InventoryTracker.Api/Services/WebhookService.cs
// Implementation of HMAC-SHA256 event signing, HTTP webhook dispatching, and delivery logging.
// Connects to: src/InventoryTracker.Api/Data/InventoryDbContext.cs, src/InventoryTracker.Api/Models/WebhookSubscription.cs
// Created: 2026-08-27

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using InventoryTracker.Api.Data;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service dispatching event notifications to external webhook listener endpoints with HMAC signature validation.
/// </summary>
public class WebhookService : IWebhookService
{
    private readonly InventoryDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(InventoryDbContext context, HttpClient httpClient, ILogger<WebhookService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<WebhookSubscriptionDto>> GetSubscriptionsAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _context.WebhookSubscriptions.AsNoTracking().AsQueryable();
        if (activeOnly)
        {
            query = query.Where(s => s.IsActive);
        }

        var list = await query.OrderBy(s => s.Name).ToListAsync(cancellationToken);
        return list.Select(MapToDto).ToList();
    }

    public async Task<WebhookSubscriptionDto?> GetSubscriptionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var sub = await _context.WebhookSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return sub == null ? null : MapToDto(sub);
    }

    public async Task<WebhookSubscriptionDto> CreateSubscriptionAsync(CreateWebhookSubscriptionDto dto, CancellationToken cancellationToken = default)
    {
        var secret = string.IsNullOrWhiteSpace(dto.SecretKey)
            ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            : dto.SecretKey.Trim();

        var sub = new WebhookSubscription
        {
            Name = dto.Name.Trim(),
            TargetUrl = dto.TargetUrl.Trim(),
            SecretKey = secret,
            SubscribedEvents = string.IsNullOrWhiteSpace(dto.SubscribedEvents) ? "*" : dto.SubscribedEvents.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _context.WebhookSubscriptions.AddAsync(sub, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Webhook subscription created: {Name} -> {Url} (ID: {Id})", sub.Name, sub.TargetUrl, sub.Id);
        return MapToDto(sub);
    }

    public async Task<WebhookSubscriptionDto?> UpdateSubscriptionAsync(int id, UpdateWebhookSubscriptionDto dto, CancellationToken cancellationToken = default)
    {
        var sub = await _context.WebhookSubscriptions.FindAsync(new object[] { id }, cancellationToken);
        if (sub == null)
        {
            return null;
        }

        sub.Name = dto.Name.Trim();
        sub.TargetUrl = dto.TargetUrl.Trim();
        sub.SubscribedEvents = string.IsNullOrWhiteSpace(dto.SubscribedEvents) ? "*" : dto.SubscribedEvents.Trim();
        sub.IsActive = dto.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Webhook subscription updated: {Name} (ID: {Id})", sub.Name, sub.Id);

        return MapToDto(sub);
    }

    public async Task<bool> DeleteSubscriptionAsync(int id, CancellationToken cancellationToken = default)
    {
        var sub = await _context.WebhookSubscriptions.FindAsync(new object[] { id }, cancellationToken);
        if (sub == null)
        {
            return false;
        }

        _context.WebhookSubscriptions.Remove(sub);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Webhook subscription deleted: ID {Id}", id);

        return true;
    }

    public async Task<IReadOnlyList<WebhookDeliveryLogDto>> GetDeliveryLogsAsync(int subscriptionId, int limit = 50, CancellationToken cancellationToken = default)
    {
        var logs = await _context.WebhookDeliveryLogs
            .AsNoTracking()
            .Where(l => l.WebhookSubscriptionId == subscriptionId)
            .OrderByDescending(l => l.TimestampUtc)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);

        return logs.Select(l => new WebhookDeliveryLogDto
        {
            Id = l.Id,
            WebhookSubscriptionId = l.WebhookSubscriptionId,
            EventType = l.EventType,
            PayloadJson = l.PayloadJson,
            ResponseStatusCode = l.ResponseStatusCode,
            IsSuccess = l.IsSuccess,
            ErrorMessage = l.ErrorMessage,
            DurationMs = l.DurationMs,
            TimestampUtc = l.TimestampUtc
        }).ToList();
    }

    public async Task<WebhookTestResultDto> TestWebhookAsync(int subscriptionId, CancellationToken cancellationToken = default)
    {
        var sub = await _context.WebhookSubscriptions.FindAsync(new object[] { subscriptionId }, cancellationToken);
        if (sub == null)
        {
            throw new KeyNotFoundException($"Webhook subscription with ID {subscriptionId} not found.");
        }

        var testPayload = new WebhookEventPayload<object>
        {
            EventType = "TEST_PING",
            Data = new { message = "Antigravity Inventory Tracker Webhook Ping", timestamp = DateTime.UtcNow }
        };

        var json = JsonSerializer.Serialize(testPayload);
        var signature = ComputeHmacSha256(json, sub.SecretKey);

        var stopwatch = Stopwatch.StartNew();
        var isSuccess = false;
        var statusCode = 0;
        string message;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, sub.TargetUrl);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            request.Headers.Add("X-Inventory-Signature-256", $"sha256={signature}");
            request.Headers.Add("X-Inventory-Event", "TEST_PING");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            stopwatch.Stop();
            statusCode = (int)response.StatusCode;
            isSuccess = response.IsSuccessStatusCode;
            message = isSuccess ? "Webhook responded successfully." : $"Endpoint returned HTTP status {statusCode}.";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            message = $"Delivery failed: {ex.Message}";
        }

        // Record delivery log
        var log = new WebhookDeliveryLog
        {
            WebhookSubscriptionId = sub.Id,
            EventType = WebhookEventType.StockAdjusted,
            PayloadJson = json,
            ResponseStatusCode = statusCode,
            IsSuccess = isSuccess,
            ErrorMessage = isSuccess ? null : message,
            DurationMs = stopwatch.ElapsedMilliseconds,
            TimestampUtc = DateTime.UtcNow
        };
        await _context.WebhookDeliveryLogs.AddAsync(log, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new WebhookTestResultDto
        {
            SubscriptionId = sub.Id,
            TargetUrl = sub.TargetUrl,
            Success = isSuccess,
            StatusCode = statusCode,
            Message = message,
            DurationMs = stopwatch.ElapsedMilliseconds
        };
    }

    public async Task PublishEventAsync(WebhookEventType eventType, object eventData, CancellationToken cancellationToken = default)
    {
        var subscriptions = await _context.WebhookSubscriptions
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken);

        if (subscriptions.Count == 0)
        {
            return;
        }

        var eventName = eventType.ToString();
        var matchingSubs = subscriptions.Where(s => s.SubscribedEvents == "*" ||
            s.SubscribedEvents.Split(',', StringSplitOptions.TrimEntries).Contains(eventName, StringComparer.OrdinalIgnoreCase)).ToList();

        if (matchingSubs.Count == 0)
        {
            return;
        }

        var payload = new WebhookEventPayload<object>
        {
            EventType = eventName,
            Data = eventData
        };

        var json = JsonSerializer.Serialize(payload);

        foreach (var sub in matchingSubs)
        {
            var signature = ComputeHmacSha256(json, sub.SecretKey);
            var stopwatch = Stopwatch.StartNew();
            var isSuccess = false;
            var statusCode = 0;
            string? errorMessage = null;

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, sub.TargetUrl);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                request.Headers.Add("X-Inventory-Signature-256", $"sha256={signature}");
                request.Headers.Add("X-Inventory-Event", eventName);

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                stopwatch.Stop();
                statusCode = (int)response.StatusCode;
                isSuccess = response.IsSuccessStatusCode;

                if (!isSuccess)
                {
                    errorMessage = $"Endpoint returned HTTP status {statusCode}.";
                    sub.FailureCount++;
                }
                else
                {
                    sub.FailureCount = 0;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                errorMessage = ex.Message;
                sub.FailureCount++;
            }

            sub.LastTriggeredAtUtc = DateTime.UtcNow;

            var log = new WebhookDeliveryLog
            {
                WebhookSubscriptionId = sub.Id,
                EventType = eventType,
                PayloadJson = json,
                ResponseStatusCode = statusCode,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                DurationMs = stopwatch.ElapsedMilliseconds,
                TimestampUtc = DateTime.UtcNow
            };
            await _context.WebhookDeliveryLogs.AddAsync(log, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public string ComputeHmacSha256(string payload, string secretKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static WebhookSubscriptionDto MapToDto(WebhookSubscription s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        TargetUrl = s.TargetUrl,
        SubscribedEvents = s.SubscribedEvents,
        IsActive = s.IsActive,
        FailureCount = s.FailureCount,
        CreatedAtUtc = s.CreatedAtUtc,
        LastTriggeredAtUtc = s.LastTriggeredAtUtc
    };
}
