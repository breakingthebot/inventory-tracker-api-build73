// src/InventoryTracker.Api/Models/CycleCountStatus.cs
// Defines lifecycle workflow states for physical inventory cycle counts and audit sessions.
// Connects to: src/InventoryTracker.Api/Models/CycleCount.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// Workflow status of an inventory cycle count physical audit session.
/// </summary>
public enum CycleCountStatus
{
    /// <summary>
    /// Initial session created; snapshot generated but counting has not begun.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Floor counting actively in progress by warehouse clerks.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Physical counts completed and submitted to warehouse supervisor for variance review.
    /// </summary>
    UnderReview = 2,

    /// <summary>
    /// Variances approved by supervisor; inventory balances reconciled and ledger adjustments posted.
    /// </summary>
    Reconciled = 3,

    /// <summary>
    /// Audit session voided or abandoned without adjusting inventory.
    /// </summary>
    Cancelled = 4
}
