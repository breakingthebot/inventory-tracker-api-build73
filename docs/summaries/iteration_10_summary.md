# Iteration 10 Summary: Customer Sales Order Processing & Pick-Pack-Ship Fulfillment Pipeline

## Plain English Summary

In this iteration, we expanded the **ASP.NET Core Inventory Tracker API** with a comprehensive **Customer Account & Sales Order Fulfillment Engine (Pick-Pack-Ship Pipeline)**.

The system now provides an end-to-end sales order management and warehouse execution lifecycle:
1. **Customer Purchasing Profiles (`/api/v1/customers`)**:
   - Manage business customer accounts (`CUST-1001`), primary contacts, phone, and destination shipping addresses.
2. **Order Drafting & Financial Totals (`POST /api/v1/sales-orders`)**:
   - Create multi-item sales orders with automatic unit price lookup, subtotal aggregation, shipping fee, and tax calculations.
3. **Inventory Allocation & Reservation (`POST /api/v1/sales-orders/{id}/allocate`)**:
   - Validates uncommitted available stock (`QuantityOnHand - QuantityReserved >= Ordered`) across all line items and commits stock (`QuantityReserved += Ordered`) at the fulfillment facility.
4. **Bin-Routed Warehouse Pick Sheets (`GET /api/v1/sales-orders/{id}/pick-list`)**:
   - Generates optimized pick lists ordered by physical aisle/rack/shelf bin coordinates (`A-01-01`) for floor runner efficiency.
5. **Physical Picking & Packing Verification (`/pick` and `/pack`)**:
   - Warehouse clerks confirm picked unit counts and package dimensions with assigned shipping carriers (FedEx, UPS, DHL).
6. **Carrier Dispatch & Inventory Deduction (`POST /api/v1/sales-orders/{id}/ship`)**:
   - Transitions order to `Shipped`, assigns tracking numbers, atomically deducts physical on-hand stock (`QuantityOnHand -= Ordered`), clears reserved quantities (`QuantityReserved -= Ordered`), logs immutable `StockOut` audit entries, and dispatches outbound webhooks.
7. **Order Cancellation & Stock Release (`POST /api/v1/sales-orders/{id}/cancel`)**:
   - Releases committed reserved inventory back to available stock if an order is cancelled prior to shipment.

The automated test suite was expanded with 4 new tests, bringing the total to 72 unit and integration tests with a 100% pass rate.

---

## File Table & Connections

