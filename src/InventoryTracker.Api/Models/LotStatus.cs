// src/InventoryTracker.Api/Models/LotStatus.cs
// Defines lifecycle and quality quarantine states for inventory product lots.
// Connects to: src/InventoryTracker.Api/Models/ProductLot.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// Operational status of an inventory product batch or lot.
/// </summary>
public enum LotStatus
{
    /// <summary>
    /// Active and available for FEFO/FIFO dispatching and fulfillment.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Placed on quality hold or inspection; locked from dispatching.
    /// </summary>
    Quarantine = 1,

    /// <summary>
    /// Past expiration date; flagged for disposal or vendor return.
    /// </summary>
    Expired = 2,

    /// <summary>
    /// Fully consumed; zero on-hand units remaining.
    /// </summary>
    Depleted = 3
}
