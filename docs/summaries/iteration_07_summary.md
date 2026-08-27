# Iteration 07 Summary: Expiration Date & Batch / Lot Number Tracking (FEFO / FIFO Dispatching)

## Plain English Summary

In this iteration, we expanded the **ASP.NET Core Inventory Tracker API** with enterprise **Product Lot & Batch Number Tracking**, **Manufacturing / Expiration Date Monitoring**, and an automated **First-Expired, First-Out (FEFO) Dispatching Engine**.

The system now provides comprehensive batch-level traceability for perishable goods, chemicals, pharmaceuticals, and dated electronic assemblies:
1. **Lot Lifecycle & Quarantine Controls**:
   - Track discrete batches (`LOT-2026-NIT01`) with manufacturing dates, expiration dates, initial vs remaining units, and status (`Active`, `Quarantine`, `Expired`, `Depleted`).
   - Quality inspectors can place compromised batches in `Quarantine`, instantly preventing accidental warehouse picking or transfer.
2. **Expiration Risk Analytics (`GET /api/v1/lots/expiring`)**:
   - Scans active warehouse stock for lots nearing expiration (configurable day threshold), aggregating total units and calculating financial dollar valuation at risk (`QuantityOnHand * UnitCost`).
3. **Automated FEFO Allocation Engine (`GET /api/v1/lots/fefo-plan`)**:
   - Recommends optimal picking allocations prioritising earliest-expiring active batches first (with oldest received date as FIFO fallback), preventing stock spoilage.
4. **FEFO Batch Dispatch Execution (`POST /api/v1/lots/dispatch-fefo`)**:
   - Executes multi-lot pick allocations atomically, updates individual lot balances, synchronizes warehouse/product global stock, and logs immutable transaction audit entries.

The automated test suite was expanded with 4 new tests, bringing the total to 60 unit and integration tests with a 100% pass rate.

---

## File Table & Connections

| File Path | Description | Connects To |
| :--- | :--- | :--- |
| `src/InventoryTracker.Api/Models/LotStatus.cs` | Enum defining lot states (`Active`, `Quarantine`, `Expired`, `Depleted`). | `ProductLot.cs`, `LotTrackingService.cs` |
| `src/InventoryTracker.Api/Models/ProductLot.cs` | Domain entity tracking manufacturing/expiration dates and facility lot quantities. | `Product.cs`, `Warehouse.cs`, `InventoryDbContext.cs` |
| `src/InventoryTracker.Api/Models/Product.cs` | Updated with `IsLotTracked` flag and `ProductLots` navigation collection. | `ProductLot.cs`, `InventoryDbContext.cs` |
| `src/InventoryTracker.Api/Models/Warehouse.cs` | Updated with `ProductLots` navigation collection. | `ProductLot.cs`, `InventoryDbContext.cs` |
| `src/InventoryTracker.Api/DTOs/LotDtos.cs` | DTO contracts for lot registration, expiration reports, FEFO plans, and dispatch requests. | `LotsController.cs`, `LotTrackingService.cs` |
| `src/InventoryTracker.Api/Services/ILotTrackingService.cs` | Service interface for batch lot tracking, FEFO calculations, and expiration reports. | `LotTrackingService.cs`, `LotsController.cs` |
| `src/InventoryTracker.Api/Services/LotTrackingService.cs` | Implementation executing FEFO allocation algorithms and lot deductions. | `InventoryDbContext.cs`, `ILotTrackingService.cs` |
| `src/InventoryTracker.Api/Controllers/LotsController.cs` | REST controller exposing lot CRUD, expiration warnings, FEFO planning, and dispatching. | `ILotTrackingService.cs` |
| `src/InventoryTracker.Api/Data/InventoryDbContext.cs` | Added `DbSet<ProductLot>` and unique index constraints. | Entity Models, `Program.cs` |
| `src/InventoryTracker.Api/Data/DbInitializer.cs` | Updated to seed batch lots for nitrile gloves and copy paper. | `InventoryDbContext.cs`, `Program.cs` |
| `src/InventoryTracker.Api/Program.cs` | Registered `ILotTrackingService` in dependency injection. | Application Root |
| `tests/InventoryTracker.Tests/Services/LotTrackingServiceTests.cs` | Unit tests for FEFO algorithm priority and stock balance deductions. | `LotTrackingService.cs` |
| `tests/InventoryTracker.Tests/Controllers/LotsControllerTests.cs` | Unit tests for lot REST endpoints and FEFO plan computation. | `LotsController.cs` |
| `README.md` | Updated with Lot Tracking, Expiration, and FEFO documentation and curl examples. | Repository root |
| `CHANGELOG.md` | Logged v1.6.0 release notes. | Repository root |
| `BUILD_NOTES.md` | Appended conversational build log for Iteration 7 (ignored in git). | Repository root |

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
   *Expected output*: `Passed: 60, Failed: 0, Total: 60`.

3. Launch the API:
   ```powershell
   dotnet run --project src/InventoryTracker.Api
   ```

4. Test lot tracking workflows:
   - **View Expiring Batches within 30 Days**:
     ```bash
     curl -i http://localhost:5000/api/v1/lots/expiring?daysThreshold=30
     ```
   - **Calculate FEFO Picking Plan for Nitrile Gloves (Product ID 8, 10 units at WH-EAST ID 1)**:
     ```bash
     curl -i "http://localhost:5000/api/v1/lots/fefo-plan?productId=8&quantity=10&warehouseId=1"
     ```
   - **Execute FEFO Batch Dispatch**:
     ```bash
     curl -i -X POST http://localhost:5000/api/v1/lots/dispatch-fefo \
       -H "Content-Type: application/json" \
       -d '{
         "productId": 8,
         "warehouseId": 1,
         "quantity": 10,
         "referenceNumber": "SO-2026-FEFO-01",
         "reason": "Expedited Hospital Order"
       }'
     ```
   - **Check Remaining Lot Balances**:
     ```bash
     curl -i http://localhost:5000/api/v1/lots?productId=8
     ```
