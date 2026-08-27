// src/InventoryTracker.Api/DTOs/WarehouseDtos.cs
// Data Transfer Objects for warehouse management, location stock queries, and bin assignments.
// Connects to: src/InventoryTracker.Api/Models/Warehouse.cs, src/InventoryTracker.Api/Controllers/WarehousesController.cs
// Created: 2026-08-27

using System.ComponentModel.DataAnnotations;

namespace InventoryTracker.Api.DTOs;

/// <summary>
/// Data contract returned when reading warehouse facility information.
/// </summary>
public class WarehouseDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int CapacityUnits { get; set; }
    public int TotalUnitsStored { get; set; }
    public double UtilizationPercentage => CapacityUnits > 0 ? Math.Round((double)TotalUnitsStored / CapacityUnits * 100, 1) : 0;
    public int TotalDistinctSkus { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Request payload for registering a new warehouse facility.
/// </summary>
public class CreateWarehouseDto
{
    [Required(ErrorMessage = "Warehouse code is required.")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "Warehouse code must be between 2 and 20 characters.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Warehouse name is required.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Warehouse name must be between 3 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "Address cannot exceed 150 characters.")]
    public string Address { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "City cannot exceed 50 characters.")]
    public string City { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "State cannot exceed 50 characters.")]
    public string State { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "PostalCode cannot exceed 20 characters.")]
    public string PostalCode { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Country cannot exceed 50 characters.")]
    public string Country { get; set; } = "USA";

    [Range(100, 10000000, ErrorMessage = "CapacityUnits must be at least 100.")]
    public int CapacityUnits { get; set; } = 10000;
}

/// <summary>
/// Request payload for updating warehouse facility information.
/// </summary>
public class UpdateWarehouseDto
{
    [Required(ErrorMessage = "Warehouse name is required.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Warehouse name must be between 3 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "Address cannot exceed 150 characters.")]
    public string Address { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "City cannot exceed 50 characters.")]
    public string City { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "State cannot exceed 50 characters.")]
    public string State { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "PostalCode cannot exceed 20 characters.")]
    public string PostalCode { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Country cannot exceed 50 characters.")]
    public string Country { get; set; } = "USA";

    [Range(100, 10000000, ErrorMessage = "CapacityUnits must be at least 100.")]
    public int CapacityUnits { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Data contract returned for specific product balance within a warehouse facility.
/// </summary>
public class WarehouseStockDto
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; }
    public int AvailableQuantity { get; set; }
    public string BinLocation { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public decimal TotalValuation => QuantityOnHand * UnitCost;
    public DateTime UpdatedAtUtc { get; set; }
}

/// <summary>
/// Request payload to assign or update a product's warehouse bin coordinate.
/// </summary>
public class SetBinLocationDto
{
    [Required(ErrorMessage = "BinLocation is required.")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "BinLocation must be between 1 and 50 characters.")]
    public string BinLocation { get; set; } = string.Empty;
}
