// src/InventoryTracker.Api/DTOs/TransferDtos.cs
// Data Transfer Objects for initiating, executing, and querying inter-warehouse stock transfers.
// Connects to: src/InventoryTracker.Api/Models/StockTransfer.cs, src/InventoryTracker.Api/Controllers/TransfersController.cs
// Created: 2026-08-27

using System.ComponentModel.DataAnnotations;
using InventoryTracker.Api.Models;

namespace InventoryTracker.Api.DTOs;

/// <summary>
/// Data contract returned when querying a stock transfer shipment.
/// </summary>
public class StockTransferDto
{
    public int Id { get; set; }
    public string TransferNumber { get; set; } = string.Empty;
    public int SourceWarehouseId { get; set; }
    public string SourceWarehouseCode { get; set; } = string.Empty;
    public string SourceWarehouseName { get; set; } = string.Empty;
    public int DestinationWarehouseId { get; set; }
    public string DestinationWarehouseCode { get; set; } = string.Empty;
    public string DestinationWarehouseName { get; set; } = string.Empty;
    public StockTransferStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string RequestedBy { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public string? Notes { get; set; }
    public int TotalItemsRequested { get; set; }
    public int TotalItemsShipped { get; set; }
    public int TotalItemsReceived { get; set; }
    public decimal TotalTransferValuation { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ShippedAtUtc { get; set; }
    public DateTime? ReceivedAtUtc { get; set; }
    public List<StockTransferItemDto> Items { get; set; } = new();
}

/// <summary>
/// Line item data contract within a stock transfer order.
/// </summary>
public class StockTransferItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int QuantityRequested { get; set; }
    public int QuantityShipped { get; set; }
    public int QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotalCost => QuantityRequested * UnitCost;
}

/// <summary>
/// Request payload to initiate a new inter-warehouse stock transfer.
/// </summary>
public class CreateStockTransferDto
{
    [Required(ErrorMessage = "SourceWarehouseId is required.")]
    public int SourceWarehouseId { get; set; }

    [Required(ErrorMessage = "DestinationWarehouseId is required.")]
    public int DestinationWarehouseId { get; set; }

    [Required(ErrorMessage = "At least one transfer item is required.")]
    [MinLength(1, ErrorMessage = "At least one item must be included in the transfer.")]
    public List<CreateStockTransferItemDto> Items { get; set; } = new();

    public string RequestedBy { get; set; } = "logistics_coordinator";

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }
}

/// <summary>
/// Line item request payload when creating a transfer.
/// </summary>
public class CreateStockTransferItemDto
{
    [Required(ErrorMessage = "ProductId is required.")]
    public int ProductId { get; set; }

    [Range(1, 100000, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }
}

/// <summary>
/// Request payload to mark a transfer as shipped and in-transit.
/// </summary>
public class ShipTransferDto
{
    [StringLength(100, ErrorMessage = "TrackingNumber cannot exceed 100 characters.")]
    public string? TrackingNumber { get; set; }

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }

    public string ShippedBy { get; set; } = "source_warehouse_dock";
}

/// <summary>
/// Request payload to confirm receipt of transfer items at destination warehouse.
/// </summary>
public class ReceiveTransferDto
{
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }

    public string ReceivedBy { get; set; } = "dest_warehouse_receiving";
}

/// <summary>
/// Query filter parameters for querying stock transfers.
/// </summary>
public class TransferFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int? SourceWarehouseId { get; set; }
    public int? DestinationWarehouseId { get; set; }
    public StockTransferStatus? Status { get; set; }
    public string? Search { get; set; }
}
