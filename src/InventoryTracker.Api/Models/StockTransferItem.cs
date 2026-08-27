// src/InventoryTracker.Api/Models/StockTransferItem.cs
// Represents a specific product line item and quantity in a stock transfer shipment.
// Connects to: src/InventoryTracker.Api/Models/StockTransfer.cs, src/InventoryTracker.Api/Models/Product.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// Line item specifying product and quantities associated with a stock transfer.
/// </summary>
public class StockTransferItem
{
    /// <summary>
    /// Unique database primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key referencing the parent stock transfer.
    /// </summary>
    public int StockTransferId { get; set; }

    /// <summary>
    /// Navigation reference to parent stock transfer.
    /// </summary>
    public StockTransfer? StockTransfer { get; set; }

    /// <summary>
    /// Foreign key referencing the product being transferred.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Navigation reference to the product entity.
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Quantity initially requested to transfer.
    /// </summary>
    public int QuantityRequested { get; set; }

    /// <summary>
    /// Quantity physically packed and shipped from the source warehouse.
    /// </summary>
    public int QuantityShipped { get; set; }

    /// <summary>
    /// Quantity verified and received into destination warehouse stock.
    /// </summary>
    public int QuantityReceived { get; set; }

    /// <summary>
    /// Unit cost snapshot at the time of transfer in USD.
    /// </summary>
    public decimal UnitCost { get; set; }
}
