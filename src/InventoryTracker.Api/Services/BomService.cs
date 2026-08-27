// src/InventoryTracker.Api/Services/BomService.cs
// Implementation of BOM component trees, cost roll-ups, max yield analytics, and atomic assembly/disassembly.
// Connects to: src/InventoryTracker.Api/Data/InventoryDbContext.cs, src/InventoryTracker.Api/Models/BillOfMaterials.cs
// Created: 2026-08-27

using InventoryTracker.Api.Data;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service executing BOM component management, production cost roll-up calculations, and kit assembly workflows.
/// </summary>
public class BomService : IBomService
{
    private readonly InventoryDbContext _context;
    private readonly IWebhookService _webhookService;
    private readonly ILogger<BomService> _logger;

    public BomService(InventoryDbContext context, IWebhookService webhookService, ILogger<BomService> logger)
    {
        _context = context;
        _webhookService = webhookService;
        _logger = logger;
    }

    public async Task<BomComponentDto> AddBomComponentAsync(CreateBomComponentDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.ParentProductId == dto.ComponentProductId)
        {
            throw new InvalidOperationException("A product cannot be a sub-component of itself.");
        }

        var parent = await _context.Products.FindAsync(new object[] { dto.ParentProductId }, cancellationToken);
        if (parent == null)
        {
            throw new KeyNotFoundException($"Parent product with ID {dto.ParentProductId} was not found.");
        }

        var component = await _context.Products.FindAsync(new object[] { dto.ComponentProductId }, cancellationToken);
        if (component == null)
        {
            throw new KeyNotFoundException($"Component product with ID {dto.ComponentProductId} was not found.");
        }

        var existing = await _context.BillOfMaterials
            .FirstOrDefaultAsync(b => b.ParentProductId == dto.ParentProductId && b.ComponentProductId == dto.ComponentProductId, cancellationToken);

        if (existing != null)
        {
            existing.QuantityRequired = dto.QuantityRequired;
            existing.ScrapPercentage = dto.ScrapPercentage;
            existing.Notes = dto.Notes?.Trim();
        }
        else
        {
            existing = new BillOfMaterials
            {
                ParentProductId = dto.ParentProductId,
                ComponentProductId = dto.ComponentProductId,
                QuantityRequired = dto.QuantityRequired,
                ScrapPercentage = dto.ScrapPercentage,
                Notes = dto.Notes?.Trim()
            };
            await _context.BillOfMaterials.AddAsync(existing, cancellationToken);
        }

