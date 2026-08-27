// src/InventoryTracker.Api/Services/SupplierService.cs
// Implementation of supplier vendor directory management and query operations.
// Connects to: src/InventoryTracker.Api/Data/InventoryDbContext.cs, src/InventoryTracker.Api/Models/Supplier.cs
// Created: 2026-08-27

using InventoryTracker.Api.Data;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service managing vendor supplier profiles and sourced catalog products.
/// </summary>
public class SupplierService : ISupplierService
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<SupplierService> _logger;

    public SupplierService(InventoryDbContext context, ILogger<SupplierService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Suppliers
            .AsNoTracking()
            .Include(s => s.SourcedProducts)
            .AsQueryable();

        if (activeOnly)
        {
            query = query.Where(s => s.IsActive);
        }

        var suppliers = await query.OrderBy(s => s.Name).ToListAsync(cancellationToken);
        return suppliers.Select(MapToDto).ToList();
    }

    public async Task<SupplierDto?> GetSupplierByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var supplier = await _context.Suppliers
            .AsNoTracking()
            .Include(s => s.SourcedProducts)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return supplier == null ? null : MapToDto(supplier);
    }

    public async Task<SupplierDto?> GetSupplierByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var supplier = await _context.Suppliers
            .AsNoTracking()
            .Include(s => s.SourcedProducts)
            .FirstOrDefaultAsync(s => s.Code.ToUpper() == normalizedCode, cancellationToken);

        return supplier == null ? null : MapToDto(supplier);
    }

    public async Task<SupplierDto> CreateSupplierAsync(CreateSupplierDto dto, CancellationToken cancellationToken = default)
    {
        var normalizedCode = dto.Code.Trim().ToUpperInvariant();

        var exists = await _context.Suppliers.AnyAsync(s => s.Code.ToUpper() == normalizedCode, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Supplier with code '{normalizedCode}' already exists.");
        }

        var supplier = new Supplier
        {
            Code = normalizedCode,
            Name = dto.Name.Trim(),
            ContactName = dto.ContactName?.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            Phone = dto.Phone?.Trim(),
            LeadTimeDays = dto.LeadTimeDays,
            PaymentTerms = string.IsNullOrWhiteSpace(dto.PaymentTerms) ? "Net 30" : dto.PaymentTerms.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _context.Suppliers.AddAsync(supplier, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Supplier registered: {Code} - {Name} (ID: {Id})", supplier.Code, supplier.Name, supplier.Id);
        return MapToDto(supplier);
    }

    public async Task<SupplierDto?> UpdateSupplierAsync(int id, UpdateSupplierDto dto, CancellationToken cancellationToken = default)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.SourcedProducts)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (supplier == null)
        {
            return null;
        }

        supplier.Name = dto.Name.Trim();
        supplier.ContactName = dto.ContactName?.Trim();
        supplier.Email = dto.Email.Trim().ToLowerInvariant();
        supplier.Phone = dto.Phone?.Trim();
        supplier.LeadTimeDays = dto.LeadTimeDays;
        supplier.PaymentTerms = string.IsNullOrWhiteSpace(dto.PaymentTerms) ? "Net 30" : dto.PaymentTerms.Trim();
        supplier.IsActive = dto.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Supplier updated: {Code} (ID: {Id})", supplier.Code, supplier.Id);

        return MapToDto(supplier);
    }

    private static SupplierDto MapToDto(Supplier s) => new()
    {
        Id = s.Id,
        Code = s.Code,
        Name = s.Name,
        ContactName = s.ContactName,
        Email = s.Email,
        Phone = s.Phone,
        LeadTimeDays = s.LeadTimeDays,
        PaymentTerms = s.PaymentTerms,
        TotalProductsSupplied = s.SourcedProducts.Count,
        IsActive = s.IsActive,
        CreatedAtUtc = s.CreatedAtUtc
    };
}
