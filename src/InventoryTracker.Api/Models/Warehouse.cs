// src/InventoryTracker.Api/Models/Warehouse.cs
// Represents a physical warehouse or regional fulfillment facility.
// Connects to: src/InventoryTracker.Api/Models/WarehouseStock.cs, src/InventoryTracker.Api/Models/StockTransfer.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// Domain entity representing a physical warehouse or logistics center.
/// </summary>
public class Warehouse
{
    /// <summary>
    /// Unique database primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique facility code (e.g. WH-EAST, WH-WEST, WH-CENTRAL).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Facility descriptive name (e.g. Atlanta Regional Fulfillment Center).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Street address.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// City location.
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State or province.
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Postal code / ZIP.
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Country name or ISO code.
    /// </summary>
    public string Country { get; set; } = "USA";

    /// <summary>
    /// Total storage capacity in maximum pallet or unit volume.
    /// </summary>
    public int CapacityUnits { get; set; } = 10000;

    /// <summary>
    /// Indicates whether the warehouse is actively operating.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Timestamp when the facility was registered in the system.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation collection of product stock levels stored at this facility.
    /// </summary>
    public ICollection<WarehouseStock> StockLevels { get; set; } = new List<WarehouseStock>();

    /// <summary>
    /// Navigation collection of outbound stock transfers initiated from this facility.
    /// </summary>
    public ICollection<StockTransfer> OutboundTransfers { get; set; } = new List<StockTransfer>();

    /// <summary>
    /// Navigation collection of inbound stock transfers routed to this facility.
    /// </summary>
    public ICollection<StockTransfer> InboundTransfers { get; set; } = new List<StockTransfer>();
}