| File Path | Description | Connects To |
| :--- | :--- | :--- |
| `src/InventoryTracker.Api/Models/Customer.cs` | Domain entity representing customer accounts and shipping addresses. | `SalesOrder.cs`, `InventoryDbContext.cs` |
| `src/InventoryTracker.Api/Models/SalesOrderStatus.cs` | Lifecycle workflow enum (`Draft`, `Allocated`, `Picked`, `Packed`, `Shipped`, `Delivered`, `Cancelled`). | `SalesOrder.cs`, `SalesOrderService.cs` |
| `src/InventoryTracker.Api/Models/SalesOrder.cs` | Domain entity storing order header, totals, warehouse origin, carrier tracking, and stage timestamps. | `Customer.cs`, `Warehouse.cs`, `SalesOrderItem.cs` |
| `src/InventoryTracker.Api/Models/SalesOrderItem.cs` | Domain entity storing ordered vs picked quantities, unit prices, cost snapshots, and bin coordinates. | `SalesOrder.cs`, `Product.cs`, `InventoryDbContext.cs` |
| `src/InventoryTracker.Api/Models/Warehouse.cs` | Updated with `SalesOrders` navigation collection. | `SalesOrder.cs`, `InventoryDbContext.cs` |
| `src/InventoryTracker.Api/DTOs/SalesOrderDtos.cs` | DTO contracts for customer profiles, order drafting, stock allocation, pick lists, packing, and shipment. | `SalesOrdersController.cs`, `CustomersController.cs` |
| `src/InventoryTracker.Api/Services/ISalesOrderService.cs` | Service interface for customer management and sales order fulfillment pipeline. | `SalesOrderService.cs`, `SalesOrdersController.cs` |
| `src/InventoryTracker.Api/Services/SalesOrderService.cs` | Implementation executing order drafting, stock reservation, bin routing, packing, and carrier dispatching. | `InventoryDbContext.cs`, `ISalesOrderService.cs` |
| `src/InventoryTracker.Api/Controllers/SalesOrdersController.cs` | REST controller exposing order drafting, allocation, pick sheets, packing, and shipment dispatching. | `ISalesOrderService.cs` |
| `src/InventoryTracker.Api/Controllers/CustomersController.cs` | REST controller exposing customer account registration and profile updates. | `ISalesOrderService.cs` |
| `src/InventoryTracker.Api/Data/InventoryDbContext.cs` | Added `DbSet<Customer>`, `DbSet<SalesOrder>`, and `DbSet<SalesOrderItem>` entity configurations. | Entity Models, `Program.cs` |
| `src/InventoryTracker.Api/Data/DbInitializer.cs` | Updated to seed customer accounts (`CUST-1001`, `CUST-1002`) and sample shipped order (`SO-20260826-0001`). | `InventoryDbContext.cs`, `Program.cs` |
| `src/InventoryTracker.Api/Program.cs` | Registered `ISalesOrderService` in dependency injection. | Application Root |
| `tests/InventoryTracker.Tests/Services/SalesOrderServiceTests.cs` | Unit tests for stock reservation, pick sheet generation, and shipment inventory deductions. | `SalesOrderService.cs` |
| `tests/InventoryTracker.Tests/Controllers/SalesOrdersControllerTests.cs` | Unit tests for sales order REST action endpoints and allocation responses. | `SalesOrdersController.cs` |
| `README.md` | Updated with Sales Orders and Customer Fulfillment documentation, status workflows, and curl examples. | Repository root |
| `CHANGELOG.md` | Logged v1.9.0 release notes. | Repository root |
| `BUILD_NOTES.md` | Appended conversational build log for Iteration 10 (ignored in git). | Repository root |

---

## Exact Steps to Test Manually

1. Open a terminal in `Build_73/`:
   ```powershell
   cd C:\Users\marve\Desktop\AI-286-Builds\Build_73
   ```
2. Run the test suite:
   ```powershell
   dotnet test
   ```
   *Expected output*: `Passed: 72, Failed: 0, Total: 72`.

3. Launch the API:
   ```powershell
   dotnet run --project src/InventoryTracker.Api
   ```

4. Test customer sales order workflows:
   - **View Pre-Seeded Customers**:
     ```bash
     curl -i http://localhost:5000/api/v1/customers
     ```
   - **View Pre-Seeded Shipped Order**:
     ```bash
     curl -i http://localhost:5000/api/v1/sales-orders/1
     ```
   - **Draft New Sales Order at WH-EAST (ID 1)**:
     ```bash
     curl -i -X POST http://localhost:5000/api/v1/sales-orders \
       -H "Content-Type: application/json" \
       -d '{
         "customerId": 2,
         "warehouseId": 1,
         "shippingFee": 15.00,
         "taxAmount": 28.00,
         "notes": "Urgent customer order",
         "items": [
           { "productId": 1, "quantityOrdered": 2 }
         ]
       }'
     ```
   - **Allocate/Reserve Inventory for Order 2**:
     ```bash
     curl -i -X POST http://localhost:5000/api/v1/sales-orders/2/allocate
     ```
   - **Get Pick List**:
     ```bash
     curl -i http://localhost:5000/api/v1/sales-orders/2/pick-list
     ```
   - **Record Picking & Packing**:
     ```bash
     curl -i -X POST http://localhost:5000/api/v1/sales-orders/2/pick \
       -H "Content-Type: application/json" \
       -d '{ "pickedItems": [{ "itemId": 2, "quantityPicked": 2 }] }'

     curl -i -X POST http://localhost:5000/api/v1/sales-orders/2/pack \
       -H "Content-Type: application/json" \
       -d '{ "shippingCarrier": "UPS Next Day Air" }'
     ```
   - **Ship Order**:
     ```bash
     curl -i -X POST http://localhost:5000/api/v1/sales-orders/2/ship \
       -H "Content-Type: application/json" \
       -d '{ "trackingNumber": "1Z9999999999999999", "shippingCarrier": "UPS Next Day Air" }'
     ```
