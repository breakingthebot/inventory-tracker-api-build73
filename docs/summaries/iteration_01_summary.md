# Iteration 01 Summary: ASP.NET Core Inventory Tracker API Foundation

## Plain English Summary

In this initial iteration, we established the complete foundation for the **ASP.NET Core Inventory Tracker API** (.NET 8.0, C#, Entity Framework Core). 

The service provides a robust backend for tracking physical products, monitoring stock quantities, executing warehouse stock movements (such as restocks, dispatches, and count adjustments), logging an immutable transaction audit history, and calculating real-time financial valuation summaries across categories. 

The API includes an automated in-memory database seeder with realistic sample inventory, comprehensive validation rules (preventing negative inventory balances and duplicate SKUs), standardized JSON response envelopes, global exception handling, performance request logging, interactive Swagger/OpenAPI documentation, and a suite of 16 passing xUnit tests.

---

## File Table & Connections

| File Path | Description | Connects To |
| :--- | :--- | :--- |
| `src/InventoryTracker.Api/Models/TransactionType.cs` | Enum defining stock movement types (`InitialStock`, `StockIn`, `StockOut`, `Adjustment`, `Return`, `WriteOff`). | `InventoryTransaction.cs` |
| `src/InventoryTracker.Api/Models/Category.cs` | Entity model representing product categories. | `Product.cs`, `InventoryDbContext.cs` |
| `src/InventoryTracker.Api/Models/Product.cs` | Entity model representing tracked inventory items, SKUs, pricing, and reorder levels. | `Category.cs`, `InventoryTransaction.cs`, `InventoryDbContext.cs` |
| `src/InventoryTracker.Api/Models/InventoryTransaction.cs` | Entity model recording immutable transaction logs for stock movements. | `Product.cs`, `TransactionType.cs`, `InventoryDbContext.cs` |
| `src/InventoryTracker.Api/DTOs/ApiResponse.cs` | Generic JSON response envelope wrapper for API consistency. | All Controllers, `GlobalExceptionMiddleware.cs` |
| `src/InventoryTracker.Api/DTOs/PagedResult.cs` | Generic container for paginated query results and pagination metadata. | `ApiResponse.cs`, `IProductService.cs`, `IInventoryService.cs` |
| `src/InventoryTracker.Api/DTOs/ProductDtos.cs` | DTO contracts for product listings, creation, updates, and filtering. | `ProductsController.cs`, `ProductService.cs` |
| `src/InventoryTracker.Api/DTOs/InventoryDtos.cs` | DTO contracts for restock, dispatch, adjustments, and transaction queries. | `InventoryController.cs`, `InventoryService.cs` |
| `src/InventoryTracker.Api/DTOs/AnalyticsDtos.cs` | DTO contracts for financial inventory valuation and system health probes. | `AnalyticsController.cs`, `HealthController.cs`, `AnalyticsService.cs` |
| `src/InventoryTracker.Api/Data/InventoryDbContext.cs` | EF Core database context configuring entity mappings, relationships, and decimal precision. | Entity Models, `Program.cs`, `DbInitializer.cs` |
| `src/InventoryTracker.Api/Data/DbInitializer.cs` | Seeder populating categories, product records, and opening balance transactions. | `InventoryDbContext.cs`, `Program.cs` |
| `src/InventoryTracker.Api/Services/IProductService.cs` | Service interface contract for catalog management and SKU queries. | `ProductService.cs`, `ProductsController.cs` |
| `src/InventoryTracker.Api/Services/ProductService.cs` | Implementation of product business logic, filtering, and SKU uniqueness validation. | `InventoryDbContext.cs`, `IProductService.cs` |
| `src/InventoryTracker.Api/Services/IInventoryService.cs` | Service interface contract for stock movement operations and audit queries. | `InventoryService.cs`, `InventoryController.cs` |
| `src/InventoryTracker.Api/Services/InventoryService.cs` | Implementation of restock, dispatch, and adjustment logic with balance guards. | `InventoryDbContext.cs`, `IInventoryService.cs` |
| `src/InventoryTracker.Api/Services/IAnalyticsService.cs` | Service interface contract for valuation rollups and health metrics. | `AnalyticsService.cs`, `AnalyticsController.cs` |
| `src/InventoryTracker.Api/Services/AnalyticsService.cs` | Implementation calculating inventory financial totals, gross margins, and DB status. | `InventoryDbContext.cs`, `IAnalyticsService.cs` |
| `src/InventoryTracker.Api/Middleware/GlobalExceptionMiddleware.cs` | Global error handling middleware formatting unified JSON error responses. | HTTP pipeline, `ApiResponse.cs` |
| `src/InventoryTracker.Api/Middleware/RequestLoggingMiddleware.cs` | HTTP request duration timing and structured logging middleware. | HTTP pipeline |
| `src/InventoryTracker.Api/Controllers/ProductsController.cs` | REST controller exposing product CRUD, SKU lookups, and low-stock alerts. | `IProductService.cs` |
| `src/InventoryTracker.Api/Controllers/InventoryController.cs` | REST controller exposing restock, dispatch, adjustment, and transaction logs. | `IInventoryService.cs` |
| `src/InventoryTracker.Api/Controllers/AnalyticsController.cs` | REST controller exposing financial valuation summaries and category rollups. | `IAnalyticsService.cs` |
| `src/InventoryTracker.Api/Controllers/HealthController.cs` | REST controller exposing system health check and uptime diagnostic probes. | `IAnalyticsService.cs` |
| `src/InventoryTracker.Api/Program.cs` | Application entry point configuring DI, Swagger, middleware, and auto-seeding. | All Services, Controllers, and Middleware |
| `tests/InventoryTracker.Tests/Services/ProductServiceTests.cs` | Unit tests for catalog operations, SKU uniqueness, and search filtering. | `ProductService.cs` |
| `tests/InventoryTracker.Tests/Services/InventoryServiceTests.cs` | Unit tests for restock, dispatch, adjustments, and negative stock prevention. | `InventoryService.cs` |
| `tests/InventoryTracker.Tests/Services/AnalyticsServiceTests.cs` | Unit tests for financial valuation arithmetic and health checks. | `AnalyticsService.cs` |
| `tests/InventoryTracker.Tests/Controllers/ProductsControllerTests.cs` | Unit tests for ProductsController action results and HTTP response codes. | `ProductsController.cs` |
| `tests/InventoryTracker.Tests/Controllers/InventoryControllerTests.cs` | Unit tests for InventoryController restock and dispatch actions. | `InventoryController.cs` |
| `tests/InventoryTracker.Tests/Controllers/HealthControllerTests.cs` | Unit tests for HealthController diagnostic responses. | `HealthController.cs` |
| `.github/workflows/ci.yml` | GitHub Actions CI workflow building and running tests on every push/PR. | GitHub repository |
| `LICENSE` | Standard MIT License. | Repository root |
| `README.md` | Comprehensive project documentation, setup, architecture, and curl examples. | Repository root |
| `CHANGELOG.md` | Technical release changelog adhering to Keep a Changelog standards. | Repository root |
| `BUILD_NOTES.md` | Plain-English conversational build notes log (ignored in git). | Repository root |

---

## Exact Steps to Test Manually

1. Open a terminal in `Build_73/`:
   ```bash
   cd C:\Users\marve\Desktop\AI-286-Builds\Build_73
   ```
2. Run the test suite:
   ```bash
   dotnet test
   ```
   *Expected result*: All 16 tests pass with 0 failures.

3. Launch the API service:
   ```bash
   dotnet run --project src/InventoryTracker.Api
   ```
   *Expected output*: Service starts on `http://localhost:5000` (or dynamic port) with Swagger UI available at `/`.

4. In another terminal window, verify the endpoints:
   - **Health Check**:
     ```bash
     curl -i http://localhost:5000/health
     ```
   - **List Products**:
     ```bash
     curl -i http://localhost:5000/api/v1/products
     ```
   - **Query Low-Stock Items**:
     ```bash
     curl -i http://localhost:5000/api/v1/products/low-stock
     ```
   - **Perform a Restock**:
     ```bash
     curl -i -X POST http://localhost:5000/api/v1/inventory/restock \
       -H "Content-Type: application/json" \
       -d '{"productId": 3, "quantity": 50, "unitCost": 28.00, "purchaseOrderNumber": "PO-991", "notes": "Restocked USB hub"}'
     ```
   - **Fulfill a Dispatch Order**:
     ```bash
     curl -i -X POST http://localhost:5000/api/v1/inventory/dispatch \
       -H "Content-Type: application/json" \
       -d '{"productId": 3, "quantity": 10, "salesOrderNumber": "SO-401", "notes": "Customer shipment"}'
     ```
   - **Inspect Financial Valuation Summary**:
     ```bash
     curl -i http://localhost:5000/api/v1/inventory/summary
     ```

---

## Suggested Candidate Next Iterations

### Option 1: Multi-Warehouse Location Support & Inter-Warehouse Stock Transfers
- **Plain English**: Add warehouse location entities (`Warehouse`, `BinLocation`) and support transferring stock between different warehouses with in-transit status tracking.
- **Benefit**: Essential for enterprise supply chain operations where inventory is distributed across regional fulfillment centers.
- **Trade-off**: Increases schema complexity and requires multi-location stock balance tables.
- **Interview Answer**: "We introduced multi-location inventory partitioning with transactional transfer validation, allowing the enterprise to manage distributed warehouse stock while preventing double-allocation during transit."

### Option 2: Automated Purchase Order (PO) Generation & Low-Stock Auto-Reorder Engine
- **Plain English**: Automatically generate draft Purchase Orders when stock reaches the reorder threshold, calculating recommended reorder quantities based on historical turnover rate.
- **Benefit**: Streamlines procurement workflows and eliminates stockouts caused by human oversight.
- **Trade-off**: Requires supplier entity management and configurable reorder policies.
- **Interview Answer**: "We built an automated replenishment service that monitors inventory velocity and triggers draft POs with dynamic economic order quantity calculations."

### Option 3: Barcode & QR Code Generation / Scanning Endpoint with CSV Bulk Import/Export
- **Plain English**: Provide endpoints to generate Code 128 / QR codes for SKUs, scan barcodes for quick lookup, and bulk import/export product catalog spreadsheets.
- **Benefit**: Enables mobile warehouse handheld barcode scanning integration and bulk catalog onboarding.
- **Trade-off**: Requires external image rendering or barcode generation libraries.
- **Interview Answer**: "We implemented streaming CSV bulk import with row-by-row validation alongside vector SVG barcode generation for warehouse label printing."
