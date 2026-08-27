// src/InventoryTracker.Api/DTOs/CycleCountDtos.cs
// Data Transfer Objects for cycle counting sessions, blind count entry, variance analytics, and reconciliation.
// Connects to: src/InventoryTracker.Api/Services/ICycleCountService.cs, src/InventoryTracker.Api/Controllers/CycleCountsController.cs
// Created: 2026-08-27

using System.ComponentModel.DataAnnotations;
using InventoryTracker.Api.Models;

namespace InventoryTracker.Api.DTOs;

/// <summary>
/// Data contract returned for a cycle count audit session.
/// </summary>
public class CycleCountDto
{
    public int Id { get; set; }
    public string CountNumber { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public CycleCountStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string Scope { get; set; } = string.Empty;
    public string InitiatedBy { get; set; } = string.Empty;
    public string? ReviewedBy { get; set; }
    public int TotalItemsCounted { get; set; }
    public int TotalVarianceUnits { get; set; }
    public decimal TotalVarianceCost { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? ReconciledAtUtc { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<CycleCountItemDto> Items { get; set; } = new List<CycleCountItemDto>();
}

/// <summary>
/// Data contract returned for individual line items in a cycle count audit session.
/// </summary>
public class CycleCountItemDto
{
    public int Id { get; set; }
    public int CycleCountId { get; set; }
    public int ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string BinLocation { get; set; } = string.Empty;
    public int SystemQuantity { get; set; }
    public int? CountedQuantity { get; set; }
    public int VarianceQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal VarianceCost { get; set; }
    public string? CountedBy { get; set; }
    public DateTime? CountedAtUtc { get; set; }
    public bool IsReconciled { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Request payload to initiate a new cycle count audit session.
/// </summary>
public class CreateCycleCountDto
{
    [Required(ErrorMessage = "WarehouseId is required.")]
    public int WarehouseId { get; set; }

    [StringLength(100, ErrorMessage = "Scope cannot exceed 100 characters.")]
    public string Scope { get; set; } = "FullWarehouse";

    public int? CategoryId { get; set; }

    public string? InitiatedBy { get; set; }

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }
}

/// <summary>
/// Request payload to record blind physical counts for multiple line items in bulk.
/// </summary>
public class RecordBatchCountDto
{
    [Required]
    public List<CountItemSubmissionDto> Counts { get; set; } = new();

    public string? CountedBy { get; set; }
}

/// <summary>
/// Individual line item physical count submission.
/// </summary>
public class CountItemSubmissionDto
{
    [Required]
    public int ItemId { get; set; }

    [Range(0, 1000000, ErrorMessage = "CountedQuantity must be non-negative.")]
    public int CountedQuantity { get; set; }

    public string? Notes { get; set; }
}

/// <summary>
/// Request payload to approve and reconcile an audit session.
/// </summary>
public class ReconcileCycleCountDto
{
    [Required(ErrorMessage = "ApprovedBy is required.")]
    public string ApprovedBy { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }
}

/// <summary>
/// Detailed variance summary report comparing system vs physical inventory.
/// </summary>
public class CycleCountVarianceReportDto
{
    public int CycleCountId { get; set; }
    public string CountNumber { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public int TotalLinesAudited { get; set; }
    public int TotalLinesWithDiscrepancy { get; set; }
    public int NetUnitVariance { get; set; }
    public decimal NetCostVariance { get; set; }
    public decimal AbsoluteCostVariance { get; set; }
    public decimal InventoryAccuracyRate { get; set; }
    public IReadOnlyList<CycleCountItemDto> Discrepancies { get; set; } = new List<CycleCountItemDto>();
}
