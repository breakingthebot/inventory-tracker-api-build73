// src/InventoryTracker.Api/Services/ISalesOrderService.cs
// Defines service contracts for customer management and sales order pick-pack-ship fulfillment workflows.
// Connects to: src/InventoryTracker.Api/Services/SalesOrderService.cs, src/InventoryTracker.Api/Controllers/SalesOrdersController.cs
// Created: 2026-08-27

using InventoryTracker.Api.DTOs;
using InventoryTracker.Api.Models;

namespace InventoryTracker.Api.Services;

/// <summary>
/// Service contract for customer accounts and the pick-pack-ship sales order fulfillment pipeline.
/// </summary>
public interface ISalesOrderService
{
    // Customer Management
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetCustomerByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CustomerDto> UpdateCustomerAsync(int id, UpdateCustomerDto dto, CancellationToken cancellationToken = default);

    // Sales Order Operations
    Task<SalesOrderDto> CreateSalesOrderAsync(CreateSalesOrderDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesOrderDto>> GetSalesOrdersAsync(SalesOrderStatus? status = null, int? customerId = null, int? warehouseId = null, CancellationToken cancellationToken = default);
    Task<SalesOrderDto?> GetSalesOrderByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SalesOrderDto> AllocateOrderAsync(int orderId, CancellationToken cancellationToken = default);
    Task<PickListDto> GetPickListAsync(int orderId, CancellationToken cancellationToken = default);
    Task<SalesOrderDto> RecordPickingAsync(int orderId, PickOrderDto dto, CancellationToken cancellationToken = default);
    Task<SalesOrderDto> RecordPackingAsync(int orderId, PackOrderDto dto, CancellationToken cancellationToken = default);
    Task<SalesOrderDto> ShipOrderAsync(int orderId, ShipOrderDto dto, CancellationToken cancellationToken = default);
    Task<SalesOrderDto> CancelOrderAsync(int orderId, string reason, CancellationToken cancellationToken = default);
}
