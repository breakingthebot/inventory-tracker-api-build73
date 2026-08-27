// src/InventoryTracker.Api/Controllers/PurchaseOrdersController.cs
// REST controller for purchase order replenishment workflows, auto-reorder generation, and intake receiving.
// Connects to: src/InventoryTracker.Api/Services/IPurchaseOrderService.cs, src/InventoryTracker.Api/DTOs/PurchaseOrderDtos.cs
// Created: 2026-08-27

using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryTracker.Api.Controllers;

/// <summary>
/// Manages supplier purchase orders, low-stock reorder suggestions, automated batch generation, and intake receiving.
/// </summary>
[ApiController]
[Route("api/v1/purchase-orders")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _poService;

    public PurchaseOrdersController(IPurchaseOrderService poService)
    {
        _poService = poService;
    }

    /// <summary>
    /// Retrieves a paginated list of purchase orders with status and vendor filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PurchaseOrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPurchaseOrders([FromQuery] PurchaseOrderFilterDto filter, CancellationToken cancellationToken)
    {
        var result = await _poService.GetPurchaseOrdersAsync(filter, cancellationToken);
        return Ok(ApiResponse<PagedResult<PurchaseOrderDto>>.Ok(result, $"Retrieved {result.Items.Count} purchase orders."));
    }

    /// <summary>
    /// Retrieves a single purchase order by its integer database ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPurchaseOrderById(int id, CancellationToken cancellationToken)
    {
        var po = await _poService.GetPurchaseOrderByIdAsync(id, cancellationToken);
        if (po == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Purchase order with ID {id} was not found."));
        }

        return Ok(ApiResponse<PurchaseOrderDto>.Ok(po));
    }

    /// <summary>
    /// Analyzes catalog stock levels vs reorder thresholds and returns replenishment recommendations.
    /// </summary>
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AutoReorderSuggestionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReorderSuggestions(CancellationToken cancellationToken)
    {
        var suggestions = await _poService.GetAutoReorderSuggestionsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AutoReorderSuggestionDto>>.Ok(suggestions, $"Found {suggestions.Count} products requiring replenishment."));
    }

    /// <summary>
    /// Automatically groups low-stock items by primary vendor and generates draft replenishment purchase orders.
    /// </summary>
    [HttpPost("auto-generate")]
    [ProducesResponseType(typeof(ApiResponse<AutoGeneratePoResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AutoGeneratePurchaseOrders([FromQuery] int defaultDestinationWarehouseId = 1, CancellationToken cancellationToken = default)
    {
        var result = await _poService.AutoGeneratePurchaseOrdersAsync(defaultDestinationWarehouseId, cancellationToken);
        return Ok(ApiResponse<AutoGeneratePoResultDto>.Ok(result, $"Auto-generated {result.TotalPurchaseOrdersCreated} replenishment purchase orders."));
    }

    /// <summary>
    /// Drafts a new purchase order manually.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePurchaseOrder([FromBody] CreatePurchaseOrderDto dto, CancellationToken cancellationToken)
    {
        var po = await _poService.CreatePurchaseOrderAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetPurchaseOrderById), new { id = po.Id },
            ApiResponse<PurchaseOrderDto>.Ok(po, "Purchase order created successfully."));
    }

    /// <summary>
    /// Officially submits a draft purchase order to the vendor.
    /// </summary>
    [HttpPost("{id:int}/submit")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitPurchaseOrder(int id, CancellationToken cancellationToken)
    {
        var po = await _poService.SubmitPurchaseOrderAsync(id, cancellationToken);
        return Ok(ApiResponse<PurchaseOrderDto>.Ok(po, "Purchase order submitted to vendor."));
    }

    /// <summary>
    /// Records goods receipt against an open purchase order, incrementing inventory balances and recalculating unit costs.
    /// </summary>
    [HttpPost("{id:int}/receive")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveGoods(int id, [FromBody] ReceivePurchaseOrderDto dto, CancellationToken cancellationToken)
    {
        var po = await _poService.ReceivePurchaseOrderAsync(id, dto, cancellationToken);
        return Ok(ApiResponse<PurchaseOrderDto>.Ok(po, "Goods received and inventory balances incremented."));
    }

    /// <summary>
    /// Cancels an open purchase order.
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelPurchaseOrder(int id, [FromQuery] string reason = "Cancelled by procurement", CancellationToken cancellationToken = default)
    {
        var po = await _poService.CancelPurchaseOrderAsync(id, reason, cancellationToken);
        return Ok(ApiResponse<PurchaseOrderDto>.Ok(po, "Purchase order cancelled."));
    }
}
