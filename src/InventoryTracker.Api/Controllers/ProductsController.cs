// src/InventoryTracker.Api/Controllers/ProductsController.cs
// REST controller exposing product CRUD endpoints, search, filtering, and low-stock alerts.
// Connects to: src/InventoryTracker.Api/Services/IProductService.cs, src/InventoryTracker.Api/DTOs/ProductDtos.cs
// Created: 2026-08-26

using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryTracker.Api.Controllers;

/// <summary>
/// Manages catalog products, SKU validation, stock levels, and search filters.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Retrieves a paginated list of catalog products with optional filtering and sorting.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts([FromQuery] ProductFilterDto filter, CancellationToken cancellationToken)
    {
        var result = await _productService.GetProductsAsync(filter, cancellationToken);
        return Ok(ApiResponse<PagedResult<ProductDto>>.Ok(result, $"Retrieved {result.Items.Count} products."));
    }

    /// <summary>
    /// Retrieves a single product by its unique integer database ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById(int id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductByIdAsync(id, cancellationToken);
        if (product == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Product with ID {id} was not found."));
        }

        return Ok(ApiResponse<ProductDto>.Ok(product));
    }

    /// <summary>
    /// Retrieves a single product by its unique SKU code.
    /// </summary>
    [HttpGet("sku/{sku}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductBySku(string sku, CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductBySkuAsync(sku, cancellationToken);
        if (product == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Product with SKU '{sku}' was not found."));
        }

        return Ok(ApiResponse<ProductDto>.Ok(product));
    }

    /// <summary>
    /// Retrieves all products whose on-hand quantity is currently at or below their reorder threshold.
    /// </summary>
    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStockProducts(CancellationToken cancellationToken)
    {
        var products = await _productService.GetLowStockProductsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ProductDto>>.Ok(products, $"Found {products.Count} low-stock products requiring replenishment."));
    }

    /// <summary>
    /// Creates a new product record in the catalog.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto, CancellationToken cancellationToken)
    {
        var product = await _productService.CreateProductAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetProductById), new { id = product.Id },
            ApiResponse<ProductDto>.Ok(product, "Product created successfully."));
    }

    /// <summary>
    /// Updates details of an existing product.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto dto, CancellationToken cancellationToken)
    {
        var updated = await _productService.UpdateProductAsync(id, dto, cancellationToken);
        if (updated == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Product with ID {id} was not found."));
        }

        return Ok(ApiResponse<ProductDto>.Ok(updated, "Product updated successfully."));
    }

    /// <summary>
    /// Deletes a product from the catalog if on-hand stock is zero.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(int id, CancellationToken cancellationToken)
    {
        var deleted = await _productService.DeleteProductAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(ApiResponse<object>.Fail($"Product with ID {id} was not found."));
        }

        return Ok(ApiResponse<object>.Ok(new { Id = id }, "Product deleted successfully."));
    }
}
