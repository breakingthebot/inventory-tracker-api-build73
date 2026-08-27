// src/InventoryTracker.Api/Controllers/CustomersController.cs
// REST controller for customer purchasing accounts management.
// Connects to: src/InventoryTracker.Api/Services/ISalesOrderService.cs, src/InventoryTracker.Api/DTOs/SalesOrderDtos.cs
// Created: 2026-08-27

using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryTracker.Api.Controllers;

/// <summary>
/// Manages customer account profiles, contact info, and shipping destinations.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ISalesOrderService _salesOrderService;

    public CustomersController(ISalesOrderService salesOrderService)
    {
        _salesOrderService = salesOrderService;
    }

    /// <summary>
    /// Retrieves all registered customer purchasing accounts.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CustomerDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomers(CancellationToken cancellationToken)
    {
        var customers = await _salesOrderService.GetCustomersAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CustomerDto>>.Ok(customers, $"Retrieved {customers.Count} customer accounts."));
    }

    /// <summary>
    /// Retrieves a specific customer account by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerById(int id, CancellationToken cancellationToken)
    {
        var customer = await _salesOrderService.GetCustomerByIdAsync(id, cancellationToken);
        if (customer == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Customer with ID {id} was not found."));
        }

        return Ok(ApiResponse<CustomerDto>.Ok(customer));
    }

    /// <summary>
    /// Registers a new customer purchasing account.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto, CancellationToken cancellationToken)
    {
        var created = await _salesOrderService.CreateCustomerAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetCustomerById), new { id = created.Id },
            ApiResponse<CustomerDto>.Ok(created, "Customer account registered successfully."));
    }

    /// <summary>
    /// Updates customer account profile and shipping details.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateCustomerDto dto, CancellationToken cancellationToken)
    {
        var updated = await _salesOrderService.UpdateCustomerAsync(id, dto, cancellationToken);
        return Ok(ApiResponse<CustomerDto>.Ok(updated, "Customer account updated successfully."));
    }
}
