// src/InventoryTracker.Api/Models/Customer.cs
// Domain entity representing a customer or corporate purchasing account.
// Connects to: src/InventoryTracker.Api/Models/SalesOrder.cs, src/InventoryTracker.Api/Data/InventoryDbContext.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// Domain entity representing a customer purchasing account.
/// </summary>
public class Customer
{
    /// <summary>
    /// Unique database primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique customer code (e.g. CUST-1001).
    /// </summary>
    public string CustomerCode { get; set; } = string.Empty;

    /// <summary>
    /// Company or organization name.
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Primary contact person name.
    /// </summary>
    public string ContactName { get; set; } = string.Empty;

    /// <summary>
    /// Primary contact email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Primary telephone number.
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Destination shipping street address.
    /// </summary>
    public string ShippingAddress { get; set; } = string.Empty;

    /// <summary>
    /// Destination shipping city.
    /// </summary>
    public string ShippingCity { get; set; } = string.Empty;

    /// <summary>
    /// Destination shipping state or province.
    /// </summary>
    public string ShippingState { get; set; } = string.Empty;

    /// <summary>
    /// Destination shipping postal or ZIP code.
    /// </summary>
    public string ShippingPostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Destination shipping country.
    /// </summary>
    public string ShippingCountry { get; set; } = "USA";

    /// <summary>
    /// Flag indicating whether the customer account is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Timestamp when customer account was registered.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation collection of sales orders placed by this customer.
    /// </summary>
    public ICollection<SalesOrder> Orders { get; set; } = new List<SalesOrder>();
}
