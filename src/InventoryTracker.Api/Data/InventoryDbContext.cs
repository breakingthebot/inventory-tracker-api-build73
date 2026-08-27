// src/InventoryTracker.Api/Data/InventoryDbContext.cs
// Entity Framework Core database context configuring entity mappings, indexes, and precision.
// Connects to: src/InventoryTracker.Api/Models/*, src/InventoryTracker.Api/Program.cs
// Created: 2026-08-26

using InventoryTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryTracker.Api.Data;

/// <summary>
/// Entity Framework Core database context managing persistence for products, categories, and stock transactions.
/// </summary>
public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

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
    }
}
