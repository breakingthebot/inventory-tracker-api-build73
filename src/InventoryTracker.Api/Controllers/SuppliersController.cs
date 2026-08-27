// src/InventoryTracker.Api/Controllers/SuppliersController.cs
// REST controller for vendor supplier registration, directory queries, and contact updates.
// Connects to: src/InventoryTracker.Api/Services/ISupplierService.cs, src/InventoryTracker.Api/DTOs/SupplierDtos.cs
// Created: 2026-08-27

using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryTracker.Api.Controllers;

/// <summary>
/// Manages vendor supplier profiles, procurement contacts, and sourcing contracts.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    /// <summary>
    /// Retrieves all registered vendor suppliers.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SupplierDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuppliers([FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var suppliers = await _supplierService.GetSuppliersAsync(activeOnly, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SupplierDto>>.Ok(suppliers, $"Retrieved {suppliers.Count} suppliers."));
    }

    /// <summary>
    /// Retrieves a single vendor supplier by its database ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<SupplierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSupplierById(int id, CancellationToken cancellationToken)
    {
        var supplier = await _supplierService.GetSupplierByIdAsync(id, cancellationToken);
        if (supplier == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Supplier with ID {id} was not found."));
        }

        return Ok(ApiResponse<SupplierDto>.Ok(supplier));
    }

    /// <summary>
    /// Retrieves a single vendor supplier by its unique vendor code (e.g. SUP-TECH-CORP).
    /// </summary>
    [HttpGet("code/{code}")]
    [ProducesResponseType(typeof(ApiResponse<SupplierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSupplierByCode(string code, CancellationToken cancellationToken)
    {
        var supplier = await _supplierService.GetSupplierByCodeAsync(code, cancellationToken);
        if (supplier == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Supplier with code '{code}' was not found."));
        }

        return Ok(ApiResponse<SupplierDto>.Ok(supplier));
    }

    /// <summary>
    /// Registers a new vendor supplier.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SupplierDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierDto dto, CancellationToken cancellationToken)
    {
        var created = await _supplierService.CreateSupplierAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetSupplierById), new { id = created.Id },
            ApiResponse<SupplierDto>.Ok(created, "Supplier registered successfully."));
    }

    /// <summary>
    /// Updates vendor supplier contact details, lead times, or payment terms.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<SupplierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSupplier(int id, [FromBody] UpdateSupplierDto dto, CancellationToken cancellationToken)
    {
        var updated = await _supplierService.UpdateSupplierAsync(id, dto, cancellationToken);
        if (updated == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Supplier with ID {id} was not found."));
        }

        return Ok(ApiResponse<SupplierDto>.Ok(updated, "Supplier updated successfully."));
    }
}
