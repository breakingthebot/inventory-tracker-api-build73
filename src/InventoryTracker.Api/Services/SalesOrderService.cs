// src/InventoryTracker.Api/Services/SalesOrderService.cs
// Implementation of customer account management and the multi-stage pick-pack-ship sales fulfillment pipeline.
// Connects to: src/InventoryTracker.Api/Data/InventoryDbContext.cs, src/InventoryTracker.Api/Models/SalesOrder.cs
// Created: 2026-08-27

using InventoryTracker.Api.Data;
using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service executing customer sales order workflows: drafting, stock allocation/reservation, pick sheets, packing, and carrier dispatching.
/// </summary>
public class SalesOrderService : ISalesOrderService
{
    private readonly InventoryDbContext _context;
    private readonly IWebhookService _webhookService;
    private readonly ILogger<SalesOrderService> _logger;

    public SalesOrderService(InventoryDbContext context, IWebhookService webhookService, ILogger<SalesOrderService> logger)
    {
        _context = context;
        _webhookService = webhookService;
        _logger = logger;
    }

    #region Customer Management

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto dto, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Customers
            .FirstOrDefaultAsync(c => c.CustomerCode.ToLower() == dto.CustomerCode.Trim().ToLower(), cancellationToken);

        if (existing != null)
        {
            throw new InvalidOperationException($"A customer with code '{dto.CustomerCode}' already exists.");
        }

        var customer = new Customer
        {
            CustomerCode = dto.CustomerCode.Trim().ToUpperInvariant(),
            CompanyName = dto.CompanyName.Trim(),
            ContactName = dto.ContactName.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            Phone = dto.Phone.Trim(),
            ShippingAddress = dto.ShippingAddress.Trim(),
            ShippingCity = dto.ShippingCity.Trim(),
            ShippingState = dto.ShippingState.Trim(),
            ShippingPostalCode = dto.ShippingPostalCode.Trim(),
            ShippingCountry = string.IsNullOrWhiteSpace(dto.ShippingCountry) ? "USA" : dto.ShippingCountry.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _context.Customers.AddAsync(customer, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created Customer {CustomerCode} ({CompanyName})", customer.CustomerCode, customer.CompanyName);
        return MapCustomerToDto(customer);
    }

    public async Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        var list = await _context.Customers
            .Include(c => c.Orders)
            .OrderBy(c => c.CompanyName)
            .ToListAsync(cancellationToken);

        return list.Select(MapCustomerToDto).ToList();
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return customer == null ? null : MapCustomerToDto(customer);
    }

    public async Task<CustomerDto> UpdateCustomerAsync(int id, UpdateCustomerDto dto, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers.FindAsync(new object[] { id }, cancellationToken);
        if (customer == null)
        {
            throw new KeyNotFoundException($"Customer with ID {id} was not found.");
        }

        customer.CompanyName = dto.CompanyName.Trim();
        customer.ContactName = dto.ContactName.Trim();
        customer.Email = dto.Email.Trim().ToLowerInvariant();
        customer.Phone = dto.Phone.Trim();
        customer.ShippingAddress = dto.ShippingAddress.Trim();
        customer.ShippingCity = dto.ShippingCity.Trim();
        customer.ShippingState = dto.ShippingState.Trim();
        customer.ShippingPostalCode = dto.ShippingPostalCode.Trim();
        customer.ShippingCountry = dto.ShippingCountry.Trim();
        customer.IsActive = dto.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated Customer {CustomerCode}", customer.CustomerCode);

        return MapCustomerToDto(customer);
    }

    #endregion

    #region Sales Order Pipeline

