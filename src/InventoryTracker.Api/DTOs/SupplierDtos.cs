// src/InventoryTracker.Api/DTOs/SupplierDtos.cs
// Data Transfer Objects for supplier vendor registration, details, and updates.
// Connects to: src/InventoryTracker.Api/Models/Supplier.cs, src/InventoryTracker.Api/Controllers/SuppliersController.cs
// Created: 2026-08-27

using System.ComponentModel.DataAnnotations;

namespace InventoryTracker.Api.DTOs;

/// <summary>
/// Data contract returned for supplier vendor records.
/// </summary>
public class SupplierDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int LeadTimeDays { get; set; }
    public string PaymentTerms { get; set; } = string.Empty;
    public int TotalProductsSupplied { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Request payload to register a new vendor supplier.
/// </summary>
public class CreateSupplierDto
{
    [Required(ErrorMessage = "Supplier code is required.")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "Code must be between 2 and 20 characters.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Supplier name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "ContactName cannot exceed 100 characters.")]
    public string? ContactName { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid telephone format.")]
    [StringLength(30, ErrorMessage = "Phone cannot exceed 30 characters.")]
    public string? Phone { get; set; }

    [Range(1, 180, ErrorMessage = "LeadTimeDays must be between 1 and 180 days.")]
    public int LeadTimeDays { get; set; } = 7;

    [StringLength(50, ErrorMessage = "PaymentTerms cannot exceed 50 characters.")]
    public string PaymentTerms { get; set; } = "Net 30";
}

/// <summary>
/// Request payload to update an existing vendor supplier.
/// </summary>
public class UpdateSupplierDto
{
    [Required(ErrorMessage = "Supplier name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "ContactName cannot exceed 100 characters.")]
    public string? ContactName { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid telephone format.")]
    [StringLength(30, ErrorMessage = "Phone cannot exceed 30 characters.")]
    public string? Phone { get; set; }

    [Range(1, 180, ErrorMessage = "LeadTimeDays must be between 1 and 180 days.")]
    public int LeadTimeDays { get; set; }

    [StringLength(50, ErrorMessage = "PaymentTerms cannot exceed 50 characters.")]
    public string PaymentTerms { get; set; } = "Net 30";

    public bool IsActive { get; set; } = true;
}
