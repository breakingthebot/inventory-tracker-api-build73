// src/InventoryTracker.Api/Models/UserRole.cs
// Defines authorization security roles for system operators.
// Connects to: src/InventoryTracker.Api/Models/User.cs
// Created: 2026-08-27

namespace InventoryTracker.Api.Models;

/// <summary>
/// User access role defining permission boundaries across the inventory API.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Full administrative permissions: catalog management, user accounts, and system configuration.
    /// </summary>
    Admin = 0,

    /// <summary>
    /// Facility supervisor: purchase orders, transfers, stock adjustments, and supplier setup.
    /// </summary>
    WarehouseManager = 1,

    /// <summary>
    /// Warehouse floor worker: barcode scanning, dispatching, and goods receiving.
    /// </summary>
    Clerk = 2,

    /// <summary>
    /// Read-only auditor: valuation reports, transaction history, and cycle counts.
    /// </summary>
    Auditor = 3
}
