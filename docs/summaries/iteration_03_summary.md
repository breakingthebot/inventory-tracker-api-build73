# Iteration 03 Summary: Automated Purchase Order Generation & Low-Stock Auto-Reorder Engine

## Plain English Summary

In this iteration, we expanded the **ASP.NET Core Inventory Tracker API** with an enterprise **Automated Replenishment & Purchase Order Management Engine**.

The system now tracks verified vendor suppliers with lead times and payment terms, monitors on-hand stock across all facilities against configured `ReorderThreshold` limits, and generates draft Purchase Orders automatically grouped by primary vendor.

Key workflows implemented:
1. **Low-Stock Recommendation Engine**: Scans active catalog products and identifies critical out-of-stock and low-stock deficits with recommended reorder quantities (`/api/v1/purchase-orders/suggestions`).
2. **Batch Purchase Order Auto-Generation**: Automatically groups low-stock products by primary vendor and creates draft purchase orders with projected delivery dates based on vendor lead times (`POST /api/v1/purchase-orders/auto-generate`).
3. **Purchase Order Lifecycle**: Transitions orders through `Draft` -> `Submitted` -> `PartiallyReceived` / `Fulfilled` / `Cancelled`.
4. **Goods Receiving Intake**: Verifies received quantities against open line items, increments on-hand warehouse stock and global catalog stock, recalculates weighted average unit acquisition costs on the product, and logs immutable `StockIn` transaction audits.

The automated test suite was expanded with 7 new tests, bringing the total to 34 unit and integration tests with a 100% pass rate.

---

## File Table & Connections

| File Path | Description | Connects To |
| :--- | :--- | :--- |
| `src/InventoryTracker.Api/Models/Supplier.cs` | Domain entity representing a vendor supplier with lead times and payment terms. | `Product.cs`, `PurchaseOrder.cs`, `InventoryDbContext.cs` |
| `src/InventoryTracker.Api/Models/PurchaseOrderStatus.cs` | Enum defining purchase order lifecycle states. | `PurchaseOrder.cs` |
| `src/InventoryTracker.Api/Models/PurchaseOrder.cs` | Domain entity representing PO header, financial commitments, and delivery projections. | `Supplier.cs`, `PurchaseOrderItem.cs`, `InventoryDbContext.cs` |
| `src/InventoryTracker.Api/Models/PurchaseOrderItem.cs` | Domain entity tracking ordered vs received quantities and line costs. | `PurchaseOrder.cs`, `Product.cs`, `InventoryDbContext.cs` |
| `src/InventoryTracker.Api/Models/Product.cs` | Updated with `PrimarySupplierId` and supplier navigation reference. | `Supplier.cs`, `InventoryDbContext.cs` |
| `src/InventoryTracker.Api/DTOs/SupplierDtos.cs` | DTO contracts for vendor supplier registration and profile updates. | `SuppliersController.cs`, `SupplierService.cs` |
| `src/InventoryTracker.Api/DTOs/PurchaseOrderDtos.cs` | DTO contracts for PO creation, auto-reorder recommendations, and receiving intake. | `PurchaseOrdersController.cs`, `PurchaseOrderService.cs` |
| `src/InventoryTracker.Api/Services/ISupplierService.cs` | Service interface for supplier vendor directory management. | `SupplierService.cs`, `SuppliersController.cs` |
| `src/InventoryTracker.Api/Services/SupplierService.cs` | Implementation managing supplier profiles and catalog links. | `InventoryDbContext.cs`, `ISupplierService.cs` |
| `src/InventoryTracker.Api/Services/IPurchaseOrderService.cs` | Service interface for replenishment analysis, PO workflows, and receiving. | `PurchaseOrderService.cs`, `PurchaseOrdersController.cs` |
| `src/InventoryTracker.Api/Services/PurchaseOrderService.cs` | Implementation of auto-reorder engine, batch PO generation, and goods intake. | `InventoryDbContext.cs`, `IPurchaseOrderService.cs` |
| `src/InventoryTracker.Api/Controllers/SuppliersController.cs` | REST controller exposing supplier CRUD endpoints. | `ISupplierService.cs` |
| `src/InventoryTracker.Api/Controllers/PurchaseOrdersController.cs` | REST controller exposing PO suggestions, batch auto-generation, and receiving intake. | `IPurchaseOrderService.cs` |
| `src/InventoryTracker.Api/Data/InventoryDbContext.cs` | Updated with `Suppliers`, `PurchaseOrders`, and `PurchaseOrderItems` mappings. | Entity Models, `Program.cs` |
| `src/InventoryTracker.Api/Data/DbInitializer.cs` | Updated to seed 3 suppliers and assign primary vendors to catalog items. | `InventoryDbContext.cs`, `Program.cs` |
| `src/InventoryTracker.Api/Program.cs` | Registered `ISupplierService` and `IPurchaseOrderService` in dependency injection. | Application Root |
| `tests/InventoryTracker.Tests/Services/SupplierServiceTests.cs` | Unit tests for supplier registration and duplicate code validation. | `SupplierService.cs` |
| `tests/InventoryTracker.Tests/Services/PurchaseOrderServiceTests.cs` | Unit tests for auto-reorder analysis, batch PO creation, and intake receiving. | `PurchaseOrderService.cs` |
| `tests/InventoryTracker.Tests/Controllers/PurchaseOrdersControllerTests.cs` | Unit tests for purchase order HTTP action results. | `PurchaseOrdersController.cs` |
| `README.md` | Updated with Supplier and Purchase Order documentation and curl examples. | Repository root |
| `CHANGELOG.md` | Logged v1.2.0 release notes. | Repository root |
| `BUILD_NOTES.md` | Appended conversational build log for Iteration 3 (ignored in git). | Repository root |

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
   *Expected output*: `Passed: 34, Failed: 0, Total: 34`.

3. Launch the API:
   ```powershell
   dotnet run --project src/InventoryTracker.Api
   ```

4. Test the procurement endpoints:
   - **Get Low-Stock Recommendations**:
     ```bash
     curl -i http://localhost:5000/api/v1/purchase-orders/suggestions
     ```
   - **Auto-Generate Draft Replenishment POs**:
     ```bash
     curl -i -X POST "http://localhost:5000/api/v1/purchase-orders/auto-generate?defaultDestinationWarehouseId=1"
     ```
   - **Submit Draft PO (assume ID 2)**:
     ```bash
     curl -i -X POST http://localhost:5000/api/v1/purchase-orders/2/submit
     ```
   - **Receive Shipment Intake**:
     ```bash
     curl -i -X POST http://localhost:5000/api/v1/purchase-orders/2/receive \
       -H "Content-Type: application/json" \
       -d '{
         "receivedItems": [{"purchaseOrderItemId": 1, "quantityReceived": 50, "actualUnitCost": 28.00}],
         "notes": "Intake verified at dock"
       }'
     ```
