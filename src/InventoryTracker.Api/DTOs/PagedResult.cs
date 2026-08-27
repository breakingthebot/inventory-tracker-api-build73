// src/InventoryTracker.Api/DTOs/PagedResult.cs
// Generic container for paginated query results with pagination metadata.
// Connects to: src/InventoryTracker.Api/DTOs/ApiResponse.cs, src/InventoryTracker.Api/Services/*
// Created: 2026-08-26

namespace InventoryTracker.Api.DTOs;

/// <summary>
/// Encapsulates paginated collection results alongside paging metadata.
/// </summary>
/// <typeparam name="T">Type of items in the page.</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Current 1-based page index.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items requested per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total count of matching records across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of calculated pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Indicates whether a subsequent page is available.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Indicates whether a preceding page exists.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// The collection of items on this page.
    /// </summary>
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    /// <summary>
    /// Factory method to construct a PagedResult.
    /// </summary>
    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
