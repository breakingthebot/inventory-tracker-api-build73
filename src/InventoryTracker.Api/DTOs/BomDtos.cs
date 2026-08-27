// src/InventoryTracker.Api/DTOs/BomDtos.cs
// Data Transfer Objects for Bill of Materials (BOM) recipes, kit assembly yields, and disassembly.
// Connects to: src/InventoryTracker.Api/Services/IBomService.cs, src/InventoryTracker.Api/Controllers/BomController.cs
// Created: 2026-08-27

using System.ComponentModel.DataAnnotations;

namespace InventoryTracker.Api.DTOs;

/// <summary>
/// Data contract returned for a BOM component line item within a parent product kit.
/// </summary>
public class BomComponentDto
{
    public int Id { get; set; }
    public int ParentProductId { get; set; }
    public int ComponentProductId { get; set; }
    public string ComponentSku { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public decimal ComponentUnitCost { get; set; }
    public int QuantityRequired { get; set; }
    public decimal ScrapPercentage { get; set; }
    public decimal ExtendedCost => Math.Round(ComponentUnitCost * QuantityRequired * (1 + (ScrapPercentage / 100m)), 2);
    public int AvailableComponentStock { get; set; }
    public int MaxKitsFromThisComponent => QuantityRequired > 0 ? AvailableComponentStock / QuantityRequired : 0;
    public string? Notes { get; set; }
}

/// <summary>
/// Request payload to attach a sub-component product to a parent kit recipe.
/// </summary>
public class CreateBomComponentDto
{
    [Required(ErrorMessage = "ParentProductId is required.")]
    public int ParentProductId { get; set; }

    [Required(ErrorMessage = "ComponentProductId is required.")]
    public int ComponentProductId { get; set; }

    [Range(1, 10000, ErrorMessage = "QuantityRequired must be at least 1.")]
    public int QuantityRequired { get; set; } = 1;

    [Range(0, 100, ErrorMessage = "ScrapPercentage must be between 0 and 100.")]
    public decimal ScrapPercentage { get; set; } = 0m;

    [StringLength(300, ErrorMessage = "Notes cannot exceed 300 characters.")]
    public string? Notes { get; set; }
}

/// <summary>
/// Request payload to update an existing BOM component quantity or scrap tolerance.
/// </summary>
public class UpdateBomComponentDto
{
    [Range(1, 10000, ErrorMessage = "QuantityRequired must be at least 1.")]
    public int QuantityRequired { get; set; }

    [Range(0, 100, ErrorMessage = "ScrapPercentage must be between 0 and 100.")]
    public decimal ScrapPercentage { get; set; }

    [StringLength(300, ErrorMessage = "Notes cannot exceed 300 characters.")]
    public string? Notes { get; set; }
}

/// <summary>
/// Comprehensive BOM recipe summary with financial cost roll-up and maximum assemblable yield analytics.
/// </summary>
public class ProductBomDetailsDto
{
    public int ParentProductId { get; set; }
    public string ParentSku { get; set; } = string.Empty;
    public string ParentName { get; set; } = string.Empty;
    public decimal ParentUnitPrice { get; set; }
    public decimal RolledUpMaterialCost { get; set; }
    public int? WarehouseId { get; set; }
    public string? WarehouseCode { get; set; }
    public int MaxAssemblableKits { get; set; }
    public string? LimitingComponentSku { get; set; }
    public IReadOnlyList<BomComponentDto> Components { get; set; } = new List<BomComponentDto>();
}

/// <summary>
/// Request payload to execute an assembly run consuming sub-components into finished parent kits.
/// </summary>
public class AssembleKitRequestDto
{
    [Required(ErrorMessage = "KitProductId is required.")]
    public int KitProductId { get; set; }

    [Required(ErrorMessage = "WarehouseId is required.")]
    public int WarehouseId { get; set; }

    [Range(1, 10000, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }

    [Range(0, 1000000, ErrorMessage = "LaborCost must be non-negative.")]
    public decimal LaborCost { get; set; } = 0m;

    public string? AssembledBy { get; set; }

    public string? Notes { get; set; }
}

/// <summary>
/// Result returned after executing a kit assembly run.
/// </summary>
public class AssembleKitResultDto
{
    public string AssemblyNumber { get; set; } = string.Empty;
    public int KitProductId { get; set; }
    public string KitSku { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public int QuantityAssembled { get; set; }
    public decimal RolledUpUnitCost { get; set; }
    public int KitNewQuantityOnHand { get; set; }
    public IReadOnlyList<ComponentDeductionSummaryDto> ComponentsConsumed { get; set; } = new List<ComponentDeductionSummaryDto>();
}

/// <summary>
/// Summary of sub-component quantities consumed during kit assembly.
/// </summary>
public class ComponentDeductionSummaryDto
{
    public int ComponentProductId { get; set; }
    public string ComponentSku { get; set; } = string.Empty;
    public int QuantityDeducted { get; set; }
    public int RemainingComponentStock { get; set; }
}

/// <summary>
/// Request payload to disassemble an assembled kit back into individual sub-components.
/// </summary>
public class DisassembleKitRequestDto
{
    [Required(ErrorMessage = "KitProductId is required.")]
    public int KitProductId { get; set; }

    [Required(ErrorMessage = "WarehouseId is required.")]
    public int WarehouseId { get; set; }

    [Range(1, 10000, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }

    public string? DisassembledBy { get; set; }

    public string? Reason { get; set; }
}

/// <summary>
/// Result returned after disassembling parent kits back into component inventory.
/// </summary>
public class DisassembleKitResultDto
{
    public int KitProductId { get; set; }
    public string KitSku { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public int QuantityDisassembled { get; set; }
    public int KitRemainingStock { get; set; }
    public IReadOnlyList<ComponentDeductionSummaryDto> ComponentsReturned { get; set; } = new List<ComponentDeductionSummaryDto>();
}
