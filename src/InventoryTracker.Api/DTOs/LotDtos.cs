// src/InventoryTracker.Api/DTOs/LotDtos.cs
// Data Transfer Objects for product lots, expiration tracking, FEFO dispatching plans, and quarantine status.
// Connects to: src/InventoryTracker.Api/Services/ILotTrackingService.cs, src/InventoryTracker.Api/Controllers/LotsController.cs
// Created: 2026-08-27

using System.ComponentModel.DataAnnotations;
using InventoryTracker.Api.Models;

namespace InventoryTracker.Api.DTOs;

/// <summary>
/// Data contract returned for product batch lot records.
/// </summary>
public class ProductLotDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public int QuantityInitial { get; set; }
    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; }
    public int AvailableQuantity { get; set; }
    public DateTime? ManufactureDateUtc { get; set; }
    public DateTime? ExpirationDateUtc { get; set; }
    public LotStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public bool IsExpired => ExpirationDateUtc.HasValue && ExpirationDateUtc.Value < DateTime.UtcNow;
    public int? DaysUntilExpiration => ExpirationDateUtc.HasValue ? (int)(ExpirationDateUtc.Value - DateTime.UtcNow).TotalDays : null;
    public DateTime ReceivedAtUtc { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Request payload to register a new product batch lot.
/// </summary>
public class CreateProductLotDto
{
    [Required(ErrorMessage = "ProductId is required.")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "WarehouseId is required.")]
    public int WarehouseId { get; set; }

    [Required(ErrorMessage = "LotNumber is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "LotNumber must be between 2 and 50 characters.")]
    public string LotNumber { get; set; } = string.Empty;

    [Range(1, 1000000, ErrorMessage = "Quantity must be greater than zero.")]
    public int Quantity { get; set; }

    public DateTime? ManufactureDateUtc { get; set; }
    public DateTime? ExpirationDateUtc { get; set; }

    public LotStatus Status { get; set; } = LotStatus.Active;

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }
}

/// <summary>
/// Request payload to update an existing lot's status or expiration date.
/// </summary>
public class UpdateProductLotDto
{
    public LotStatus Status { get; set; }
    public DateTime? ExpirationDateUtc { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Query filters for product lot listings.
/// </summary>
public class LotFilterDto
{
    public int? ProductId { get; set; }
    public int? WarehouseId { get; set; }
    public LotStatus? Status { get; set; }
    public bool? ExpiredOnly { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Summary report of lots approaching expiration.
/// </summary>
public class ExpiringLotsSummaryDto
{
    public int TotalExpiringLotsCount { get; set; }
    public int TotalExpiringUnits { get; set; }
    public decimal EstimatedAtRiskValuation { get; set; }
    public IReadOnlyList<ProductLotDto> ExpiringLots { get; set; } = new List<ProductLotDto>();
}

/// <summary>
/// Recommended allocation plan following FEFO (First-Expired, First-Out) rules.
/// </summary>
public class FefoAllocationPlanDto
{
    public int ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    public int TotalAllocatedQuantity { get; set; }
    public bool IsFullyAllocated => TotalAllocatedQuantity >= RequestedQuantity;
    public IReadOnlyList<FefoLotAllocationItemDto> Allocations { get; set; } = new List<FefoLotAllocationItemDto>();
}

/// <summary>
/// Individual lot pick recommendation within a FEFO allocation plan.
/// </summary>
public class FefoLotAllocationItemDto
{
    public int LotId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateTime? ExpirationDateUtc { get; set; }
    public int? DaysUntilExpiration { get; set; }
    public int AvailableInLot { get; set; }
    public int QuantityToPick { get; set; }
}

/// <summary>
/// Request payload to execute a FEFO batch dispatch.
/// </summary>
public class DispatchFefoRequestDto
{
    [Required(ErrorMessage = "ProductId is required.")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "WarehouseId is required.")]
    public int WarehouseId { get; set; }

    [Range(1, 100000, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "ReferenceNumber is required.")]
    public string ReferenceNumber { get; set; } = string.Empty;

    public string? Reason { get; set; }
}

/// <summary>
/// Result returned after executing a FEFO batch dispatch.
/// </summary>
public class DispatchFefoResultDto
{
    public int ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public int TotalDispatchedQuantity { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public IReadOnlyList<FefoLotAllocationItemDto> DispatchedLots { get; set; } = new List<FefoLotAllocationItemDto>();
}
