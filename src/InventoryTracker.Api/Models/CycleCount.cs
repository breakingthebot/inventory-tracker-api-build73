// src/InventoryTracker.Api/Models/CycleCount.cs
// Represents a physical inventory audit session capturing facility snapshots and count discrepancies.
// Connects to: src/InventoryTracker.Api/Models/Warehouse.cs, src/InventoryTracker.Api/Models/CycleCountItem.cs, src/InventoryTracker.Api/Models/CycleCountStatus.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// Domain entity representing a cycle counting physical audit session.
/// </summary>
public class CycleCount
{
    /// <summary>
    /// Unique database primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Human-readable audit tracking number (e.g. CC-2026-08-01).
    /// </summary>
    public string CountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key referencing the warehouse facility being audited.
    /// </summary>
    public int WarehouseId { get; set; }

    /// <summary>
    /// Navigation reference to the warehouse facility.
    /// </summary>
    public Warehouse? Warehouse { get; set; }

    /// <summary>
    /// Current workflow state of the audit session.
    /// </summary>
    public CycleCountStatus Status { get; set; } = CycleCountStatus.Draft;

    /// <summary>
    /// Audit scope description (e.g. FullWarehouse, Category:Electronics, Aisle:A-01).
    /// </summary>
    public string Scope { get; set; } = "FullWarehouse";

    /// <summary>
    /// Username of the operator who initiated the audit session.
    /// </summary>
    public string InitiatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Username of the supervisor who reviewed or reconciled the audit.
    /// </summary>
    public string? ReviewedBy { get; set; }

    /// <summary>
    /// Total number of unique line items included in the audit.
    /// </summary>
    public int TotalItemsCounted { get; set; }

    /// <summary>
    /// Aggregate unit variance across all counted lines (Counted - System).
    /// </summary>
    public int TotalVarianceUnits { get; set; }

    /// <summary>
    /// Aggregate dollar valuation variance across all counted lines.
    /// </summary>
    public decimal TotalVarianceCost { get; set; }

    /// <summary>
    /// Timestamp when audit session was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when floor counting completed and was submitted for review.
    /// </summary>
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// Timestamp when inventory was reconciled and ledger adjustments posted.
    /// </summary>
    public DateTime? ReconciledAtUtc { get; set; }

    /// <summary>
    /// Additional audit notes, reconciliation justification, or cancellation reason.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Collection of individual line item count records within this audit session.
    /// </summary>
    public ICollection<CycleCountItem> Items { get; set; } = new List<CycleCountItem>();
}
