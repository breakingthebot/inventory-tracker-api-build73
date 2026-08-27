// src/InventoryTracker.Api/Models/Supplier.cs
// Represents a verified vendor or manufacturer supplying catalog inventory.
// Connects to: src/InventoryTracker.Api/Models/Product.cs, src/InventoryTracker.Api/Models/PurchaseOrder.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// Domain entity representing a product supplier or manufacturing vendor.
/// </summary>
public class Supplier
{
    /// <summary>
    /// Unique database primary key identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique vendor code identifier (e.g. SUP-ELEC, SUP-OFFICE).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Commercial business name of the supplier.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Primary contact representative name.
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    /// Purchasing or order notification email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Vendor telephone number.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Standard order fulfillment lead time in business days.
    /// </summary>
    public int LeadTimeDays { get; set; } = 7;

    /// <summary>
    /// Payment terms (e.g. Net 30, Net 60, Due on Receipt).
    /// </summary>
    public string PaymentTerms { get; set; } = "Net 30";

    /// <summary>
    /// Indicates whether the vendor is an active supplier.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Timestamp when the vendor record was established.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation collection of products primarily sourced from this supplier.
    /// </summary>
    public ICollection<Product> SourcedProducts { get; set; } = new List<Product>();

    /// <summary>
    /// Navigation collection of purchase orders issued to this supplier.
    /// </summary>
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
