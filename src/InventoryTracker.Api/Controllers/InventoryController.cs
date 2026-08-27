// src/InventoryTracker.Api/Controllers/InventoryController.cs
// REST controller for inventory adjustments, restock intake, dispatch fulfillment, and transaction history.
// Connects to: src/InventoryTracker.Api/Services/IInventoryService.cs, src/InventoryTracker.Api/DTOs/InventoryDtos.cs
// Created: 2026-08-26

using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryTracker.Api.Controllers;

/// <summary>
/// Manages physical stock adjustments, inbound restock, dispatch fulfillment, and transaction logs.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    /// <summary>
    /// Records a stock adjustment (positive or negative count variance).
    /// </summary>
    [HttpPost("adjust")]
    [ProducesResponseType(typeof(ApiResponse<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AdjustStock([FromBody] StockAdjustmentDto dto, CancellationToken cancellationToken)
    {
        var tx = await _inventoryService.AdjustStockAsync(dto, cancellationToken);
        return Ok(ApiResponse<TransactionDto>.Ok(tx, "Stock adjustment recorded successfully."));
    }

    /// <summary>
    /// Processes an inbound stock replenishment from a supplier.
    /// </summary>
    [HttpPost("restock")]
    [ProducesResponseType(typeof(ApiResponse<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Restock([FromBody] RestockRequestDto dto, CancellationToken cancellationToken)
    {
        var tx = await _inventoryService.RestockAsync(dto, cancellationToken);
        return Ok(ApiResponse<TransactionDto>.Ok(tx, "Inbound restock received and inventory updated."));
    }

    /// <summary>
    /// Processes an outbound stock dispatch / customer order fulfillment.
    /// </summary>
    [HttpPost("dispatch")]
    [ProducesResponseType(typeof(ApiResponse<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Dispatch([FromBody] DispatchRequestDto dto, CancellationToken cancellationToken)
    {
        var tx = await _inventoryService.DispatchAsync(dto, cancellationToken);
        return Ok(ApiResponse<TransactionDto>.Ok(tx, "Outbound dispatch fulfilled and inventory decremented."));
    }

    /// <summary>
    /// Retrieves a paginated list of inventory transactions with optional filtering.
    /// </summary>
    [HttpGet("transactions")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TransactionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions([FromQuery] TransactionFilterDto filter, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetTransactionsAsync(filter, cancellationToken);
        return Ok(ApiResponse<PagedResult<TransactionDto>>.Ok(result, $"Retrieved {result.Items.Count} transactions."));
    }

    /// <summary>
    /// Retrieves recent stock movement history for a specific product.
    /// </summary>
    [HttpGet("transactions/product/{productId:int}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TransactionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductTransactions(int productId, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var history = await _inventoryService.GetProductTransactionsAsync(productId, limit, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TransactionDto>>.Ok(history, $"Retrieved {history.Count} transactions for product {productId}."));
    }
}
