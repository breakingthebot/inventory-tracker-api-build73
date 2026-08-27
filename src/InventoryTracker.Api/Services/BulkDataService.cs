// src/InventoryTracker.Api/Services/BulkDataService.cs
// Implementation of high-throughput CSV catalog parsing, row-level validation, batch upserting, and export streaming.
// Connects to: src/InventoryTracker.Api/Data/InventoryDbContext.cs, src/InventoryTracker.Api/DTOs/BulkDtos.cs
// Created: 2026-08-27

using System.Globalization;
using System.Text;
using InventoryTracker.Api.Data;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service providing streaming CSV product catalog import, validation error reporting, and export generation.
/// </summary>
public class BulkDataService : IBulkDataService
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<BulkDataService> _logger;

    public BulkDataService(InventoryDbContext context, ILogger<BulkDataService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BulkImportResultDto> ImportProductsFromCsvAsync(string csvContent, CancellationToken cancellationToken = default)
    {
        var result = new BulkImportResultDto();

        if (string.IsNullOrWhiteSpace(csvContent))
        {
            result.Errors.Add(new BulkImportRowErrorDto
            {
                RowNumber = 0,
                ErrorMessage = "CSV payload is empty."
            });
            result.RowsFailed = 1;
            return result;
        }

        var lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (lines.Count <= 1)
        {
            result.Errors.Add(new BulkImportRowErrorDto
            {
                RowNumber = 0,
                ErrorMessage = "CSV contains no data rows (only header or empty)."
            });
            result.RowsFailed = 1;
            return result;
        }

        var headerTokens = ParseCsvLine(lines[0]);
        var headerMap = BuildHeaderMap(headerTokens);

        // Preload categories and suppliers
        var categories = await _context.Categories.ToDictionaryAsync(c => c.Name.ToLowerInvariant(), c => c.Id, cancellationToken);
        var suppliers = await _context.Suppliers.ToDictionaryAsync(s => s.Code.ToUpperInvariant(), s => s.Id, cancellationToken);

        for (int i = 1; i < lines.Count; i++)
        {
            result.TotalRowsRead++;
            var rawLine = lines[i];
            var rowTokens = ParseCsvLine(rawLine);

            try
            {
                var sku = GetColumnValue(rowTokens, headerMap, "sku")?.Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(sku))
                {
                    throw new FormatException("SKU is required and cannot be empty.");
                }

                var name = GetColumnValue(rowTokens, headerMap, "name")?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new FormatException("Product Name is required.");
                }

                var categoryName = GetColumnValue(rowTokens, headerMap, "category")?.Trim();
                if (string.IsNullOrWhiteSpace(categoryName))
                {
                    categoryName = "General";
                }

                // Resolve or create category
                int categoryId;
                var catKey = categoryName.ToLowerInvariant();
                if (!categories.TryGetValue(catKey, out categoryId))
                {
                    var newCat = new Category { Name = categoryName, Description = "Auto-created via bulk import" };
                    await _context.Categories.AddAsync(newCat, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                    categoryId = newCat.Id;
                    categories[catKey] = categoryId;
                }

                // Resolve optional supplier
                int? primarySupplierId = null;
                var supCode = GetColumnValue(rowTokens, headerMap, "primarysuppliercode")?.Trim().ToUpperInvariant();
                if (!string.IsNullOrWhiteSpace(supCode) && suppliers.TryGetValue(supCode, out var sId))
                {
                    primarySupplierId = sId;
                }

                var description = GetColumnValue(rowTokens, headerMap, "description")?.Trim();
                var unitPrice = decimal.Parse(GetColumnValue(rowTokens, headerMap, "unitprice") ?? "0.00", CultureInfo.InvariantCulture);
                var unitCost = decimal.Parse(GetColumnValue(rowTokens, headerMap, "unitcost") ?? "0.00", CultureInfo.InvariantCulture);
                var qtyInStock = int.Parse(GetColumnValue(rowTokens, headerMap, "quantityinstock") ?? "0", CultureInfo.InvariantCulture);
                var reorderThreshold = int.Parse(GetColumnValue(rowTokens, headerMap, "reorderthreshold") ?? "10", CultureInfo.InvariantCulture);
                var reorderQty = int.Parse(GetColumnValue(rowTokens, headerMap, "reorderquantity") ?? "50", CultureInfo.InvariantCulture);
                var uom = GetColumnValue(rowTokens, headerMap, "unitofmeasure")?.Trim() ?? "pcs";

                if (unitPrice < 0 || unitCost < 0 || qtyInStock < 0)
                {
                    throw new ArgumentException("UnitPrice, UnitCost, and QuantityInStock must be non-negative.");
                }

                var existingProduct = await _context.Products.FirstOrDefaultAsync(p => p.Sku.ToUpper() == sku, cancellationToken);
                if (existingProduct != null)
                {
                    existingProduct.Name = name;
                    existingProduct.Description = description;
                    existingProduct.CategoryId = categoryId;
                    existingProduct.PrimarySupplierId = primarySupplierId ?? existingProduct.PrimarySupplierId;
                    existingProduct.UnitPrice = unitPrice;
                    existingProduct.UnitCost = unitCost;
                    existingProduct.ReorderThreshold = reorderThreshold;
                    existingProduct.ReorderQuantity = reorderQty;
                    existingProduct.UnitOfMeasure = uom;
                    existingProduct.UpdatedAtUtc = DateTime.UtcNow;
                    result.RowsUpdated++;
                }
                else
                {
                    var newProduct = new Product
                    {
                        Sku = sku,
                        Name = name,
                        Description = description,
                        CategoryId = categoryId,
                        PrimarySupplierId = primarySupplierId,
                        UnitPrice = unitPrice,
                        UnitCost = unitCost,
                        QuantityInStock = qtyInStock,
                        ReorderThreshold = reorderThreshold,
                        ReorderQuantity = reorderQty,
                        UnitOfMeasure = uom,
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    await _context.Products.AddAsync(newProduct, cancellationToken);
                    result.RowsInserted++;
                }
            }
            catch (Exception ex)
            {
                result.RowsFailed++;
                result.Errors.Add(new BulkImportRowErrorDto
                {
                    RowNumber = i + 1,
                    Sku = GetColumnValue(rowTokens, headerMap, "sku") ?? "UNKNOWN",
                    ErrorMessage = ex.Message,
                    RawRowData = rawLine
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Bulk CSV import completed: {Inserted} inserted, {Updated} updated, {Failed} failed.",
            result.RowsInserted, result.RowsUpdated, result.RowsFailed);

        return result;
    }

    public async Task<string> ExportProductsToCsvAsync(CancellationToken cancellationToken = default)
    {
        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.PrimarySupplier)
            .OrderBy(p => p.Sku)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Sku,Name,Category,Description,UnitPrice,UnitCost,QuantityInStock,ReorderThreshold,ReorderQuantity,UnitOfMeasure,PrimarySupplierCode");

        foreach (var p in products)
        {
            var cleanSku = EscapeCsv(p.Sku);
            var cleanName = EscapeCsv(p.Name);
            var cleanCat = EscapeCsv(p.Category?.Name ?? "General");
            var cleanDesc = EscapeCsv(p.Description ?? string.Empty);
            var supplierCode = EscapeCsv(p.PrimarySupplier?.Code ?? string.Empty);

            sb.AppendLine($"{cleanSku},{cleanName},{cleanCat},{cleanDesc},{p.UnitPrice.ToString("F2", CultureInfo.InvariantCulture)},{p.UnitCost.ToString("F2", CultureInfo.InvariantCulture)},{p.QuantityInStock},{p.ReorderThreshold},{p.ReorderQuantity},{EscapeCsv(p.UnitOfMeasure)},{supplierCode}");
        }

        return sb.ToString();
    }

    public string GetProductCsvTemplate()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Sku,Name,Category,Description,UnitPrice,UnitCost,QuantityInStock,ReorderThreshold,ReorderQuantity,UnitOfMeasure,PrimarySupplierCode");
        sb.AppendLine("ELEC-SAMPLE-01,Wireless Ergonomic Vertical Mouse,Electronics,Ergonomic 2.4G optical mouse with silent clicks,49.99,22.50,100,20,50,pcs,SUP-TECH-CORP");
        sb.AppendLine("OFF-SAMPLE-02,Thermal Shipping Labels 4x6 (Roll of 500),Packaging & Shipping,Direct thermal blank shipping labels,18.50,7.20,250,50,200,roll,SUP-OFFICE-DIR");
        return sb.ToString();
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    sb.Append('\"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString().Trim());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        result.Add(sb.ToString().Trim());
        return result;
    }

    private static Dictionary<string, int> BuildHeaderMap(List<string> headers)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Count; i++)
        {
            var header = headers[i].Replace(" ", "").Replace("_", "").ToLowerInvariant();
            map[header] = i;
        }
        return map;
    }

    private static string? GetColumnValue(List<string> tokens, Dictionary<string, int> headerMap, string columnName)
    {
        if (headerMap.TryGetValue(columnName, out var index) && index < tokens.Count)
        {
            return tokens[index];
        }
        return null;
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Contains(',') || value.Contains('\"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