    public async Task<SalesOrderDto> CreateSalesOrderAsync(CreateSalesOrderDto dto, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers.FindAsync(new object[] { dto.CustomerId }, cancellationToken);
        if (customer == null)
        {
            throw new KeyNotFoundException($"Customer with ID {dto.CustomerId} was not found.");
        }

        var warehouse = await _context.Warehouses.FindAsync(new object[] { dto.WarehouseId }, cancellationToken);
        if (warehouse == null)
        {
            throw new KeyNotFoundException($"Warehouse with ID {dto.WarehouseId} was not found.");
        }

        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var orderSequence = await _context.SalesOrders.CountAsync(so => so.OrderDateUtc.Date == DateTime.UtcNow.Date, cancellationToken) + 1;
        var orderNumber = $"SO-{today}-{orderSequence:D4}";

        var salesOrder = new SalesOrder
        {
            OrderNumber = orderNumber,
            CustomerId = dto.CustomerId,
            WarehouseId = dto.WarehouseId,
            Status = SalesOrderStatus.Draft,
            ShippingFee = dto.ShippingFee,
            TaxAmount = dto.TaxAmount,
            Notes = dto.Notes?.Trim(),
            OrderDateUtc = DateTime.UtcNow
        };

        decimal subtotal = 0m;

        foreach (var itemDto in dto.Items)
        {
            var product = await _context.Products.FindAsync(new object[] { itemDto.ProductId }, cancellationToken);
            if (product == null)
            {
                throw new KeyNotFoundException($"Product with ID {itemDto.ProductId} was not found.");
            }

            var whStock = await _context.WarehouseStocks
                .FirstOrDefaultAsync(ws => ws.WarehouseId == dto.WarehouseId && ws.ProductId == product.Id, cancellationToken);

            var unitPrice = itemDto.UnitPrice ?? product.UnitPrice;
            var lineTotal = itemDto.QuantityOrdered * unitPrice;
            subtotal += lineTotal;

            salesOrder.Items.Add(new SalesOrderItem
            {
                ProductId = product.Id,
                QuantityOrdered = itemDto.QuantityOrdered,
                QuantityPicked = 0,
                UnitPrice = unitPrice,
                UnitCostSnapshot = product.UnitCost,
                BinLocationSnapshot = whStock?.BinLocation ?? "A-01"
            });
        }

        salesOrder.Subtotal = Math.Round(subtotal, 2);
        salesOrder.TotalAmount = Math.Round(salesOrder.Subtotal + salesOrder.ShippingFee + salesOrder.TaxAmount, 2);

        await _context.SalesOrders.AddAsync(salesOrder, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created Sales Order {OrderNumber} for Customer {CustomerCode} (Total: ${Total})",
            salesOrder.OrderNumber, customer.CustomerCode, salesOrder.TotalAmount);

        return (await GetSalesOrderByIdAsync(salesOrder.Id, cancellationToken))!;
    }

    public async Task<IReadOnlyList<SalesOrderDto>> GetSalesOrdersAsync(SalesOrderStatus? status = null, int? customerId = null, int? warehouseId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SalesOrders
            .Include(so => so.Customer)
            .Include(so => so.Warehouse)
            .Include(so => so.Items)
            .ThenInclude(i => i.Product)
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(so => so.Status == status.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(so => so.CustomerId == customerId.Value);
        }

        if (warehouseId.HasValue)
        {
            query = query.Where(so => so.WarehouseId == warehouseId.Value);
        }

        var list = await query.OrderByDescending(so => so.OrderDateUtc).ToListAsync(cancellationToken);
        return list.Select(MapOrderToDto).ToList();
    }

    public async Task<SalesOrderDto?> GetSalesOrderByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await _context.SalesOrders
            .Include(so => so.Customer)
            .Include(so => so.Warehouse)
            .Include(so => so.Items)
            .ThenInclude(i => i.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(so => so.Id == id, cancellationToken);

        return order == null ? null : MapOrderToDto(order);
    }

