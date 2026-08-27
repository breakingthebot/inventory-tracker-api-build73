// src/InventoryTracker.Api/DTOs/SalesOrderDtos.cs
// Data Transfer Objects for customer accounts, sales order creation, stock allocation, pick lists, packing, and shipment.
// Connects to: src/InventoryTracker.Api/Services/ISalesOrderService.cs, src/InventoryTracker.Api/Controllers/SalesOrdersController.cs
// Created: 2026-08-27

using System.ComponentModel.DataAnnotations;
using InventoryTracker.Api.Models;

namespace InventoryTracker.Api.DTOs;

/// <summary>
/// Data contract returned for a customer account.
/// </summary>
public class CustomerDto
{
    public int Id { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingState { get; set; } = string.Empty;
    public string ShippingPostalCode { get; set; } = string.Empty;
    public string ShippingCountry { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int TotalOrdersPlaced { get; set; }
}

/// <summary>
/// Request payload to register a new customer account.
/// </summary>
public class CreateCustomerDto
{
    [Required(ErrorMessage = "CustomerCode is required.")]
    [StringLength(30, ErrorMessage = "CustomerCode cannot exceed 30 characters.")]
    public string CustomerCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "CompanyName is required.")]
    [StringLength(150, ErrorMessage = "CompanyName cannot exceed 150 characters.")]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "ContactName cannot exceed 100 characters.")]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number.")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "ShippingAddress is required.")]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "ShippingCity is required.")]
    public string ShippingCity { get; set; } = string.Empty;

    [Required(ErrorMessage = "ShippingState is required.")]
    public string ShippingState { get; set; } = string.Empty;

    [Required(ErrorMessage = "ShippingPostalCode is required.")]
    public string ShippingPostalCode { get; set; } = string.Empty;

    public string ShippingCountry { get; set; } = "USA";
}

/// <summary>
/// Request payload to update an existing customer account.
/// </summary>
public class UpdateCustomerDto
{
    [Required(ErrorMessage = "CompanyName is required.")]
    [StringLength(150, ErrorMessage = "CompanyName cannot exceed 150 characters.")]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "ContactName cannot exceed 100 characters.")]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingState { get; set; } = string.Empty;
    public string ShippingPostalCode { get; set; } = string.Empty;
    public string ShippingCountry { get; set; } = "USA";
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Data contract returned for a customer sales order.
/// </summary>
public class SalesOrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public SalesOrderStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? ShippingCarrier { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime OrderDateUtc { get; set; }
    public DateTime? AllocatedAtUtc { get; set; }
    public DateTime? PickedAtUtc { get; set; }
    public DateTime? PackedAtUtc { get; set; }
    public DateTime? ShippedAtUtc { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<SalesOrderItemDto> Items { get; set; } = new List<SalesOrderItemDto>();
}

/// <summary>
/// Data contract returned for individual line items within a sales order.
/// </summary>
public class SalesOrderItemDto
{
    public int Id { get; set; }
    public int SalesOrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int QuantityOrdered { get; set; }
    public int QuantityPicked { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal UnitCostSnapshot { get; set; }
    public string? BinLocationSnapshot { get; set; }
}

/// <summary>
/// Request payload to draft a new customer sales order.
/// </summary>
public class CreateSalesOrderDto
{
    [Required(ErrorMessage = "CustomerId is required.")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "WarehouseId is required.")]
    public int WarehouseId { get; set; }

    [Range(0, 100000, ErrorMessage = "ShippingFee must be non-negative.")]
    public decimal ShippingFee { get; set; } = 0m;

    [Range(0, 100000, ErrorMessage = "TaxAmount must be non-negative.")]
    public decimal TaxAmount { get; set; } = 0m;

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }

    [Required(ErrorMessage = "Order must have at least one line item.")]
    [MinLength(1, ErrorMessage = "Order must have at least one line item.")]
    public List<CreateSalesOrderItemDto> Items { get; set; } = new();
}

/// <summary>
/// Line item payload for creating a sales order.
/// </summary>
public class CreateSalesOrderItemDto
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, 10000, ErrorMessage = "QuantityOrdered must be at least 1.")]
    public int QuantityOrdered { get; set; }

    [Range(0.01, 1000000, ErrorMessage = "UnitPrice must be greater than zero.")]
    public decimal? UnitPrice { get; set; } // If null, product catalog unit price is used
}

/// <summary>
/// Pick list sheet with bin locations for warehouse runners.
/// </summary>
public class PickListDto
{
    public int SalesOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public DateTime OrderDateUtc { get; set; }
    public int TotalItemsToPick { get; set; }
    public IReadOnlyList<PickListItemDto> Lines { get; set; } = new List<PickListItemDto>();
}

/// <summary>
/// Individual line on a warehouse runner pick sheet.
/// </summary>
public class PickListItemDto
{
    public int ItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string BinLocation { get; set; } = string.Empty;
    public int QuantityToPick { get; set; }
    public int QuantityPicked { get; set; }
}

/// <summary>
/// Request payload to record picking completion.
/// </summary>
public class PickOrderDto
{
    [Required]
    public List<PickItemDto> PickedItems { get; set; } = new();

    public string? PickedBy { get; set; }
}

/// <summary>
/// Line item picking confirmation.
/// </summary>
public class PickItemDto
{
    [Required]
    public int ItemId { get; set; }

    [Range(0, 10000)]
    public int QuantityPicked { get; set; }
}

/// <summary>
/// Request payload to complete carton packing.
/// </summary>
public class PackOrderDto
{
    [Required(ErrorMessage = "ShippingCarrier is required.")]
    [StringLength(50)]
    public string ShippingCarrier { get; set; } = string.Empty;

    public string? PackedBy { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Request payload to dispatch shipment with carrier tracking.
/// </summary>
public class ShipOrderDto
{
    [Required(ErrorMessage = "TrackingNumber is required.")]
    [StringLength(100)]
    public string TrackingNumber { get; set; } = string.Empty;

    [StringLength(50)]
    public string? ShippingCarrier { get; set; }

    public string? ShippedBy { get; set; }
}
