// src/InventoryTracker.Api/Services/ProductService.cs
// Implementation of product catalog management, queries, and SKU uniqueness validation.
// Connects to: src/InventoryTracker.Api/Data/InventoryDbContext.cs, src/InventoryTracker.Api/Models/Product.cs
// Created: 2026-08-26

using InventoryTracker.Api.Data;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service executing product operations against Entity Framework Core.
/// </summary>
public class ProductService : IProductService
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<ProductService> _logger;

    public ProductService(InventoryDbContext context, ILogger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<ProductDto>> GetProductsAsync(ProductFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        // Keyword filter across Name, SKU, and Description
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(search) ||
                                     p.Sku.ToLower().Contains(search) ||
                                     (p.Description != null && p.Description.ToLower().Contains(search)));
        }

        // Category filter
        if (filter.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
        }

        // Low stock filter
        if (filter.LowStockOnly == true)
        {
            query = query.Where(p => p.QuantityInStock <= p.ReorderThreshold);
        }

        // In-stock only filter
        if (filter.InStockOnly == true)
        {
            query = query.Where(p => p.QuantityInStock > 0);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Sorting
        query = (filter.SortBy?.ToLower(), filter.SortDescending) switch
        {
            ("sku", false) => query.OrderBy(p => p.Sku),
            ("sku", true) => query.OrderByDescending(p => p.Sku),
            ("price", false) => query.OrderBy(p => p.UnitPrice),
            ("price", true) => query.OrderByDescending(p => p.UnitPrice),
            ("stock", false) => query.OrderBy(p => p.QuantityInStock),
            ("stock", true) => query.OrderByDescending(p => p.QuantityInStock),
            ("category", false) => query.OrderBy(p => p.Category!.Name),
            ("category", true) => query.OrderByDescending(p => p.Category!.Name),
            ("created", false) => query.OrderBy(p => p.CreatedAtUtc),
            ("created", true) => query.OrderByDescending(p => p.CreatedAtUtc),
            (_, true) => query.OrderByDescending(p => p.Name),
            _ => query.OrderBy(p => p.Name)
        };

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => MapToDto(p))
            .ToListAsync(cancellationToken);

        return PagedResult<ProductDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return product == null ? null : MapToDto(product);
    }

    public async Task<ProductDto?> GetProductBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        var normalizedSku = sku.Trim().ToUpperInvariant();
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Sku.ToUpper() == normalizedSku, cancellationToken);

        return product == null ? null : MapToDto(product);
    }

    public async Task<IReadOnlyList<ProductDto>> GetLowStockProductsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.QuantityInStock <= p.ReorderThreshold)
            .OrderBy(p => p.QuantityInStock)
            .Select(p => MapToDto(p))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        var normalizedSku = dto.Sku.Trim().ToUpperInvariant();

        if (await SkuExistsAsync(normalizedSku, null, cancellationToken))
        {
            throw new InvalidOperationException($"Product with SKU '{normalizedSku}' already exists.");
        }

        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new KeyNotFoundException($"Category with Id {dto.CategoryId} was not found.");
        }

        var product = new Product
        {
            Sku = normalizedSku,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            CategoryId = dto.CategoryId,
            UnitPrice = dto.UnitPrice,
            UnitCost = dto.UnitCost,
            QuantityInStock = dto.InitialQuantity,
            ReorderThreshold = dto.ReorderThreshold,
            ReorderQuantity = dto.ReorderQuantity,
            UnitOfMeasure = string.IsNullOrWhiteSpace(dto.UnitOfMeasure) ? "pcs" : dto.UnitOfMeasure.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _context.Products.AddAsync(product, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Record initial stock transaction if quantity > 0
        if (dto.InitialQuantity > 0)
        {
            var initialTx = new InventoryTransaction
            {
                ProductId = product.Id,
                Type = TransactionType.InitialStock,
                QuantityChange = dto.InitialQuantity,
                QuantityAfter = dto.InitialQuantity,
                UnitCost = dto.UnitCost,
                Reason = "Initial stock allocation at product creation",
                ReferenceNumber = "PROD-INIT",
                PerformedBy = "product_creator",
                TimestampUtc = DateTime.UtcNow
            };
            await _context.InventoryTransactions.AddAsync(initialTx, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Product created successfully: {Sku} - {Name} (ID: {Id})", product.Sku, product.Name, product.Id);

        // Reload with category navigation property
        await _context.Entry(product).Reference(p => p.Category).LoadAsync(cancellationToken);
        return MapToDto(product);
    }

    public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product == null)
        {
            return null;
        }

        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new KeyNotFoundException($"Category with Id {dto.CategoryId} was not found.");
        }

        product.Name = dto.Name.Trim();
        product.Description = dto.Description?.Trim();
        product.CategoryId = dto.CategoryId;
        product.UnitPrice = dto.UnitPrice;
        product.UnitCost = dto.UnitCost;
        product.ReorderThreshold = dto.ReorderThreshold;
        product.ReorderQuantity = dto.ReorderQuantity;
        product.UnitOfMeasure = string.IsNullOrWhiteSpace(dto.UnitOfMeasure) ? "pcs" : dto.UnitOfMeasure.Trim();
        product.IsActive = dto.IsActive;
        product.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product updated successfully: ID {Id} ({Sku})", product.Id, product.Sku);

        // Ensure category name is loaded
        await _context.Entry(product).Reference(p => p.Category).LoadAsync(cancellationToken);
        return MapToDto(product);
    }

    public async Task<bool> DeleteProductAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync(new object[] { id }, cancellationToken);
        if (product == null)
        {
            return false;
        }

        // Soft-delete by default if transactions exist or mark inactive
        if (product.QuantityInStock > 0)
        {
            throw new InvalidOperationException($"Cannot delete product '{product.Sku}' while it has on-hand stock ({product.QuantityInStock} units). Adjust stock to zero first.");
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product deleted: ID {Id} ({Sku})", id, product.Sku);
        return true;
    }

    public async Task<bool> SkuExistsAsync(string sku, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = sku.Trim().ToUpperInvariant();
        return await _context.Products.AnyAsync(p => p.Sku.ToUpper() == normalized && (!excludeId.HasValue || p.Id != excludeId.Value), cancellationToken);
    }

    private static ProductDto MapToDto(Product p) => new()
    {
        Id = p.Id,
        Sku = p.Sku,
        Name = p.Name,
        Description = p.Description,
        CategoryId = p.CategoryId,
        CategoryName = p.Category?.Name ?? "Uncategorized",
        UnitPrice = p.UnitPrice,
        UnitCost = p.UnitCost,
        QuantityInStock = p.QuantityInStock,
        ReorderThreshold = p.ReorderThreshold,
        ReorderQuantity = p.ReorderQuantity,
        UnitOfMeasure = p.UnitOfMeasure,
        IsActive = p.IsActive,
        CreatedAtUtc = p.CreatedAtUtc,
        UpdatedAtUtc = p.UpdatedAtUtc
    };
}
