// src/InventoryTracker.Api/Data/DbInitializer.cs
// Seeds initial category, supplier, product, warehouse facility, and inventory stock distributions into the database.
// Connects to: src/InventoryTracker.Api/Data/InventoryDbContext.cs, src/InventoryTracker.Api/Program.cs
// Created: 2026-08-26

using InventoryTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryTracker.Api.Data;

/// <summary>
/// Database seeder populating initial catalog items, vendors, facilities, and opening balances.
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Ensures database creation and seeds default categories, vendors, facilities, and catalog items.
    /// </summary>
    public static async Task SeedAsync(InventoryDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (await context.Categories.AnyAsync())
        {
            return; // Data already seeded
        }

        // 1. Seed Categories
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

        // 2. Seed Suppliers
        var suppliers = new List<Supplier>
        {
            new()
            {
                Code = "SUP-TECH-CORP",
                Name = "Apex Technology Components Ltd",
                ContactName = "David Chen",
                Email = "orders@apextechcomponents.com",
                Phone = "+1-800-555-0199",
                LeadTimeDays = 5,
                PaymentTerms = "Net 30",
                IsActive = true
            },
            new()
            {
                Code = "SUP-OFFICE-DIR",
                Name = "National Office Direct Wholesale",
                ContactName = "Sarah Jenkins",
                Email = "sales@nationalofficedirect.com",
                Phone = "+1-888-555-4422",
                LeadTimeDays = 3,
                PaymentTerms = "Net 45",
                IsActive = true
            },
            new()
            {
                Code = "SUP-IND-SUPPLY",
                Name = "Titan Industrial Equipment Supply",
                ContactName = "Marcus Vance",
                Email = "procurement@titanindustrialsupply.com",
                Phone = "+1-877-555-8833",
                LeadTimeDays = 8,
                PaymentTerms = "Net 30",
                IsActive = true
            }
        };

        await context.Suppliers.AddRangeAsync(suppliers);
        await context.SaveChangesAsync();

        var supTech = suppliers[0].Id;
        var supOffice = suppliers[1].Id;
        var supInd = suppliers[2].Id;

        // 3. Seed Warehouses
        var warehouses = new List<Warehouse>
        {
            new()
            {
                Code = "WH-EAST",
                Name = "Atlanta Regional Fulfillment Hub",
                Address = "4800 Logistics Parkway",
                City = "Atlanta",
                State = "GA",
                PostalCode = "30336",
                Country = "USA",
                CapacityUnits = 25000,
                IsActive = true
            },
            new()
            {
                Code = "WH-WEST",
                Name = "Reno Distribution Center",
                Address = "1200 Tahoe Logistics Way",
                City = "Reno",
                State = "NV",
                PostalCode = "89502",
                Country = "USA",
                CapacityUnits = 30000,
                IsActive = true
            },
            new()
            {
                Code = "WH-CENTRAL",
                Name = "Dallas Logistics Depot",
                Address = "8900 DFW Trade Center Blvd",
                City = "Dallas",
                State = "TX",
                PostalCode = "75261",
                Country = "USA",
                CapacityUnits = 20000,
                IsActive = true
            }
        };

        await context.Warehouses.AddRangeAsync(warehouses);
        await context.SaveChangesAsync();

        var whEast = warehouses[0].Id;
        var whWest = warehouses[1].Id;
        var whCentral = warehouses[2].Id;

        // 4. Seed Products with Supplier Links
        var products = new List<Product>
        {
            new()
            {
                Sku = "ELEC-MON-4K27",
                Name = "27-inch 4K UHD IPS Monitor",
                Description = "Color-accurate 60Hz 4K designer monitor with USB-C 90W PD",
                CategoryId = elec,
                PrimarySupplierId = supTech,
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
                PrimarySupplierId = supTech,
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
                PrimarySupplierId = supTech,
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
                PrimarySupplierId = supOffice,
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
                PrimarySupplierId = supOffice,
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
                PrimarySupplierId = supInd,
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
                PrimarySupplierId = supOffice,
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
                PrimarySupplierId = supInd,
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

        // 5. Seed Warehouse Stock Distribution
        var warehouseStocks = new List<WarehouseStock>
        {
            new() { WarehouseId = whEast, ProductId = products[0].Id, QuantityOnHand = 25, BinLocation = "A-01-01" },
            new() { WarehouseId = whWest, ProductId = products[0].Id, QuantityOnHand = 15, BinLocation = "W-12-04" },
            new() { WarehouseId = whCentral, ProductId = products[0].Id, QuantityOnHand = 5, BinLocation = "C-05-10" },

            new() { WarehouseId = whEast, ProductId = products[1].Id, QuantityOnHand = 60, BinLocation = "A-02-14" },
            new() { WarehouseId = whWest, ProductId = products[1].Id, QuantityOnHand = 60, BinLocation = "W-10-02" },

            new() { WarehouseId = whEast, ProductId = products[2].Id, QuantityOnHand = 8, BinLocation = "A-03-08" },

            new() { WarehouseId = whEast, ProductId = products[3].Id, QuantityOnHand = 200, BinLocation = "B-01-01" },
            new() { WarehouseId = whWest, ProductId = products[3].Id, QuantityOnHand = 150, BinLocation = "W-01-01" },
            new() { WarehouseId = whCentral, ProductId = products[3].Id, QuantityOnHand = 100, BinLocation = "C-01-01" },

            new() { WarehouseId = whWest, ProductId = products[5].Id, QuantityOnHand = 20, BinLocation = "W-08-05" },
            new() { WarehouseId = whCentral, ProductId = products[5].Id, QuantityOnHand = 8, BinLocation = "C-04-12" },

            new() { WarehouseId = whEast, ProductId = products[6].Id, QuantityOnHand = 40, BinLocation = "B-10-01" },
            new() { WarehouseId = whCentral, ProductId = products[6].Id, QuantityOnHand = 30, BinLocation = "C-09-02" },

            new() { WarehouseId = whCentral, ProductId = products[7].Id, QuantityOnHand = 5, BinLocation = "C-02-04" }
        };

        await context.WarehouseStocks.AddRangeAsync(warehouseStocks);
        await context.SaveChangesAsync();

        // 6. Seed Initial Stock Transactions
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

        // 7. Seed Sample Stock Transfer
        var sampleTransfer = new StockTransfer
        {
            TransferNumber = "TRF-2026-0001",
            SourceWarehouseId = whEast,
            DestinationWarehouseId = whCentral,
            Status = StockTransferStatus.InTransit,
            RequestedBy = "logistics_admin",
            TrackingNumber = "FDX-994821034",
            Notes = "Rebalancing USB hubs and monitors to Dallas depot",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
            ShippedAtUtc = DateTime.UtcNow.AddDays(-1),
            Items = new List<StockTransferItem>
            {
                new()
                {
                    ProductId = products[0].Id,
                    QuantityRequested = 5,
                    QuantityShipped = 5,
                    QuantityReceived = 0,
                    UnitCost = products[0].UnitCost
                }
            }
        };

        await context.StockTransfers.AddAsync(sampleTransfer);
        await context.SaveChangesAsync();

        // 8. Seed Sample Purchase Order
        var samplePo = new PurchaseOrder
        {
            OrderNumber = "PO-2026-0101",
            SupplierId = supTech,
            DestinationWarehouseId = whEast,
            Status = PurchaseOrderStatus.Submitted,
            IsAutoGenerated = false,
            TotalEstimatedCost = 2100.00m,
            ExpectedDeliveryDateUtc = DateTime.UtcNow.AddDays(4),
            CreatedBy = "procurement_lead",
            Notes = "Q3 Replenishment for USB-C Docking Hubs",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
            SubmittedAtUtc = DateTime.UtcNow.AddDays(-2),
            Items = new List<PurchaseOrderItem>
            {
                new()
                {
                    ProductId = products[2].Id,
                    QuantityOrdered = 75,
                    QuantityReceived = 0,
                    UnitCost = 28.00m
                }
            }
        };

        await context.PurchaseOrders.AddAsync(samplePo);
        await context.SaveChangesAsync();
    }
}
