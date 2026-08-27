// src/InventoryTracker.Api/DTOs/InventoryDtos.cs
// Data Transfer Objects for stock adjustments, restock, dispatch, and transaction records.
// Connects to: src/InventoryTracker.Api/Models/InventoryTransaction.cs, src/InventoryTracker.Api/Controllers/InventoryController.cs
// Created: 2026-08-26

using System.ComponentModel.DataAnnotations;
using InventoryTracker.Api.Models;

namespace InventoryTracker.Api.DTOs;

/// <summary>
/// Data contract returned for an individual inventory transaction audit record.
/// </summary>
public class TransactionDto
{
    public long Id { get; set; }
    public int ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public string TypeName => Type.ToString();
    public int QuantityChange { get; set; }
    public int QuantityAfter { get; set; }
    public decimal UnitCost { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
}

/// <summary>
/// Request payload for adjusting stock quantity (positive count up or negative count down).
/// </summary>
public class StockAdjustmentDto
{
    [Required(ErrorMessage = "ProductId is required.")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "QuantityChange cannot be zero.")]
    public int QuantityChange { get; set; }

    [Required(ErrorMessage = "Reason is required for stock adjustment.")]
    [StringLength(250, MinimumLength = 3, ErrorMessage = "Reason must be between 3 and 250 characters.")]
    public string Reason { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "ReferenceNumber cannot exceed 100 characters.")]
    public string? ReferenceNumber { get; set; }

    public string PerformedBy { get; set; } = "system";
}

/// <summary>
/// Request payload for inbound stock replenishment.
/// </summary>
public class RestockRequestDto
{
    [Required(ErrorMessage = "ProductId is required.")]
    public int ProductId { get; set; }

    [Range(1, 1000000, ErrorMessage = "Quantity must be at least 1 unit.")]
    public int Quantity { get; set; }

    [Range(0.00, 1000000.00, ErrorMessage = "UnitCost must be non-negative.")]
    public decimal UnitCost { get; set; }

    [StringLength(100, ErrorMessage = "PurchaseOrderNumber cannot exceed 100 characters.")]
    public string? PurchaseOrderNumber { get; set; }

    [StringLength(250, ErrorMessage = "Notes cannot exceed 250 characters.")]
    public string? Notes { get; set; }

    public string PerformedBy { get; set; } = "warehouse";
}

/// <summary>
/// Request payload for outbound stock dispatch / customer order fulfillment.
/// </summary>
public class DispatchRequestDto
{
    [Required(ErrorMessage = "ProductId is required.")]
    public int ProductId { get; set; }

    [Range(1, 1000000, ErrorMessage = "Quantity must be at least 1 unit.")]
    public int Quantity { get; set; }

    [StringLength(100, ErrorMessage = "SalesOrderNumber cannot exceed 100 characters.")]
    public string? SalesOrderNumber { get; set; }

    [StringLength(250, ErrorMessage = "Notes cannot exceed 250 characters.")]
    public string? Notes { get; set; }

    public string PerformedBy { get; set; } = "dispatch";
}

/// <summary>
/// Query filter parameters for querying stock transactions.
/// </summary>
public class TransactionFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int? ProductId { get; set; }
    public TransactionType? Type { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