    public async Task<SalesOrderDto> AllocateOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _context.SalesOrders
            .Include(so => so.Warehouse)
            .Include(so => so.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(so => so.Id == orderId, cancellationToken);

        if (order == null)
        {
            throw new KeyNotFoundException($"Sales order with ID {orderId} was not found.");
        }

        if (order.Status != SalesOrderStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot allocate sales order in status '{order.Status}'. Must be in 'Draft'.");
        }

        // Validate stock availability and reserve
        foreach (var item in order.Items)
        {
            var whStock = await _context.WarehouseStocks
                .FirstOrDefaultAsync(ws => ws.WarehouseId == order.WarehouseId && ws.ProductId == item.ProductId, cancellationToken);

            var available = whStock?.AvailableQuantity ?? 0;
            if (available < item.QuantityOrdered)
            {
                throw new InvalidOperationException(
                    $"Insufficient uncommitted stock for product '{item.Product?.Sku}'. Needed: {item.QuantityOrdered}, Available: {available} at {order.Warehouse?.Code}.");
            }
        }

        foreach (var item in order.Items)
        {
            var whStock = await _context.WarehouseStocks
                .FirstAsync(ws => ws.WarehouseId == order.WarehouseId && ws.ProductId == item.ProductId, cancellationToken);

            whStock.QuantityReserved += item.QuantityOrdered;
            whStock.UpdatedAtUtc = DateTime.UtcNow;
        }

        order.Status = SalesOrderStatus.Allocated;
        order.AllocatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Allocated/Reserved stock for Sales Order {OrderNumber}", order.OrderNumber);

        return (await GetSalesOrderByIdAsync(order.Id, cancellationToken))!;
    }

    public async Task<PickListDto> GetPickListAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _context.SalesOrders
            .Include(so => so.Warehouse)
            .Include(so => so.Items)
            .ThenInclude(i => i.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(so => so.Id == orderId, cancellationToken);

        if (order == null)
        {
            throw new KeyNotFoundException($"Sales order with ID {orderId} was not found.");
        }

        var lines = order.Items
            .OrderBy(i => i.BinLocationSnapshot)
            .Select(i => new PickListItemDto
            {
                ItemId = i.Id,
                ProductId = i.ProductId,
                ProductSku = i.Product?.Sku ?? string.Empty,
                ProductName = i.Product?.Name ?? string.Empty,
                BinLocation = i.BinLocationSnapshot ?? "A-01",
                QuantityToPick = i.QuantityOrdered,
                QuantityPicked = i.QuantityPicked
            })
            .ToList();

        return new PickListDto
        {
            SalesOrderId = order.Id,
            OrderNumber = order.OrderNumber,
            WarehouseCode = order.Warehouse?.Code ?? string.Empty,
            OrderDateUtc = order.OrderDateUtc,
            TotalItemsToPick = lines.Sum(l => l.QuantityToPick),
            Lines = lines
        };
    }

