// src/InventoryTracker.Api/Models/InventoryTransaction.cs
// Represents an immutable audit log record of a stock movement event.
// Connects to: src/InventoryTracker.Api/Models/Product.cs, src/InventoryTracker.Api/Models/TransactionType.cs
// Created: 2026-08-26

namespace InventoryTracker.Api.Models;

/// <summary>
/// Domain entity recording an individual stock movement or balance adjustment.
/// </summary>
public class InventoryTransaction
{
    /// <summary>
    /// Unique database primary key identifier for the transaction.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Foreign key referencing the affected product.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Navigation reference to the associated product.
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Type of stock movement (e.g. StockIn, StockOut, Adjustment).
    /// </summary>
    public TransactionType Type { get; set; }

    /// <summary>
    /// Delta quantity applied to on-hand inventory (positive or negative).
    /// </summary>
    public int QuantityChange { get; set; }

    /// <summary>
    /// Snapshot of on-hand inventory quantity after this transaction was applied.
    /// </summary>
    public int QuantityAfter { get; set; }

    /// <summary>
    /// Unit cost associated with this transaction in USD.
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Business reason or note explaining the stock movement.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// External reference number (e.g. PO-10023, SO-48192, INV-COUNT-08).
    /// </summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Operator, system user, or API client responsible for the entry.
    /// </summary>
    public string PerformedBy { get; set; } = "system";

    /// <summary>
    /// Timestamp when the stock transaction occurred.
    /// </summary>
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
