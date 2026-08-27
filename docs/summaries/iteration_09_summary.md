# Iteration 09 Summary: Bill of Materials (BOM) & Kitting / Assembly Decomposition

## Plain English Summary

In this iteration, we expanded the **ASP.NET Core Inventory Tracker API** with a full **Bill of Materials (BOM) & Light Manufacturing Kitting / Assembly Engine**.

The system now enables manufacturing, assembly, and multi-item product bundling:
1. **BOM Component Mapping (`POST /api/v1/bom/components` & `GET /api/v1/bom/product/{id}`)**:
   - Link sub-component raw materials to composite parent kit products (e.g. `KIT-DESK-PRO` requiring 1x 4K Monitor, 1x USB-C Hub, and 1x Desk Mat).
   - Configure quantity multipliers and scrap tolerance percentages.
2. **Cost Roll-Up & Max Assemblable Yield Analytics**:
   - Automatically computes total rolled-up acquisition material costs based on component costs and scrap factors.
   - Evaluates on-hand warehouse inventory across all child components to calculate the `MaxAssemblableKits` yield and isolates the exact bottleneck component (`LimitingComponentSku`).
3. **Atomic Kit Assembly Execution (`POST /api/v1/bom/assemble`)**:
   - Validates that sufficient component stock exists across all child items.
   - Atomically deducts required component quantities from warehouse and product balances, logging `StockOut` audit movements.
   - Computes weighted unit acquisition cost (combining component material costs + direct labor/overhead allocation) and receives finished goods into warehouse inventory with a `StockIn` audit record.
   - Persists a `KitAssemblyOrder` audit log and fires real-time webhooks.
4. **Kit Disassembly Decomposition (`POST /api/v1/bom/disassemble`)**:
   - Reverses assembly operations by deducting parent finished goods and returning sub-components back to active warehouse stock.

The automated test suite was expanded with 4 new tests, bringing the total to 68 unit and integration tests with a 100% pass rate.

---

## File Table & Connections

| File Path | Description | Connects To |
| :--- | :--- | :--- |
| `src/InventoryTracker.Api/Models/BillOfMaterials.cs` | Domain entity defining parent kit to sub-component mappings with scrap percentage. | `Product.cs`, `InventoryDbContext.cs` |
| `src/InventoryTracker.Api/Models/KitAssemblyOrder.cs` | Audit entity logging assembly batch runs, labor costs, rolled-up unit costs, and timestamps. | `Product.cs`, `Warehouse.cs`, `InventoryDbContext.cs` |
| `src/InventoryTracker.Api/Models/Product.cs` | Updated with `IsBundleOrKit`, `BomComponents`, `UsedInBoms`, and `AssemblyOrders`. | `BillOfMaterials.cs`, `KitAssemblyOrder.cs` |
| `src/InventoryTracker.Api/Models/Warehouse.cs` | Updated with `AssemblyOrders` navigation collection. | `KitAssemblyOrder.cs`, `InventoryDbContext.cs` |
| `src/InventoryTracker.Api/DTOs/BomDtos.cs` | DTO contracts for BOM component definitions, cost roll-ups, max yield analytics, and assembly requests. | `BomController.cs`, `BomService.cs` |
| `src/InventoryTracker.Api/Services/IBomService.cs` | Service interface for BOM component management, cost roll-up calculations, and assembly runs. | `BomService.cs`, `BomController.cs` |
| `src/InventoryTracker.Api/Services/BomService.cs` | Implementation executing cost roll-up calculations, bottleneck detection, atomic assembly, and disassembly. | `InventoryDbContext.cs`, `IBomService.cs` |
| `src/InventoryTracker.Api/Controllers/BomController.cs` | REST controller exposing BOM inspection, component management, assembly execution, and disassembly. | `IBomService.cs` |
| `src/InventoryTracker.Api/Data/InventoryDbContext.cs` | Added `DbSet<BillOfMaterials>` and `DbSet<KitAssemblyOrder>` entity configurations. | Entity Models, `Program.cs` |
| `src/InventoryTracker.Api/Data/DbInitializer.cs` | Updated to seed sample composite kit (`KIT-DESK-PRO`) with sub-components. | `InventoryDbContext.cs`, `Program.cs` |
| `src/InventoryTracker.Api/Program.cs` | Registered `IBomService` in dependency injection. | Application Root |
| `tests/InventoryTracker.Tests/Services/BomServiceTests.cs` | Unit tests for cost roll-up calculations, bottleneck identification, and assembly stock deductions. | `BomService.cs` |
| `tests/InventoryTracker.Tests/Controllers/BomControllerTests.cs` | Unit tests for BOM REST action endpoints and assembly run responses. | `BomController.cs` |
| `README.md` | Updated with Bill of Materials and Kit Assembly documentation, status workflows, and curl examples. | Repository root |
| `CHANGELOG.md` | Logged v1.8.0 release notes. | Repository root |
| `BUILD_NOTES.md` | Appended conversational build log for Iteration 9 (ignored in git). | Repository root |

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
   *Expected output*: `Passed: 68, Failed: 0, Total: 68`.

3. Launch the API:
   ```powershell
   dotnet run --project src/InventoryTracker.Api
   ```

4. Test BOM and kit assembly workflows:
   - **Inspect Seeded Kit BOM (Product ID 9 - `KIT-DESK-PRO` at WH-EAST ID 1)**:
     ```bash
     curl -i "http://localhost:5000/api/v1/bom/product/9?warehouseId=1"
     ```
   - **Execute Kit Assembly Run (assemble 2 units with $30 direct labor)**:
     ```bash
     curl -i -X POST http://localhost:5000/api/v1/bom/assemble \
       -H "Content-Type: application/json" \
       -d '{
         "kitProductId": 9,
         "warehouseId": 1,
         "quantity": 2,
         "laborCost": 30.00,
         "assembledBy": "lead_tech",
         "notes": "Finished workstation assembly run"
       }'
     ```
   - **Verify Kit Stock Increased & Components Decreased**:
     ```bash
     curl -i http://localhost:5000/api/v1/products/9
     ```
   - **Disassemble 1 Kit Back to Components**:
     ```bash
     curl -i -X POST http://localhost:5000/api/v1/bom/disassemble \
       -H "Content-Type: application/json" \
       -d '{
         "kitProductId": 9,
         "warehouseId": 1,
         "quantity": 1,
         "disassembledBy": "clerk",
         "reason": "Decomposition back to components"
       }'
     ```
