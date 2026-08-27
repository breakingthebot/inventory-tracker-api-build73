// src/InventoryTracker.Api/Models/TransactionType.cs
// Defines the enumeration of inventory transaction classifications.
// Connects to: src/InventoryTracker.Api/Models/InventoryTransaction.cs
// Created: 2026-08-26

namespace InventoryTracker.Api.Models;

/// <summary>
/// Specifies the type of stock movement in the inventory system.
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Initial stock allocation or baseline count.
    /// </summary>
    InitialStock = 0,

    /// <summary>
    /// Receiving inbound stock from supplier or purchase order.
    /// </summary>
    StockIn = 1,

    /// <summary>
    /// Outbound shipment, customer fulfillment, or consumption.
    /// </summary>
    StockOut = 2,

    /// <summary>
    /// Manual inventory count correction or shrinkage adjustment.
    /// </summary>
    Adjustment = 3,

    /// <summary>
    /// Inbound return of goods back to inventory.
    /// </summary>
    Return = 4,

    /// <summary>
    /// Damaged or expired goods removal.
    /// </summary>
    WriteOff = 5
}
