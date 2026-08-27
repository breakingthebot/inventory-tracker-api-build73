// src/InventoryTracker.Api/Controllers/BomController.cs
// REST controller for Bill of Materials (BOM) management, component cost roll-ups, assembly runs, and disassembly.
// Connects to: src/InventoryTracker.Api/Services/IBomService.cs, src/InventoryTracker.Api/DTOs/BomDtos.cs
// Created: 2026-08-27

using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryTracker.Api.Controllers;

/// <summary>
/// Manages Bill of Materials (BOM) product recipes, component cost roll-ups, kit assembly production, and disassembly.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class BomController : ControllerBase
{
    private readonly IBomService _bomService;

    public BomController(IBomService bomService)
    {
        _bomService = bomService;
    }

    /// <summary>
    /// Retrieves full Bill of Materials (BOM) recipe details, component cost roll-ups, and maximum assemblable yield for a product kit.
    /// </summary>
    [HttpGet("product/{productId:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProductBomDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductBom(int productId, [FromQuery] int? warehouseId, CancellationToken cancellationToken)
    {
        var details = await _bomService.GetProductBomAsync(productId, warehouseId, cancellationToken);
        return Ok(ApiResponse<ProductBomDetailsDto>.Ok(details));
    }

    /// <summary>
    /// Adds or updates a sub-component product requirement in a parent product's BOM recipe.
    /// </summary>
    [HttpPost("components")]
    [ProducesResponseType(typeof(ApiResponse<BomComponentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddBomComponent([FromBody] CreateBomComponentDto dto, CancellationToken cancellationToken)
    {
        var result = await _bomService.AddBomComponentAsync(dto, cancellationToken);
        return Ok(ApiResponse<BomComponentDto>.Ok(result, "BOM component configured successfully."));
    }

    /// <summary>
    /// Removes a sub-component product requirement from a parent product's BOM recipe.
    /// </summary>
    [HttpDelete("components")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveBomComponent([FromQuery] int parentProductId, [FromQuery] int componentProductId, CancellationToken cancellationToken)
    {
        var removed = await _bomService.RemoveBomComponentAsync(parentProductId, componentProductId, cancellationToken);
        if (!removed)
        {
            return NotFound(ApiResponse<object>.Fail("BOM component link was not found."));
        }

        return Ok(ApiResponse<object>.Ok(new { parentProductId, componentProductId }, "BOM component removed successfully."));
    }

    /// <summary>
    /// Executes a kit assembly run, deducting sub-component inventory and receiving finished goods into warehouse stock.
    /// </summary>
    [HttpPost("assemble")]
    [ProducesResponseType(typeof(ApiResponse<AssembleKitResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssembleKit([FromBody] AssembleKitRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _bomService.AssembleKitAsync(dto, cancellationToken);
        return Ok(ApiResponse<AssembleKitResultDto>.Ok(result, "Kit assembly completed successfully."));
    }

    /// <summary>
    /// Disassembles finished parent kits back into raw sub-component inventory.
    /// </summary>
    [HttpPost("disassemble")]
    [ProducesResponseType(typeof(ApiResponse<DisassembleKitResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DisassembleKit([FromBody] DisassembleKitRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _bomService.DisassembleKitAsync(dto, cancellationToken);
        return Ok(ApiResponse<DisassembleKitResultDto>.Ok(result, "Kit disassembly completed successfully."));
    }
}
