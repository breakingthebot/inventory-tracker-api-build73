// src/InventoryTracker.Api/Middleware/GlobalExceptionMiddleware.cs
// Intercepts unhandled exceptions across the HTTP pipeline and returns standard JSON error envelopes.
// Connects to: src/InventoryTracker.Api/DTOs/ApiResponse.cs, src/InventoryTracker.Api/Program.cs
// Created: 2026-08-26

using System.Net;
using System.Text.Json;
using InventoryTracker.Api.DTOs;

namespace InventoryTracker.Api.Middleware;

/// <summary>
/// Global middleware capturing unhandled exceptions and formatting structured HTTP error responses.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred during request {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            KeyNotFoundException notFound => (HttpStatusCode.NotFound, notFound.Message),
            ArgumentException badArg => (HttpStatusCode.BadRequest, badArg.Message),
            InvalidOperationException opEx => (HttpStatusCode.BadRequest, opEx.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Access denied."),
            _ => (HttpStatusCode.InternalServerError, "An unexpected internal server error occurred.")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.Fail(message, new List<string> { message });
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}
