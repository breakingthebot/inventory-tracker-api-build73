// src/InventoryTracker.Api/Controllers/SalesOrdersController.cs
// REST controller for sales order lifecycle management: drafting, stock allocation, pick lists, packing, and shipment.
// Connects to: src/InventoryTracker.Api/Services/ISalesOrderService.cs, src/InventoryTracker.Api/DTOs/SalesOrderDtos.cs
// Created: 2026-08-27

using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryTracker.Api.Controllers;

/// <summary>
/// Manages customer sales orders through the multi-stage pick-pack-ship fulfillment pipeline.
/// </summary>
[ApiController]
[Route("api/v1/sales-orders")]
public class SalesOrdersController : ControllerBase
{
    private readonly ISalesOrderService _salesOrderService;

    public SalesOrdersController(ISalesOrderService salesOrderService)
    {
        _salesOrderService = salesOrderService;
    }

    /// <summary>
    /// Retrieves sales orders filtered by lifecycle status, customer, or warehouse facility.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SalesOrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSalesOrders(
        [FromQuery] SalesOrderStatus? status,
        [FromQuery] int? customerId,
        [FromQuery] int? warehouseId,
        CancellationToken cancellationToken)
    {
        var orders = await _salesOrderService.GetSalesOrdersAsync(status, customerId, warehouseId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SalesOrderDto>>.Ok(orders, $"Retrieved {orders.Count} sales orders."));
    }

    /// <summary>
    /// Retrieves detailed information for a specific sales order including line items.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<SalesOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSalesOrderById(int id, CancellationToken cancellationToken)
    {
        var order = await _salesOrderService.GetSalesOrderByIdAsync(id, cancellationToken);
        if (order == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Sales order with ID {id} was not found."));
        }

        return Ok(ApiResponse<SalesOrderDto>.Ok(order));
    }

    /// <summary>
    /// Drafts a new customer sales order.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SalesOrderDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSalesOrder([FromBody] CreateSalesOrderDto dto, CancellationToken cancellationToken)
    {
        var created = await _salesOrderService.CreateSalesOrderAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetSalesOrderById), new { id = created.Id },
            ApiResponse<SalesOrderDto>.Ok(created, "Sales order drafted successfully."));
    }

    /// <summary>
    /// Allocates and reserves inventory stock at the fulfillment facility for an open order.
    /// </summary>
    [HttpPost("{id:int}/allocate")]
    [ProducesResponseType(typeof(ApiResponse<SalesOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AllocateOrder(int id, CancellationToken cancellationToken)
    {
        var allocated = await _salesOrderService.AllocateOrderAsync(id, cancellationToken);
        return Ok(ApiResponse<SalesOrderDto>.Ok(allocated, "Stock allocated and reserved for sales order."));
    }

    /// <summary>
    /// Generates a warehouse runner pick sheet sorted by physical aisle/rack/shelf bin coordinates.
    /// </summary>
    [HttpGet("{id:int}/pick-list")]
    [ProducesResponseType(typeof(ApiResponse<PickListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPickList(int id, CancellationToken cancellationToken)
    {
        var pickList = await _salesOrderService.GetPickListAsync(id, cancellationToken);
        return Ok(ApiResponse<PickListDto>.Ok(pickList));
    }

    /// <summary>
    /// Records physical warehouse picking completion for line items.
    /// </summary>
    [HttpPost("{id:int}/pick")]
    [ProducesResponseType(typeof(ApiResponse<SalesOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordPicking(int id, [FromBody] PickOrderDto dto, CancellationToken cancellationToken)
    {
        var updated = await _salesOrderService.RecordPickingAsync(id, dto, cancellationToken);
        return Ok(ApiResponse<SalesOrderDto>.Ok(updated, "Item picking recorded successfully."));
    }

    /// <summary>
    /// Records carton packing completion and assigns shipping carrier.
    /// </summary>
    [HttpPost("{id:int}/pack")]
    [ProducesResponseType(typeof(ApiResponse<SalesOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordPacking(int id, [FromBody] PackOrderDto dto, CancellationToken cancellationToken)
    {
        var packed = await _salesOrderService.RecordPackingAsync(id, dto, cancellationToken);
        return Ok(ApiResponse<SalesOrderDto>.Ok(packed, "Order packing completed and carrier assigned."));
    }

    /// <summary>
    /// Dispatches carrier shipment, deducts on-hand physical stock, releases reserved inventory, and logs audit movements.
    /// </summary>
    [HttpPost("{id:int}/ship")]
    [ProducesResponseType(typeof(ApiResponse<SalesOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ShipOrder(int id, [FromBody] ShipOrderDto dto, CancellationToken cancellationToken)
    {
        var shipped = await _salesOrderService.ShipOrderAsync(id, dto, cancellationToken);
        return Ok(ApiResponse<SalesOrderDto>.Ok(shipped, "Order shipped and inventory deducted successfully."));
    }

    /// <summary>
    /// Cancels an open sales order and releases any allocated/reserved stock back to sellable inventory.
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<SalesOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelOrder(int id, [FromQuery] string reason = "Customer request", CancellationToken cancellationToken = default)
    {
        var cancelled = await _salesOrderService.CancelOrderAsync(id, reason, cancellationToken);
        return Ok(ApiResponse<SalesOrderDto>.Ok(cancelled, "Sales order cancelled and reserved stock released."));
    }
}
