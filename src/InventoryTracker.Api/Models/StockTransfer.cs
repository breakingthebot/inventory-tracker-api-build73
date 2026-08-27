// src/InventoryTracker.Api/Models/StockTransfer.cs
// Represents an inter-warehouse inventory relocation shipment order.
// Connects to: src/InventoryTracker.Api/Models/Warehouse.cs, src/InventoryTracker.Api/Models/StockTransferItem.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// Domain entity representing a transfer order between two physical warehouses.
/// </summary>
public class StockTransfer
{
    /// <summary>
    /// Unique database primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Business reference transfer number (e.g. TRF-2026-0001).
    /// </summary>
    public string TransferNumber { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key referencing the originating source warehouse.
    /// </summary>
    public int SourceWarehouseId { get; set; }

    /// <summary>
    /// Navigation reference to the source warehouse.
    /// </summary>
    public Warehouse? SourceWarehouse { get; set; }

    /// <summary>
    /// Foreign key referencing the receiving destination warehouse.
    /// </summary>
    public int DestinationWarehouseId { get; set; }

    /// <summary>
    /// Navigation reference to the destination warehouse.
    /// </summary>
    public Warehouse? DestinationWarehouse { get; set; }

    /// <summary>
    /// Current execution state of the transfer.
    /// </summary>
    public StockTransferStatus Status { get; set; } = StockTransferStatus.Pending;

    /// <summary>
    /// User or operator initiating the transfer order.
    /// </summary>
    public string RequestedBy { get; set; } = "system";

    /// <summary>
    /// Logistics carrier tracking number (e.g. FedEx, Freight Pro tracking).
    /// </summary>
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// Optional shipment or routing instructions.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Timestamp when the transfer record was initiated.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when items departed the source warehouse.
    /// </summary>
    public DateTime? ShippedAtUtc { get; set; }

    /// <summary>
    /// Timestamp when items were received into destination inventory.
    /// </summary>
    public DateTime? ReceivedAtUtc { get; set; }

    /// <summary>
    /// Navigation collection of line items included in this transfer.
    /// </summary>
    public ICollection<StockTransferItem> Items { get; set; } = new List<StockTransferItem>();
}