        parent.IsBundleOrKit = true;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added BOM Component {CompSku} (x{Qty}) to Parent Kit {ParentSku}",
            component.Sku, existing.QuantityRequired, parent.Sku);

        return new BomComponentDto
        {
            Id = existing.Id,
            ParentProductId = parent.Id,
            ComponentProductId = component.Id,
            ComponentSku = component.Sku,
            ComponentName = component.Name,
            ComponentUnitCost = component.UnitCost,
            QuantityRequired = existing.QuantityRequired,
            ScrapPercentage = existing.ScrapPercentage,
            AvailableComponentStock = component.QuantityInStock,
            Notes = existing.Notes
        };
    }

    public async Task<bool> RemoveBomComponentAsync(int parentProductId, int componentProductId, CancellationToken cancellationToken = default)
    {
        var existing = await _context.BillOfMaterials
            .FirstOrDefaultAsync(b => b.ParentProductId == parentProductId && b.ComponentProductId == componentProductId, cancellationToken);

        if (existing == null)
        {
            return false;
        }

        _context.BillOfMaterials.Remove(existing);

        var remainingCount = await _context.BillOfMaterials
            .CountAsync(b => b.ParentProductId == parentProductId && b.ComponentProductId != componentProductId, cancellationToken);

        if (remainingCount == 0)
        {
            var parent = await _context.Products.FindAsync(new object[] { parentProductId }, cancellationToken);
            if (parent != null)
            {
                parent.IsBundleOrKit = false;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Removed BOM Component ID {CompId} from Parent Product {ParentId}", componentProductId, parentProductId);
        return true;
    }

    public async Task<ProductBomDetailsDto> GetProductBomAsync(int parentProductId, int? warehouseId = null, CancellationToken cancellationToken = default)
    {
        var parent = await _context.Products.FindAsync(new object[] { parentProductId }, cancellationToken);
        if (parent == null)
        {
            throw new KeyNotFoundException($"Product with ID {parentProductId} was not found.");
        }

        Warehouse? warehouse = null;
        if (warehouseId.HasValue)
        {
            warehouse = await _context.Warehouses.FindAsync(new object[] { warehouseId.Value }, cancellationToken);
        }

        var components = await _context.BillOfMaterials
            .Include(b => b.ComponentProduct)
            .Where(b => b.ParentProductId == parentProductId)
            .ToListAsync(cancellationToken);

        var componentDtos = new List<BomComponentDto>();
        decimal totalMaterialCost = 0m;
        var maxKits = components.Count > 0 ? int.MaxValue : 0;
        string? limitingSku = null;

        foreach (var bom in components)
        {
            var comp = bom.ComponentProduct!;
            int availableStock;

            if (warehouseId.HasValue)
            {
                var whStock = await _context.WarehouseStocks
                    .FirstOrDefaultAsync(ws => ws.WarehouseId == warehouseId.Value && ws.ProductId == comp.Id, cancellationToken);
                availableStock = whStock?.AvailableQuantity ?? 0;
            }
            else
            {
                availableStock = comp.QuantityInStock;
            }

            var extendedCost = Math.Round(comp.UnitCost * bom.QuantityRequired * (1 + (bom.ScrapPercentage / 100m)), 2);
            totalMaterialCost += extendedCost;

            var possibleFromComp = bom.QuantityRequired > 0 ? availableStock / bom.QuantityRequired : 0;
            if (possibleFromComp < maxKits)
            {
                maxKits = possibleFromComp;
                limitingSku = comp.Sku;
            }

            componentDtos.Add(new BomComponentDto
            {
                Id = bom.Id,
                ParentProductId = parent.Id,
                ComponentProductId = comp.Id,
                ComponentSku = comp.Sku,
                ComponentName = comp.Name,
                ComponentUnitCost = comp.UnitCost,
                QuantityRequired = bom.QuantityRequired,
                ScrapPercentage = bom.ScrapPercentage,
                AvailableComponentStock = availableStock,
                Notes = bom.Notes
            });
        }

        if (components.Count == 0)
        {
            maxKits = 0;
        }

        return new ProductBomDetailsDto
        {
            ParentProductId = parent.Id,
            ParentSku = parent.Sku,
            ParentName = parent.Name,
            ParentUnitPrice = parent.UnitPrice,
            RolledUpMaterialCost = Math.Round(totalMaterialCost, 2),
            WarehouseId = warehouse?.Id,
            WarehouseCode = warehouse?.Code,
            MaxAssemblableKits = maxKits,
            LimitingComponentSku = limitingSku,
            Components = componentDtos
        };
    }

    public async Task<AssembleKitResultDto> AssembleKitAsync(AssembleKitRequestDto dto, CancellationToken cancellationToken = default)
    {
        var parent = await _context.Products.FindAsync(new object[] { dto.KitProductId }, cancellationToken);
        if (parent == null)
        {
            throw new KeyNotFoundException($"Parent kit product with ID {dto.KitProductId} was not found.");
        }

        var warehouse = await _context.Warehouses.FindAsync(new object[] { dto.WarehouseId }, cancellationToken);
        if (warehouse == null)
        {
            throw new KeyNotFoundException($"Warehouse with ID {dto.WarehouseId} was not found.");
        }

        var bomList = await _context.BillOfMaterials
            .Include(b => b.ComponentProduct)
            .Where(b => b.ParentProductId == dto.KitProductId)
            .ToListAsync(cancellationToken);

        if (bomList.Count == 0)
        {
            throw new InvalidOperationException($"Product {parent.Sku} does not have a configured Bill of Materials (BOM) recipe.");
        }

        // Validate stock availability for all components
        foreach (var bom in bomList)
        {
            var needed = dto.Quantity * bom.QuantityRequired;
            var compStock = await _context.WarehouseStocks
                .FirstOrDefaultAsync(ws => ws.WarehouseId == dto.WarehouseId && ws.ProductId == bom.ComponentProductId, cancellationToken);

            var available = compStock?.AvailableQuantity ?? 0;
            if (available < needed)
            {
                throw new InvalidOperationException(
                    $"Insufficient component stock for '{bom.ComponentProduct?.Sku}'. Needed: {needed}, Available: {available} at {warehouse.Code}.");
            }
        }

        var assemblyNumber = $"ASM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var deductions = new List<ComponentDeductionSummaryDto>();
        decimal totalComponentMaterialCost = 0m;

        // Deduct raw material components
        foreach (var bom in bomList)
        {
            var comp = bom.ComponentProduct!;
            var needed = dto.Quantity * bom.QuantityRequired;

            var whStock = await _context.WarehouseStocks
                .FirstAsync(ws => ws.WarehouseId == dto.WarehouseId && ws.ProductId == bom.ComponentProductId, cancellationToken);

            whStock.QuantityOnHand -= needed;
            whStock.UpdatedAtUtc = DateTime.UtcNow;
            comp.QuantityInStock -= needed;

            totalComponentMaterialCost += comp.UnitCost * needed;

            var compTx = new InventoryTransaction
            {
                ProductId = comp.Id,
                Type = TransactionType.StockOut,
                QuantityChange = -needed,
                QuantityAfter = comp.QuantityInStock,
                UnitCost = comp.UnitCost,
                ReferenceNumber = assemblyNumber,
                Reason = $"Consumed for assembly of {parent.Sku} (Qty: {dto.Quantity})",
                TimestampUtc = DateTime.UtcNow
            };
            await _context.InventoryTransactions.AddAsync(compTx, cancellationToken);

            deductions.Add(new ComponentDeductionSummaryDto
            {
                ComponentProductId = comp.Id,
                ComponentSku = comp.Sku,
                QuantityDeducted = needed,
                RemainingComponentStock = whStock.QuantityOnHand
            });
        }

        // Calculate rolled-up unit acquisition cost
        var totalRunCost = totalComponentMaterialCost + dto.LaborCost;
        var rolledUpUnitCost = Math.Round(totalRunCost / dto.Quantity, 2);

        // Receive finished kit goods into warehouse
        var parentWhStock = await _context.WarehouseStocks
            .FirstOrDefaultAsync(ws => ws.WarehouseId == dto.WarehouseId && ws.ProductId == parent.Id, cancellationToken);

        if (parentWhStock == null)
        {
            parentWhStock = new WarehouseStock
            {
                WarehouseId = dto.WarehouseId,
                ProductId = parent.Id,
                QuantityOnHand = dto.Quantity,
                QuantityReserved = 0,
                UpdatedAtUtc = DateTime.UtcNow
            };
            await _context.WarehouseStocks.AddAsync(parentWhStock, cancellationToken);
        }
        else
        {
            parentWhStock.QuantityOnHand += dto.Quantity;
            parentWhStock.UpdatedAtUtc = DateTime.UtcNow;
        }

        // Recalculate parent unit cost using weighted average
        if (parent.QuantityInStock + dto.Quantity > 0)
        {
            var existingTotalValuation = parent.QuantityInStock * parent.UnitCost;
            var newTotalValuation = existingTotalValuation + totalRunCost;
            parent.QuantityInStock += dto.Quantity;
            parent.UnitCost = Math.Round(newTotalValuation / parent.QuantityInStock, 2);
        }
        else
        {
            parent.QuantityInStock += dto.Quantity;
            parent.UnitCost = rolledUpUnitCost;
        }

        var kitTx = new InventoryTransaction
        {
            ProductId = parent.Id,
            Type = TransactionType.StockIn,
            QuantityChange = dto.Quantity,
            QuantityAfter = parent.QuantityInStock,
            UnitCost = rolledUpUnitCost,
            ReferenceNumber = assemblyNumber,
            Reason = $"Finished goods assembly receipt (Labor: ${dto.LaborCost})",
            TimestampUtc = DateTime.UtcNow
        };
        await _context.InventoryTransactions.AddAsync(kitTx, cancellationToken);

        // Record Assembly Log
        var assemblyOrder = new KitAssemblyOrder
        {
            AssemblyNumber = assemblyNumber,
            KitProductId = parent.Id,
            WarehouseId = dto.WarehouseId,
            QuantityAssembled = dto.Quantity,
            LaborCost = dto.LaborCost,
            TotalUnitCost = rolledUpUnitCost,
            AssembledBy = string.IsNullOrWhiteSpace(dto.AssembledBy) ? "operator" : dto.AssembledBy.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            Notes = dto.Notes?.Trim()
        };
        await _context.KitAssemblyOrders.AddAsync(assemblyOrder, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        await _webhookService.PublishEventAsync(WebhookEventType.StockAdjusted, new
        {
            AssemblyNumber = assemblyNumber,
            KitSku = parent.Sku,
            WarehouseCode = warehouse.Code,
            QuantityAssembled = dto.Quantity,
            TotalUnitCost = rolledUpUnitCost
        }, cancellationToken);

        _logger.LogInformation("Assembled {Qty} units of Kit {KitSku} at WH {WhCode} (Order: {OrderNum})",
            dto.Quantity, parent.Sku, warehouse.Code, assemblyNumber);

        return new AssembleKitResultDto
        {
            AssemblyNumber = assemblyNumber,
            KitProductId = parent.Id,
            KitSku = parent.Sku,
            WarehouseId = dto.WarehouseId,
            QuantityAssembled = dto.Quantity,
            RolledUpUnitCost = rolledUpUnitCost,
            KitNewQuantityOnHand = parentWhStock.QuantityOnHand,
            ComponentsConsumed = deductions
        };
    }

    public async Task<DisassembleKitResultDto> DisassembleKitAsync(DisassembleKitRequestDto dto, CancellationToken cancellationToken = default)
    {
        var parent = await _context.Products.FindAsync(new object[] { dto.KitProductId }, cancellationToken);
        if (parent == null)
        {
            throw new KeyNotFoundException($"Kit product with ID {dto.KitProductId} was not found.");
        }

        var warehouse = await _context.Warehouses.FindAsync(new object[] { dto.WarehouseId }, cancellationToken);
        if (warehouse == null)
        {
            throw new KeyNotFoundException($"Warehouse with ID {dto.WarehouseId} was not found.");
        }

        var parentWhStock = await _context.WarehouseStocks
            .FirstOrDefaultAsync(ws => ws.WarehouseId == dto.WarehouseId && ws.ProductId == dto.KitProductId, cancellationToken);

        if (parentWhStock == null || parentWhStock.AvailableQuantity < dto.Quantity)
        {
            throw new InvalidOperationException($"Insufficient uncommitted kit stock for '{parent.Sku}' at {warehouse.Code}.");
        }

        var bomList = await _context.BillOfMaterials
            .Include(b => b.ComponentProduct)
            .Where(b => b.ParentProductId == dto.KitProductId)
            .ToListAsync(cancellationToken);

        if (bomList.Count == 0)
        {
            throw new InvalidOperationException($"Product {parent.Sku} does not have a configured BOM recipe.");
        }

        var disasmRef = $"DISASM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

        // Deduct finished parent kit
        parentWhStock.QuantityOnHand -= dto.Quantity;
        parentWhStock.UpdatedAtUtc = DateTime.UtcNow;
        parent.QuantityInStock -= dto.Quantity;

        var kitTx = new InventoryTransaction
        {
            ProductId = parent.Id,
            Type = TransactionType.StockOut,
            QuantityChange = -dto.Quantity,
            QuantityAfter = parent.QuantityInStock,
            UnitCost = parent.UnitCost,
            ReferenceNumber = disasmRef,
            Reason = $"Kit Disassembly: {dto.Reason ?? "Decomposed to components"}",
            TimestampUtc = DateTime.UtcNow
        };
        await _context.InventoryTransactions.AddAsync(kitTx, cancellationToken);

        var returnedComponents = new List<ComponentDeductionSummaryDto>();

        // Return components to warehouse
        foreach (var bom in bomList)
        {
            var comp = bom.ComponentProduct!;
            var returnQty = dto.Quantity * bom.QuantityRequired;

            var compWhStock = await _context.WarehouseStocks
                .FirstOrDefaultAsync(ws => ws.WarehouseId == dto.WarehouseId && ws.ProductId == comp.Id, cancellationToken);

            if (compWhStock == null)
            {
                compWhStock = new WarehouseStock
                {
                    WarehouseId = dto.WarehouseId,
                    ProductId = comp.Id,
                    QuantityOnHand = returnQty,
                    QuantityReserved = 0,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                await _context.WarehouseStocks.AddAsync(compWhStock, cancellationToken);
            }
            else
            {
                compWhStock.QuantityOnHand += returnQty;
                compWhStock.UpdatedAtUtc = DateTime.UtcNow;
            }

            comp.QuantityInStock += returnQty;

            var compTx = new InventoryTransaction
            {
                ProductId = comp.Id,
                Type = TransactionType.StockIn,
                QuantityChange = returnQty,
                QuantityAfter = comp.QuantityInStock,
                UnitCost = comp.UnitCost,
                ReferenceNumber = disasmRef,
                Reason = $"Restocked from disassembly of {parent.Sku}",
                TimestampUtc = DateTime.UtcNow
            };
            await _context.InventoryTransactions.AddAsync(compTx, cancellationToken);

            returnedComponents.Add(new ComponentDeductionSummaryDto
            {
                ComponentProductId = comp.Id,
                ComponentSku = comp.Sku,
                QuantityDeducted = returnQty,
                RemainingComponentStock = compWhStock.QuantityOnHand
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Disassembled {Qty} units of Kit {KitSku} at WH {WhCode}", dto.Quantity, parent.Sku, warehouse.Code);

        return new DisassembleKitResultDto
        {
            KitProductId = parent.Id,
            KitSku = parent.Sku,
            WarehouseId = dto.WarehouseId,
            QuantityDisassembled = dto.Quantity,
            KitRemainingStock = parentWhStock.QuantityOnHand,
            ComponentsReturned = returnedComponents
        };
    }
}
