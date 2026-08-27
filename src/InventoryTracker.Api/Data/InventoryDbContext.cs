// src/InventoryTracker.Api/Data/InventoryDbContext.cs
// Entity Framework Core database context configuring entity mappings, indexes, and precision.
// Connects to: src/InventoryTracker.Api/Models/*, src/InventoryTracker.Api/Program.cs
// Created: 2026-08-26

using InventoryTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryTracker.Api.Data;

/// <summary>
/// Entity Framework Core database context managing persistence for products, categories, warehouses, suppliers, and purchase orders.
/// </summary>
public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<User> Users => Set<User>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDeliveryLog> WebhookDeliveryLogs => Set<WebhookDeliveryLog>();
    public DbSet<ProductLot> ProductLots => Set<ProductLot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Category Configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(c => c.Name).IsUnique();
            entity.Property(c => c.Description).HasMaxLength(500);
        });

        // Supplier Configuration
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Code).IsRequired().HasMaxLength(20);
            entity.HasIndex(s => s.Code).IsUnique();
            entity.Property(s => s.Name).IsRequired().HasMaxLength(100);
            entity.Property(s => s.Email).IsRequired().HasMaxLength(150);
            entity.Property(s => s.Phone).HasMaxLength(30);
            entity.Property(s => s.ContactName).HasMaxLength(100);
            entity.Property(s => s.PaymentTerms).HasMaxLength(50).HasDefaultValue("Net 30");
            entity.HasIndex(s => s.IsActive);
        });

        // Product Configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Sku).IsRequired().HasMaxLength(50);
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.Property(p => p.Name).IsRequired().HasMaxLength(150);
            entity.Property(p => p.Description).HasMaxLength(1000);
            entity.Property(p => p.UnitOfMeasure).HasMaxLength(20).HasDefaultValue("pcs");

            entity.Property(p => p.UnitPrice).HasPrecision(18, 2);
            entity.Property(p => p.UnitCost).HasPrecision(18, 2);

            entity.HasOne(p => p.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.PrimarySupplier)
                  .WithMany(s => s.SourcedProducts)
                  .HasForeignKey(p => p.PrimarySupplierId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(p => p.CategoryId);
            entity.HasIndex(p => p.PrimarySupplierId);
            entity.HasIndex(p => p.IsActive);
        });

        // InventoryTransaction Configuration
        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Reason).IsRequired().HasMaxLength(250);
            entity.Property(t => t.ReferenceNumber).HasMaxLength(100);
            entity.Property(t => t.PerformedBy).HasMaxLength(100);
            entity.Property(t => t.UnitCost).HasPrecision(18, 2);

            entity.HasOne(t => t.Product)
                  .WithMany(p => p.Transactions)
                  .HasForeignKey(t => t.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(t => t.ProductId);
            entity.HasIndex(t => t.TimestampUtc);
            entity.HasIndex(t => t.Type);
        });

        // Warehouse Configuration
        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.Property(w => w.Code).IsRequired().HasMaxLength(20);
            entity.HasIndex(w => w.Code).IsUnique();
            entity.Property(w => w.Name).IsRequired().HasMaxLength(100);
            entity.Property(w => w.Address).HasMaxLength(150);
            entity.Property(w => w.City).HasMaxLength(50);
            entity.Property(w => w.State).HasMaxLength(50);
            entity.Property(w => w.PostalCode).HasMaxLength(20);
            entity.Property(w => w.Country).HasMaxLength(50).HasDefaultValue("USA");
            entity.HasIndex(w => w.IsActive);
        });

        // WarehouseStock Configuration
        modelBuilder.Entity<WarehouseStock>(entity =>
        {
            entity.HasKey(ws => ws.Id);
            entity.HasIndex(ws => new { ws.WarehouseId, ws.ProductId }).IsUnique();
            entity.Property(ws => ws.BinLocation).HasMaxLength(50).HasDefaultValue("UNASSIGNED");

            entity.HasOne(ws => ws.Warehouse)
                  .WithMany(w => w.StockLevels)
                  .HasForeignKey(ws => ws.WarehouseId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ws => ws.Product)
                  .WithMany(p => p.WarehouseStocks)
                  .HasForeignKey(ws => ws.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // StockTransfer Configuration
        modelBuilder.Entity<StockTransfer>(entity =>
        {
            entity.HasKey(st => st.Id);
            entity.Property(st => st.TransferNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(st => st.TransferNumber).IsUnique();
            entity.Property(st => st.RequestedBy).HasMaxLength(100);
            entity.Property(st => st.TrackingNumber).HasMaxLength(100);
            entity.Property(st => st.Notes).HasMaxLength(500);

            entity.HasOne(st => st.SourceWarehouse)
                  .WithMany(w => w.OutboundTransfers)
                  .HasForeignKey(st => st.SourceWarehouseId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(st => st.DestinationWarehouse)
                  .WithMany(w => w.InboundTransfers)
                  .HasForeignKey(st => st.DestinationWarehouseId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(st => st.Status);
            entity.HasIndex(st => st.CreatedAtUtc);
        });

        // StockTransferItem Configuration
        modelBuilder.Entity<StockTransferItem>(entity =>
        {
            entity.HasKey(sti => sti.Id);
            entity.Property(sti => sti.UnitCost).HasPrecision(18, 2);

            entity.HasOne(sti => sti.StockTransfer)
                  .WithMany(st => st.Items)
                  .HasForeignKey(sti => sti.StockTransferId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(sti => sti.Product)
                  .WithMany(p => p.TransferItems)
                  .HasForeignKey(sti => sti.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // PurchaseOrder Configuration
        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(po => po.Id);
            entity.Property(po => po.OrderNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(po => po.OrderNumber).IsUnique();
            entity.Property(po => po.CreatedBy).HasMaxLength(100);
            entity.Property(po => po.Notes).HasMaxLength(500);
            entity.Property(po => po.TotalEstimatedCost).HasPrecision(18, 2);

            entity.HasOne(po => po.Supplier)
                  .WithMany(s => s.PurchaseOrders)
                  .HasForeignKey(po => po.SupplierId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(po => po.DestinationWarehouse)
                  .WithMany()
                  .HasForeignKey(po => po.DestinationWarehouseId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(po => po.Status);
            entity.HasIndex(po => po.CreatedAtUtc);
        });

        // PurchaseOrderItem Configuration
        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.HasKey(poi => poi.Id);
            entity.Property(poi => poi.UnitCost).HasPrecision(18, 2);

            entity.HasOne(poi => poi.PurchaseOrder)
                  .WithMany(po => po.Items)
                  .HasForeignKey(poi => poi.PurchaseOrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(poi => poi.Product)
                  .WithMany()
                  .HasForeignKey(poi => poi.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Salt).IsRequired();
            entity.HasIndex(u => u.Role);
            entity.HasIndex(u => u.IsActive);
        });

        // WebhookSubscription Configuration
        modelBuilder.Entity<WebhookSubscription>(entity =>
        {
            entity.HasKey(ws => ws.Id);
            entity.Property(ws => ws.Name).IsRequired().HasMaxLength(100);
            entity.Property(ws => ws.TargetUrl).IsRequired().HasMaxLength(300);
            entity.Property(ws => ws.SecretKey).IsRequired().HasMaxLength(100);
            entity.Property(ws => ws.SubscribedEvents).IsRequired().HasMaxLength(200).HasDefaultValue("*");
            entity.HasIndex(ws => ws.IsActive);
        });

        // WebhookDeliveryLog Configuration
        modelBuilder.Entity<WebhookDeliveryLog>(entity =>
        {
            entity.HasKey(wdl => wdl.Id);
            entity.Property(wdl => wdl.PayloadJson).IsRequired();
            entity.Property(wdl => wdl.ErrorMessage).HasMaxLength(500);

            entity.HasOne(wdl => wdl.WebhookSubscription)
                  .WithMany(ws => ws.DeliveryLogs)
                  .HasForeignKey(wdl => wdl.WebhookSubscriptionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(wdl => wdl.WebhookSubscriptionId);
            entity.HasIndex(wdl => wdl.TimestampUtc);
            entity.HasIndex(wdl => wdl.IsSuccess);
        });

        // ProductLot Configuration
        modelBuilder.Entity<ProductLot>(entity =>
        {
            entity.HasKey(pl => pl.Id);
            entity.Property(pl => pl.LotNumber).IsRequired().HasMaxLength(50);
            entity.Property(pl => pl.Notes).HasMaxLength(500);

            entity.HasOne(pl => pl.Product)
                  .WithMany(p => p.ProductLots)
                  .HasForeignKey(pl => pl.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(pl => pl.Warehouse)
                  .WithMany(w => w.ProductLots)
                  .HasForeignKey(pl => pl.WarehouseId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(pl => new { pl.ProductId, pl.WarehouseId, pl.LotNumber }).IsUnique();
            entity.HasIndex(pl => pl.ExpirationDateUtc);
            entity.HasIndex(pl => pl.Status);
        });
    }
}
