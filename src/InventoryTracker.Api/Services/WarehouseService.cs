// src/InventoryTracker.Api/Services/WarehouseService.cs
// Implementation of warehouse facility management, capacity monitoring, and bin location assignments.
// Connects to: src/InventoryTracker.Api/Data/InventoryDbContext.cs, src/InventoryTracker.Api/Models/Warehouse.cs
// Created: 2026-08-27

using InventoryTracker.Api.Data;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service managing warehouse locations, capacity analytics, and stock distribution per facility.
/// </summary>
public class WarehouseService : IWarehouseService
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<WarehouseService> _logger;

    public WarehouseService(InventoryDbContext context, ILogger<WarehouseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<WarehouseDto>> GetWarehousesAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Warehouses
            .AsNoTracking()
            .Include(w => w.StockLevels)
            .AsQueryable();

        if (activeOnly)
        {
            query = query.Where(w => w.IsActive);
        }

        var warehouses = await query.OrderBy(w => w.Code).ToListAsync(cancellationToken);
        return warehouses.Select(MapToDto).ToList();
    }

    public async Task<WarehouseDto?> GetWarehouseByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var warehouse = await _context.Warehouses
            .AsNoTracking()
            .Include(w => w.StockLevels)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        return warehouse == null ? null : MapToDto(warehouse);
    }

    public async Task<WarehouseDto?> GetWarehouseByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var warehouse = await _context.Warehouses
            .AsNoTracking()
            .Include(w => w.StockLevels)
            .FirstOrDefaultAsync(w => w.Code.ToUpper() == normalizedCode, cancellationToken);

        return warehouse == null ? null : MapToDto(warehouse);
    }

    public async Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto dto, CancellationToken cancellationToken = default)
    {
        var normalizedCode = dto.Code.Trim().ToUpperInvariant();

        var codeExists = await _context.Warehouses.AnyAsync(w => w.Code.ToUpper() == normalizedCode, cancellationToken);
        if (codeExists)
        {
            throw new InvalidOperationException($"Warehouse with code '{normalizedCode}' already exists.");
        }

        var warehouse = new Warehouse
        {
            Code = normalizedCode,
            Name = dto.Name.Trim(),
            Address = dto.Address.Trim(),
            City = dto.City.Trim(),
            State = dto.State.Trim(),
            PostalCode = dto.PostalCode.Trim(),
            Country = string.IsNullOrWhiteSpace(dto.Country) ? "USA" : dto.Country.Trim(),
            CapacityUnits = dto.CapacityUnits,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _context.Warehouses.AddAsync(warehouse, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Warehouse registered: {Code} - {Name} (ID: {Id})", warehouse.Code, warehouse.Name, warehouse.Id);
        return MapToDto(warehouse);
    }

    public async Task<WarehouseDto?> UpdateWarehouseAsync(int id, UpdateWarehouseDto dto, CancellationToken cancellationToken = default)
    {
        var warehouse = await _context.Warehouses
            .Include(w => w.StockLevels)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (warehouse == null)
        {
            return null;
        }

        warehouse.Name = dto.Name.Trim();
        warehouse.Address = dto.Address.Trim();
        warehouse.City = dto.City.Trim();
        warehouse.State = dto.State.Trim();
        warehouse.PostalCode = dto.PostalCode.Trim();
        warehouse.Country = string.IsNullOrWhiteSpace(dto.Country) ? "USA" : dto.Country.Trim();
        warehouse.CapacityUnits = dto.CapacityUnits;
        warehouse.IsActive = dto.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Warehouse updated: {Code} (ID: {Id})", warehouse.Code, warehouse.Id);

        return MapToDto(warehouse);
    }

    public async Task<IReadOnlyList<WarehouseStockDto>> GetWarehouseStockAsync(int warehouseId, CancellationToken cancellationToken = default)
    {
        var stockLevels = await _context.WarehouseStocks
            .AsNoTracking()
            .Include(ws => ws.Warehouse)
            .Include(ws => ws.Product)
            .Where(ws => ws.WarehouseId == warehouseId)
            .OrderBy(ws => ws.BinLocation)
            .ThenBy(ws => ws.Product!.Sku)
            .ToListAsync(cancellationToken);

        return stockLevels.Select(MapStockToDto).ToList();
    }

    public async Task<IReadOnlyList<WarehouseStockDto>> GetProductWarehouseStockAsync(int productId, CancellationToken cancellationToken = default)
    {
        var stockLevels = await _context.WarehouseStocks
            .AsNoTracking()
            .Include(ws => ws.Warehouse)
            .Include(ws => ws.Product)
            .Where(ws => ws.ProductId == productId)
            .OrderBy(ws => ws.Warehouse!.Code)
            .ToListAsync(cancellationToken);

        return stockLevels.Select(MapStockToDto).ToList();
    }

    public async Task<WarehouseStockDto?> SetBinLocationAsync(int warehouseId, int productId, string binLocation, CancellationToken cancellationToken = default)
    {
        var stock = await _context.WarehouseStocks
            .Include(ws => ws.Warehouse)
            .Include(ws => ws.Product)
            .FirstOrDefaultAsync(ws => ws.WarehouseId == warehouseId && ws.ProductId == productId, cancellationToken);

        if (stock == null)
        {
            // Create initial stock entry if not exists
            var warehouseExists = await _context.Warehouses.AnyAsync(w => w.Id == warehouseId, cancellationToken);
            var productExists = await _context.Products.AnyAsync(p => p.Id == productId, cancellationToken);

            if (!warehouseExists || !productExists)
            {
                return null;
            }

            stock = new WarehouseStock
            {
                WarehouseId = warehouseId,
                ProductId = productId,
                QuantityOnHand = 0,
                QuantityReserved = 0,
                BinLocation = binLocation.Trim().ToUpperInvariant(),
                UpdatedAtUtc = DateTime.UtcNow
            };
            await _context.WarehouseStocks.AddAsync(stock, cancellationToken);
        }
        else
        {
            stock.BinLocation = binLocation.Trim().ToUpperInvariant();
            stock.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Ensure navigation properties loaded
        await _context.Entry(stock).Reference(s => s.Warehouse).LoadAsync(cancellationToken);
        await _context.Entry(stock).Reference(s => s.Product).LoadAsync(cancellationToken);

        _logger.LogInformation("Bin location updated: Warehouse {WhId}, Product {ProdId} -> {Bin}",
            warehouseId, productId, stock.BinLocation);

        return MapStockToDto(stock);
    }

    private static WarehouseDto MapToDto(Warehouse w) => new()
    {
        Id = w.Id,
        Code = w.Code,
        Name = w.Name,
        Address = w.Address,
        City = w.City,
        State = w.State,
        PostalCode = w.PostalCode,
        Country = w.Country,
        CapacityUnits = w.CapacityUnits,
        TotalUnitsStored = w.StockLevels.Sum(s => s.QuantityOnHand),
        TotalDistinctSkus = w.StockLevels.Count(s => s.QuantityOnHand > 0),
        IsActive = w.IsActive,
        CreatedAtUtc = w.CreatedAtUtc
    };

    private static WarehouseStockDto MapStockToDto(WarehouseStock s) => new()
    {
        Id = s.Id,
        WarehouseId = s.WarehouseId,
        WarehouseCode = s.Warehouse?.Code ?? string.Empty,
        WarehouseName = s.Warehouse?.Name ?? string.Empty,
        ProductId = s.ProductId,
        ProductSku = s.Product?.Sku ?? string.Empty,
        ProductName = s.Product?.Name ?? string.Empty,
        QuantityOnHand = s.QuantityOnHand,
        QuantityReserved = s.QuantityReserved,
        AvailableQuantity = s.AvailableQuantity,
        BinLocation = s.BinLocation,
        UnitCost = s.Product?.UnitCost ?? 0,
        UpdatedAtUtc = s.UpdatedAtUtc
    };
}
