// src/InventoryTracker.Api/Data/DbInitializer.cs
// Seeds initial category and product inventory records into the database.
// Connects to: src/InventoryTracker.Api/Data/InventoryDbContext.cs, src/InventoryTracker.Api/Program.cs
// Created: 2026-08-26

using InventoryTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryTracker.Api.Data;

/// <summary>
/// Database seeder populating initial catalog items and opening balances.
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Ensures database creation and seeds default categories and catalog items.
    /// </summary>
    public static async Task SeedAsync(InventoryDbContext context)
    {
        // For relational databases or in-memory, ensure created
        await context.Database.EnsureCreatedAsync();

        if (await context.Categories.AnyAsync())
        {
            return; // Data already seeded
        }

        var categories = new List<Category>
        {
            new() { Name = "Electronics", Description = "Computing, displays, audio, and accessories" },
            new() { Name = "Office Supplies", Description = "Stationery, paper, writing tools, and desk organizers" },
            new() { Name = "Industrial & Tools", Description = "Hand tools, power tools, safety gear, and fasteners" },
            new() { Name = "Packaging & Shipping", Description = "Cardboard cartons, tape, bubble wrap, and thermal labels" },
            new() { Name = "Apparel & Uniforms", Description = "Safety vests, warehouse boots, and work gloves" }
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();

        var elec = categories[0].Id;
        var office = categories[1].Id;
        var tools = categories[2].Id;
        var packing = categories[3].Id;
        var apparel = categories[4].Id;

        var products = new List<Product>
        {
            new()
            {
                Sku = "ELEC-MON-4K27",
                Name = "27-inch 4K UHD IPS Monitor",
                Description = "Color-accurate 60Hz 4K designer monitor with USB-C 90W PD",
                CategoryId = elec,
                UnitPrice = 349.99m,
                UnitCost = 210.00m,
                QuantityInStock = 45,
                ReorderThreshold = 15,
                ReorderQuantity = 50,
                UnitOfMeasure = "pcs",
                IsActive = true
            },
            new()
            {
                Sku = "ELEC-KEY-MEC01",
                Name = "Hot-Swappable Mechanical Keyboard",
                Description = "Gateron Brown tactile switches, RGB backlighting, wireless 2.4G",
                CategoryId = elec,
                UnitPrice = 89.99m,
                UnitCost = 42.50m,
                QuantityInStock = 120,
                ReorderThreshold = 25,
                ReorderQuantity = 100,
                UnitOfMeasure = "pcs",
                IsActive = true
            },
            new()
            {
                Sku = "ELEC-HUB-10P",
                Name = "10-in-1 Aluminum USB-C Docking Hub",
                Description = "Dual HDMI 4K, 100W PD, SD card reader, Gigabit Ethernet",
                CategoryId = elec,
                UnitPrice = 59.95m,
                UnitCost = 28.00m,
                QuantityInStock = 8, // Low stock on purpose
                ReorderThreshold = 20,
                ReorderQuantity = 75,
                UnitOfMeasure = "pcs",
                IsActive = true
            },
            new()
            {
                Sku = "OFF-PAP-A4500",
                Name = "Premium Multi-Use Copy Paper A4 (500 sheets)",
                Description = "80 GSM high-opacity bright white printing paper",
                CategoryId = office,
                UnitPrice = 8.50m,
                UnitCost = 4.10m,
                QuantityInStock = 450,
                ReorderThreshold = 100,
                ReorderQuantity = 500,
                UnitOfMeasure = "ream",
                IsActive = true
            },
            new()
            {
                Sku = "OFF-PEN-GEL12",
                Name = "Archival Fine Gel Pen Set (Pack of 12)",
                Description = "0.5mm quick-dry waterproof black ink gel rollerballs",
                CategoryId = office,
                UnitPrice = 14.99m,
                UnitCost = 6.25m,
                QuantityInStock = 0, // Out of stock on purpose
                ReorderThreshold = 30,
                ReorderQuantity = 150,
                UnitOfMeasure = "pack",
                IsActive = true
            },
            new()
            {
                Sku = "TOOL-DRILL-20V",
                Name = "20V Cordless Brushless Hammer Drill Kit",
                Description = "High-torque 65Nm cordless drill with 2x 4.0Ah batteries & rapid charger",
                CategoryId = tools,
                UnitPrice = 189.00m,
                UnitCost = 98.00m,
                QuantityInStock = 28,
                ReorderThreshold = 10,
                ReorderQuantity = 30,
                UnitOfMeasure = "kit",
                IsActive = true
            },
            new()
            {
                Sku = "PKG-BOX-12X12",
                Name = "Heavy Duty Corrugated Shipping Box 12x12x12 (Bundle of 25)",
                Description = "200# / 32 ECT single wall Kraft shipping boxes",
                CategoryId = packing,
                UnitPrice = 32.50m,
                UnitCost = 16.20m,
                QuantityInStock = 70,
                ReorderThreshold = 20,
                ReorderQuantity = 100,
                UnitOfMeasure = "bundle",
                IsActive = true
            },
            new()
            {
                Sku = "APP-GLV-NIT100",
                Name = "Heavy Duty Nitrile Warehouse Gloves (Box of 100)",
                Description = "6 mil textured diamond grip industrial disposable nitrile gloves",
                CategoryId = apparel,
                UnitPrice = 22.00m,
                UnitCost = 9.80m,
                QuantityInStock = 5, // Low stock on purpose
                ReorderThreshold = 15,
                ReorderQuantity = 80,
                UnitOfMeasure = "box",
                IsActive = true
            }
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();

        // Seed opening balance transactions for products
        var transactions = new List<InventoryTransaction>();
        foreach (var p in products.Where(x => x.QuantityInStock > 0))
        {
            transactions.Add(new InventoryTransaction
            {
                ProductId = p.Id,
                Type = TransactionType.InitialStock,
                QuantityChange = p.QuantityInStock,
                QuantityAfter = p.QuantityInStock,
                UnitCost = p.UnitCost,
                Reason = "Opening balance inventory count",
                ReferenceNumber = "INIT-COUNT-2026",
                PerformedBy = "system_seed",
                TimestampUtc = DateTime.UtcNow.AddDays(-14)
            });
        }

        await context.InventoryTransactions.AddRangeAsync(transactions);
        await context.SaveChangesAsync();
    }
}
