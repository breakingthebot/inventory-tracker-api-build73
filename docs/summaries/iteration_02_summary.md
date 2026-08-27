# Iteration 02 Summary: Multi-Warehouse Location Support & Inter-Warehouse Stock Transfers

## Plain English Summary

In this iteration, we expanded the **ASP.NET Core Inventory Tracker API** with enterprise **Multi-Warehouse Location Partitioning** and a complete **Inter-Warehouse Stock Transfer Workflow**.

The service now allows organizations to manage physical warehouse locations (e.g. `WH-EAST` Atlanta, `WH-WEST` Reno, `WH-CENTRAL` Dallas), track on-hand, reserved, and available stock per facility, assign precise warehouse aisle/rack/shelf bin coordinates (e.g. `A-01-01`), and execute multi-stage inter-warehouse transfer orders.

The transfer engine enforces a strict state machine (`Draft` -> `Pending` -> `InTransit` -> `Received` / `Cancelled`):
1. **Creation**: Validates stock availability, generates unique transfer tracking numbers, and reserves inventory at the source facility.
2. **Shipment**: Deducts physical on-hand stock from the source facility, releases the reservation, records an immutable `StockOut` audit transaction, and marks the shipment `InTransit`.
3. **Receiving**: Verifies intake at the destination facility, increments on-hand stock at the destination, records an immutable `StockIn` audit transaction, and marks the transfer `Received`.
4. **Cancellation**: Safely releases reserved source inventory if cancelled prior to dispatch.

The entire test suite was expanded with 11 new tests, bringing total coverage to 27 unit and integration tests with a 100% pass rate.

---

## File Table & Connections

| File Path | Description | Connects To |
| :--- | :--- | :--- |
| `src/InventoryTracker.Api/Models/Warehouse.cs` | Domain entity representing a physical warehouse facility and capacity limits. | `WarehouseStock.cs`, `StockTransfer.cs`, `InventoryDbContext.cs` |
| `src/InventoryTracker.Api/Models/WarehouseStock.cs` | Domain entity storing product on-hand, reserved, and bin locations per facility. | `Warehouse.cs`, `Product.cs`, `InventoryDbContext.cs` |
| `src/InventoryTracker.Api/Models/StockTransferStatus.cs` | Enum defining transfer order lifecycle states. | `StockTransfer.cs` |
| `src/InventoryTracker.Api/Models/StockTransfer.cs` | Domain entity representing transfer order headers, tracking numbers, and timestamps. | `Warehouse.cs`, `StockTransferItem.cs`, `InventoryDbContext.cs` |
| `src/InventoryTracker.Api/Models/StockTransferItem.cs` | Domain entity representing product line items and transfer quantities. | `StockTransfer.cs`, `Product.cs`, `InventoryDbContext.cs` |
| `src/InventoryTracker.Api/DTOs/WarehouseDtos.cs` | DTO contracts for warehouse registration, capacity utilization, and bin coordinates. | `WarehousesController.cs`, `WarehouseService.cs` |
| `src/InventoryTracker.Api/DTOs/TransferDtos.cs` | DTO contracts for initiating, shipping, receiving, and filtering stock transfers. | `TransfersController.cs`, `TransferService.cs` |
| `src/InventoryTracker.Api/Services/IWarehouseService.cs` | Service interface for warehouse facilities and facility stock balances. | `WarehouseService.cs`, `WarehousesController.cs` |
| `src/InventoryTracker.Api/Services/WarehouseService.cs` | Implementation managing facility CRUD, capacity rollups, and bin location updates. | `InventoryDbContext.cs`, `IWarehouseService.cs` |
| `src/InventoryTracker.Api/Services/ITransferService.cs` | Service interface for inter-warehouse stock transfer orchestration. | `TransferService.cs`, `TransfersController.cs` |
| `src/InventoryTracker.Api/Services/TransferService.cs` | Implementation of multi-stage transfer order state transitions and stock synchronization. | `InventoryDbContext.cs`, `ITransferService.cs` |
| `src/InventoryTracker.Api/Controllers/WarehousesController.cs` | REST controller exposing facility CRUD, warehouse stock listings, and bin updates. | `IWarehouseService.cs` |
| `src/InventoryTracker.Api/Controllers/TransfersController.cs` | REST controller exposing transfer creation, shipment dispatch, and receiving endpoints. | `ITransferService.cs` |
| `src/InventoryTracker.Api/Data/InventoryDbContext.cs` | Updated with `Warehouses`, `WarehouseStocks`, `StockTransfers`, and `StockTransferItems`. | Entity Models, `Program.cs` |
| `src/InventoryTracker.Api/Data/DbInitializer.cs` | Updated to seed 3 regional facilities, distributed inventory, and sample transfers. | `InventoryDbContext.cs`, `Program.cs` |
| `src/InventoryTracker.Api/Program.cs` | Registered `IWarehouseService` and `ITransferService` into dependency injection container. | Application Root |
| `tests/InventoryTracker.Tests/Services/WarehouseServiceTests.cs` | Unit tests for facility registration and bin coordinate updates. | `WarehouseService.cs` |
| `tests/InventoryTracker.Tests/Services/TransferServiceTests.cs` | Unit tests for transfer creation, stock reservation, shipping, receiving, and cancellation. | `TransferService.cs` |
| `tests/InventoryTracker.Tests/Controllers/TransfersControllerTests.cs` | Unit tests for transfer HTTP action results. | `TransfersController.cs` |
| `README.md` | Updated with warehouse and transfer API documentation, architecture notes, and curl commands. | Repository root |
| `CHANGELOG.md` | Logged v1.1.0 release notes. | Repository root |
| `BUILD_NOTES.md` | Appended conversational build log for Iteration 2 (ignored in git). | Repository root |

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
   *Expected output*: `Passed: 27, Failed: 0, Total: 27`.

3. Launch the API service:
   ```powershell
   dotnet run --project src/InventoryTracker.Api
   ```

4. Test endpoints:
   - **List Warehouses & Storage Utilization**:
     ```bash
     curl -i http://localhost:5000/api/v1/warehouses
     ```
   - **Inspect Stock & Bin Locations in Atlanta Hub (`WH-EAST`, ID 1)**:
     ```bash
     curl -i http://localhost:5000/api/v1/warehouses/1/stock
     ```
   - **Initiate Stock Transfer from Atlanta (ID 1) to Dallas (ID 3)**:
     ```bash
     curl -i -X POST http://localhost:5000/api/v1/transfers \
       -H "Content-Type: application/json" \
       -d '{
         "sourceWarehouseId": 1,
         "destinationWarehouseId": 3,
         "requestedBy": "dispatch_lead",
         "notes": "Transfer 5 monitors to Dallas",
         "items": [{"productId": 1, "quantity": 5}]
       }'
     ```
