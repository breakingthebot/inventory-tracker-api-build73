// src/InventoryTracker.Api/Controllers/WarehousesController.cs
// REST controller for physical warehouse facility management, capacity monitoring, and stock locations.
// Connects to: src/InventoryTracker.Api/Services/IWarehouseService.cs, src/InventoryTracker.Api/DTOs/WarehouseDtos.cs
// Created: 2026-08-27

using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryTracker.Api.Controllers;

/// <summary>
/// Manages warehouse facilities, capacity utilization, and location-specific inventory.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class WarehousesController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;

    public WarehousesController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    /// <summary>
    /// Retrieves all registered warehouse facilities.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WarehouseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWarehouses([FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var warehouses = await _warehouseService.GetWarehousesAsync(activeOnly, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<WarehouseDto>>.Ok(warehouses, $"Retrieved {warehouses.Count} warehouse facilities."));
    }

    /// <summary>
    /// Retrieves a single warehouse facility by its unique database ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWarehouseById(int id, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseService.GetWarehouseByIdAsync(id, cancellationToken);
        if (warehouse == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Warehouse with ID {id} was not found."));
        }

        return Ok(ApiResponse<WarehouseDto>.Ok(warehouse));
    }

    /// <summary>
    /// Retrieves a single warehouse facility by its unique code (e.g. WH-EAST).
    /// </summary>
    [HttpGet("code/{code}")]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWarehouseByCode(string code, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseService.GetWarehouseByCodeAsync(code, cancellationToken);
        if (warehouse == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Warehouse with code '{code}' was not found."));
        }

        return Ok(ApiResponse<WarehouseDto>.Ok(warehouse));
    }

    /// <summary>
    /// Registers a new warehouse facility.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseDto dto, CancellationToken cancellationToken)
    {
        var created = await _warehouseService.CreateWarehouseAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetWarehouseById), new { id = created.Id },
            ApiResponse<WarehouseDto>.Ok(created, "Warehouse facility registered successfully."));
    }

    /// <summary>
    /// Updates warehouse facility metadata and capacity limits.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateWarehouse(int id, [FromBody] UpdateWarehouseDto dto, CancellationToken cancellationToken)
    {
        var updated = await _warehouseService.UpdateWarehouseAsync(id, dto, cancellationToken);
        if (updated == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Warehouse with ID {id} was not found."));
        }

        return Ok(ApiResponse<WarehouseDto>.Ok(updated, "Warehouse facility updated successfully."));
    }

    /// <summary>
    /// Retrieves current product stock levels and bin locations within a specific warehouse.
    /// </summary>
    [HttpGet("{id:int}/stock")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WarehouseStockDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWarehouseStock(int id, CancellationToken cancellationToken)
    {
        var stock = await _warehouseService.GetWarehouseStockAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<WarehouseStockDto>>.Ok(stock, $"Retrieved {stock.Count} product stock lines for warehouse {id}."));
    }

    /// <summary>
    /// Assigns or updates a product's bin location coordinate in a specific warehouse.
    /// </summary>
    [HttpPut("{id:int}/stock/{productId:int}/bin")]
    [ProducesResponseType(typeof(ApiResponse<WarehouseStockDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetBinLocation(int id, int productId, [FromBody] SetBinLocationDto dto, CancellationToken cancellationToken)
    {
        var updated = await _warehouseService.SetBinLocationAsync(id, productId, dto.BinLocation, cancellationToken);
        if (updated == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Warehouse {id} or Product {productId} was not found."));
        }

        return Ok(ApiResponse<WarehouseStockDto>.Ok(updated, "Bin location updated successfully."));
    }
}
