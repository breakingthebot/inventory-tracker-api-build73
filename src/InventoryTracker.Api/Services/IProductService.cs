// src/InventoryTracker.Api/Services/IProductService.cs
// Defines service interface contracts for managing catalog products and SKU validation.
// Connects to: src/InventoryTracker.Api/Services/ProductService.cs, src/InventoryTracker.Api/Controllers/ProductsController.cs
// Created: 2026-08-26

using InventoryTracker.Api.DTOs;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service contract for product catalog operations and SKU lookups.
/// </summary>
public interface IProductService
{
    Task<PagedResult<ProductDto>> GetProductsAsync(ProductFilterDto filter, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetProductBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductDto>> GetLowStockProductsAsync(CancellationToken cancellationToken = default);
    Task<ProductDto> CreateProductAsync(CreateProductDto dto, CancellationToken cancellationToken = default);
    Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> SkuExistsAsync(string sku, int? excludeId = null, CancellationToken cancellationToken = default);
}
