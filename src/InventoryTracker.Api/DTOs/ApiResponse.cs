// src/InventoryTracker.Api/DTOs/ApiResponse.cs
// Standardized envelope wrapper for all REST API responses.
// Connects to: src/InventoryTracker.Api/Controllers/*
// Created: 2026-08-26

namespace InventoryTracker.Api.DTOs;

/// <summary>
/// Universal response envelope for API endpoints.
/// </summary>
/// <typeparam name="T">Type of the data payload.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the request succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// User-facing status message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The response payload.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Collection of validation or system errors if request failed.
    /// </summary>
    public List<string>? Errors { get; set; }

    /// <summary>
    /// Timestamp when the response was generated in UTC.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful response envelope.
    /// </summary>
    public static ApiResponse<T> Ok(T data, string message = "Operation completed successfully.")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Errors = null,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a failed response envelope.
    /// </summary>
    public static ApiResponse<T> Fail(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            Errors = errors ?? new List<string> { message },
            Timestamp = DateTime.UtcNow
        };
    }
}
