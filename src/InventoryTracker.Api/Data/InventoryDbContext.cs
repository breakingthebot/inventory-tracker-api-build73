// src/InventoryTracker.Api/Data/InventoryDbContext.cs
// Entity Framework Core database context configuring entity mappings, indexes, and precision.
// Connects to: src/InventoryTracker.Api/Models/*, src/InventoryTracker.Api/Program.cs
// Created: 2026-08-26

using InventoryTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryTracker.Api.Data;

/// <summary>
/// Entity Framework Core database context managing persistence for products, categories, warehouses, and transfers.
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

            entity.HasIndex(p => p.CategoryId);
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

        // WarehouseStock Configuration (Composite uniqueness per Warehouse + Product)
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
    }
}