    public async Task<SalesOrderDto> RecordPickingAsync(int orderId, PickOrderDto dto, CancellationToken cancellationToken = default)
    {
        var order = await _context.SalesOrders
            .Include(so => so.Items)
            .FirstOrDefaultAsync(so => so.Id == orderId, cancellationToken);

        if (order == null)
        {
            throw new KeyNotFoundException($"Sales order with ID {orderId} was not found.");
        }

        if (order.Status != SalesOrderStatus.Allocated && order.Status != SalesOrderStatus.Picked)
        {
            throw new InvalidOperationException($"Cannot record picking for order in status '{order.Status}'. Order must be 'Allocated'.");
        }

        foreach (var pick in dto.PickedItems)
        {
            var item = order.Items.FirstOrDefault(i => i.Id == pick.ItemId);
            if (item != null)
            {
                item.QuantityPicked = pick.QuantityPicked;
            }
        }

        var allPicked = order.Items.All(i => i.QuantityPicked >= i.QuantityOrdered);
        if (allPicked)
        {
            order.Status = SalesOrderStatus.Picked;
            order.PickedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated picking for Sales Order {OrderNumber} (Status: {Status})", order.OrderNumber, order.Status);

        return (await GetSalesOrderByIdAsync(order.Id, cancellationToken))!;
    }

    public async Task<SalesOrderDto> RecordPackingAsync(int orderId, PackOrderDto dto, CancellationToken cancellationToken = default)
    {
        var order = await _context.SalesOrders
            .Include(so => so.Items)
            .FirstOrDefaultAsync(so => so.Id == orderId, cancellationToken);

        if (order == null)
        {
            throw new KeyNotFoundException($"Sales order with ID {orderId} was not found.");
        }

        if (order.Status != SalesOrderStatus.Picked)
        {
            throw new InvalidOperationException($"Cannot pack order in status '{order.Status}'. Order must be 'Picked'.");
        }

        order.ShippingCarrier = dto.ShippingCarrier.Trim();
        order.Status = SalesOrderStatus.Packed;
        order.PackedAtUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(dto.Notes))
        {
            order.Notes = string.IsNullOrWhiteSpace(order.Notes)
                ? $"Pack Notes: {dto.Notes.Trim()}"
                : $"{order.Notes}; Pack Notes: {dto.Notes.Trim()}";
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Packed Sales Order {OrderNumber} via {Carrier}", order.OrderNumber, order.ShippingCarrier);

        return (await GetSalesOrderByIdAsync(order.Id, cancellationToken))!;
    }

    public async Task<SalesOrderDto> ShipOrderAsync(int orderId, ShipOrderDto dto, CancellationToken cancellationToken = default)
    {
        var order = await _context.SalesOrders
            .Include(so => so.Customer)
            .Include(so => so.Warehouse)
            .Include(so => so.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(so => so.Id == orderId, cancellationToken);

        if (order == null)
        {
            throw new KeyNotFoundException($"Sales order with ID {orderId} was not found.");
        }

        if (order.Status != SalesOrderStatus.Packed)
        {
            throw new InvalidOperationException($"Cannot ship order in status '{order.Status}'. Order must be 'Packed'.");
        }

        // Deduct physical inventory & release reserved stock
        foreach (var item in order.Items)
        {
            var whStock = await _context.WarehouseStocks
                .FirstOrDefaultAsync(ws => ws.WarehouseId == order.WarehouseId && ws.ProductId == item.ProductId, cancellationToken);

            if (whStock != null)
            {
                whStock.QuantityReserved -= item.QuantityOrdered;
                whStock.QuantityOnHand -= item.QuantityOrdered;
                whStock.UpdatedAtUtc = DateTime.UtcNow;
            }

            var product = await _context.Products.FindAsync(new object[] { item.ProductId }, cancellationToken);
            if (product != null)
            {
                product.QuantityInStock -= item.QuantityOrdered;
            }

            var transaction = new InventoryTransaction
            {
                ProductId = item.ProductId,
                Type = TransactionType.StockOut,
                QuantityChange = -item.QuantityOrdered,
                QuantityAfter = product?.QuantityInStock ?? 0,
                UnitCost = item.UnitCostSnapshot,
                ReferenceNumber = $"SO-{order.OrderNumber}",
                Reason = $"Customer shipment: {order.Customer?.CompanyName ?? "Customer"} (Tracking: {dto.TrackingNumber})",
                TimestampUtc = DateTime.UtcNow
            };
            await _context.InventoryTransactions.AddAsync(transaction, cancellationToken);
        }

        order.TrackingNumber = dto.TrackingNumber.Trim();
        if (!string.IsNullOrWhiteSpace(dto.ShippingCarrier))
        {
            order.ShippingCarrier = dto.ShippingCarrier.Trim();
        }

        order.Status = SalesOrderStatus.Shipped;
        order.ShippedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _webhookService.PublishEventAsync(WebhookEventType.StockOut, new
        {
            OrderNumber = order.OrderNumber,
            CustomerCode = order.Customer?.CustomerCode,
            TrackingNumber = order.TrackingNumber,
            Carrier = order.ShippingCarrier,
            TotalAmount = order.TotalAmount
        }, cancellationToken);

        _logger.LogInformation("Shipped Sales Order {OrderNumber} (Tracking: {Tracking})", order.OrderNumber, order.TrackingNumber);

        return (await GetSalesOrderByIdAsync(order.Id, cancellationToken))!;
    }

    public async Task<SalesOrderDto> CancelOrderAsync(int orderId, string reason, CancellationToken cancellationToken = default)
    {
        var order = await _context.SalesOrders
            .Include(so => so.Items)
            .FirstOrDefaultAsync(so => so.Id == orderId, cancellationToken);

        if (order == null)
        {
            throw new KeyNotFoundException($"Sales order with ID {orderId} was not found.");
        }

        if (order.Status == SalesOrderStatus.Shipped || order.Status == SalesOrderStatus.Delivered)
        {
            throw new InvalidOperationException($"Cannot cancel sales order in status '{order.Status}'.");
        }

        // Release reserved inventory if order was allocated, picked, or packed
        if (order.Status == SalesOrderStatus.Allocated || order.Status == SalesOrderStatus.Picked || order.Status == SalesOrderStatus.Packed)
        {
            foreach (var item in order.Items)
            {
                var whStock = await _context.WarehouseStocks
                    .FirstOrDefaultAsync(ws => ws.WarehouseId == order.WarehouseId && ws.ProductId == item.ProductId, cancellationToken);

                if (whStock != null)
                {
                    whStock.QuantityReserved = Math.Max(0, whStock.QuantityReserved - item.QuantityOrdered);
                    whStock.UpdatedAtUtc = DateTime.UtcNow;
                }
            }
        }

        order.Status = SalesOrderStatus.Cancelled;
        order.Notes = string.IsNullOrWhiteSpace(order.Notes)
            ? $"Cancelled: {reason}"
            : $"{order.Notes}; Cancelled: {reason}";

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Cancelled Sales Order {OrderNumber} (Reason: {Reason})", order.OrderNumber, reason);

        return (await GetSalesOrderByIdAsync(order.Id, cancellationToken))!;
    }

    #endregion

    #region Mappings

    private static CustomerDto MapCustomerToDto(Customer c) => new()
    {
        Id = c.Id,
        CustomerCode = c.CustomerCode,
        CompanyName = c.CompanyName,
        ContactName = c.ContactName,
        Email = c.Email,
        Phone = c.Phone,
        ShippingAddress = c.ShippingAddress,
        ShippingCity = c.ShippingCity,
        ShippingState = c.ShippingState,
        ShippingPostalCode = c.ShippingPostalCode,
        ShippingCountry = c.ShippingCountry,
        IsActive = c.IsActive,
        CreatedAtUtc = c.CreatedAtUtc,
        TotalOrdersPlaced = c.Orders?.Count ?? 0
    };

    private static SalesOrderDto MapOrderToDto(SalesOrder so) => new()
    {
        Id = so.Id,
        OrderNumber = so.OrderNumber,
        CustomerId = so.CustomerId,
        CustomerCode = so.Customer?.CustomerCode ?? string.Empty,
        CustomerName = so.Customer?.CompanyName ?? string.Empty,
        WarehouseId = so.WarehouseId,
        WarehouseCode = so.Warehouse?.Code ?? string.Empty,
        WarehouseName = so.Warehouse?.Name ?? string.Empty,
        Status = so.Status,
        Subtotal = so.Subtotal,
        ShippingFee = so.ShippingFee,
        TaxAmount = so.TaxAmount,
        TotalAmount = so.TotalAmount,
        ShippingCarrier = so.ShippingCarrier,
        TrackingNumber = so.TrackingNumber,
        OrderDateUtc = so.OrderDateUtc,
        AllocatedAtUtc = so.AllocatedAtUtc,
        PickedAtUtc = so.PickedAtUtc,
        PackedAtUtc = so.PackedAtUtc,
        ShippedAtUtc = so.ShippedAtUtc,
        Notes = so.Notes,
        Items = so.Items.Select(MapItemToDto).ToList()
    };

    private static SalesOrderItemDto MapItemToDto(SalesOrderItem i) => new()
    {
        Id = i.Id,
        SalesOrderId = i.SalesOrderId,
        ProductId = i.ProductId,
        ProductSku = i.Product?.Sku ?? string.Empty,
        ProductName = i.Product?.Name ?? string.Empty,
        QuantityOrdered = i.QuantityOrdered,
        QuantityPicked = i.QuantityPicked,
        UnitPrice = i.UnitPrice,
        TotalPrice = i.TotalPrice,
        UnitCostSnapshot = i.UnitCostSnapshot,
        BinLocationSnapshot = i.BinLocationSnapshot
    };

    #endregion
}
