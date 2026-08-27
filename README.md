# Inventory Tracker API (Build 73)

A production-grade RESTful Inventory Tracker service built with ASP.NET Core 8.0, Entity Framework Core, SQL Server / In-Memory persistence, multi-warehouse location partitioning, inter-warehouse stock transfers, automated replenishment purchase orders, vector SVG barcode/QR generation, mobile handheld scanner lookup, CSV bulk catalog import/export, outbound webhooks with HMAC-SHA256 signatures, batch lot & expiration tracking with FEFO dispatching, blind cycle counting & reconciliation audits, Bill of Materials (BOM) & kit assembly/disassembly, Role-Based Access Control (RBAC), JWT authentication, transaction auditing, and real-time business valuation analytics.

## Stack

- **Language / Runtime**: C# 12 / .NET 8.0 SDK
- **Framework**: ASP.NET Core Web API
- **Security / Authentication**: JWT Bearer Tokens, HMAC-SHA256 PBKDF2 Password Hashing, RBAC
- **Integration**: Outbound Webhooks with HMAC-SHA256 signature headers (`X-Inventory-Signature-256`)
- **Inventory Allocation**: First-Expired, First-Out (FEFO) & First-In, First-Out (FIFO) Lot Tracking
- **Manufacturing & Kitting**: Bill of Materials (BOM) Trees, Cost Roll-Up Engine, Assembly & Disassembly
- **Audit & Compliance**: Cycle Counting with Blind Count Entry & Supervisor Reconciliation Ledger Adjustments
- **ORM / Persistence**: Entity Framework Core 8.0 (SQL Server & In-Memory providers)
- **API Documentation**: OpenAPI 3.0 / Swagger UI (`Swashbuckle.AspNetCore`) with Bearer Security Scheme
- **Testing**: xUnit, Moq, Microsoft.EntityFrameworkCore.InMemory
- **CI/CD**: GitHub Actions (.NET 8 Build & Test pipeline)
- **Architecture Pattern**: Clean Layered Architecture (Controllers -> DTOs -> Domain Services -> EF Core DbContext)

## Setup

1. **Prerequisites**: Ensure .NET 8.0 SDK is installed on your machine.
   ```bash
   dotnet --version
   ```
2. **Clone the repository**:
   ```bash
   git clone https://github.com/breakingthebot/inventory-tracker-api-build73.git
   cd inventory-tracker-api-build73
   ```
3. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

## Environment Variables

See `.env.example` for environment variable templates.

| Variable | Description | Default |
| :--- | :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment name | `Development` |
| `UseInMemoryDatabase` | Uses in-memory EF Core database for zero-config local run | `true` |
| `ConnectionStrings__DefaultConnection` | SQL Server connection string | `Server=localhost;Database=InventoryTrackerDb;...` |
| `Jwt__SecretKey` | HMAC secret key for signing JWT tokens | `InventoryTrackerApiSecretKey_Production_SuperSecret_2026_Key!` |
| `Jwt__Issuer` | Token issuer identifier | `InventoryTrackerApi` |
| `Jwt__Audience` | Token audience identifier | `InventoryTrackerClients` |
| `Logging__LogLevel__Default` | Application logging threshold | `Information` |

## Default Seed User Accounts

| Username | Password | Role | Permissions |
| :--- | :--- | :--- | :--- |
| `admin` | `AdminPass123!` | `Admin` | Full administrative control, user account provisioning, system config |
| `manager` | `ManagerPass123!` | `WarehouseManager` | PO auto-generation, inter-warehouse transfers, supplier management |
| `clerk` | `ClerkPass123!` | `Clerk` | Stock dispatching, PO intake receiving, barcode scanning |
| `auditor` | `AuditorPass123!` | `Auditor` | Financial valuation analytics, audit transaction logs, cycle counts |

## Running Locally

To run the API locally:

```bash
dotnet run --project src/InventoryTracker.Api
```

Once running, navigate to:
- **Interactive Swagger UI**: `http://localhost:5000` (or `https://localhost:5001`)
- **OpenAPI JSON Spec**: `http://localhost:5000/swagger/v1/swagger.json`
- **Health Check**: `http://localhost:5000/health`

## API Endpoints Reference

### 1. Authentication & Users (`/api/v1/auth`)

| Method | Endpoint | Access | Description |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/auth/login` | Anonymous | Authenticates credentials and issues signed JWT Bearer token |
| `POST` | `/api/v1/auth/register` | Admin | Creates new system operator account |
| `GET` | `/api/v1/auth/me` | Authenticated | Retrieves current authenticated profile from token claims |
| `GET` | `/api/v1/auth/users` | Admin | Lists all system operator accounts |

### 2. Product Catalog (`/api/v1/products`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/products` | Paginated product listing with keyword search, category, and stock filters |
| `GET` | `/api/v1/products/{id}` | Retrieve product details by integer primary key |
| `GET` | `/api/v1/products/sku/{sku}` | Retrieve product by unique SKU code |
| `GET` | `/api/v1/products/low-stock` | List all items with on-hand stock at or below reorder threshold |
| `POST` | `/api/v1/products` | Create a new catalog product (enforces unique SKU) |
| `PUT` | `/api/v1/products/{id}` | Update product details, prices, and replenishment rules |
| `DELETE` | `/api/v1/products/{id}` | Delete product (fails if on-hand stock is positive) |

### 3. Warehouses & Facility Stock (`/api/v1/warehouses`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/warehouses` | List all physical facilities with storage capacity & utilization metrics |
| `GET` | `/api/v1/warehouses/{id}` | Retrieve facility details by database ID |
| `GET` | `/api/v1/warehouses/code/{code}` | Retrieve facility by code (e.g. `WH-EAST`) |
| `POST` | `/api/v1/warehouses` | Register a new warehouse facility |
| `PUT` | `/api/v1/warehouses/{id}` | Update facility metadata and storage capacity limits |
| `GET` | `/api/v1/warehouses/{id}/stock` | List product on-hand, reserved, and available stock lines per facility |
| `PUT` | `/api/v1/warehouses/{id}/stock/{productId}/bin` | Assign or update physical aisle/rack/shelf bin coordinates |

### 4. Bill of Materials (BOM) & Kitting (`/api/v1/bom`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/bom/product/{productId}` | Full BOM component tree, cost roll-ups, max yield analytics, and bottleneck SKU |
| `POST` | `/api/v1/bom/components` | Attach or update component requirement in parent kit recipe |
| `DELETE` | `/api/v1/bom/components` | Remove sub-component requirement from kit recipe |
| `POST` | `/api/v1/bom/assemble` | Execute kit assembly run, deduct components, receive finished goods, and update cost |
| `POST` | `/api/v1/bom/disassemble` | Disassemble finished kits back into raw sub-component inventory |

### 5. Cycle Counting & Audit Reconciliation (`/api/v1/cycle-counts`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/cycle-counts` | List audit sessions with status and warehouse filters |
| `GET` | `/api/v1/cycle-counts/{id}` | Retrieve audit session with line items and count progress |
| `POST` | `/api/v1/cycle-counts` | Initiate audit session snapshotting current on-hand stock per facility |
| `POST` | `/api/v1/cycle-counts/{id}/record-counts` | Batch record blind physical counts by warehouse clerks |
| `POST` | `/api/v1/cycle-counts/{id}/submit-review` | Submit completed counts for supervisor review |
| `GET` | `/api/v1/cycle-counts/{id}/variance-report` | Detailed variance report comparing counted vs system stock with accuracy rate % |
| `POST` | `/api/v1/cycle-counts/{id}/reconcile` | Approve discrepancies, post balancing ledger adjustments, and update stock |
| `POST` | `/api/v1/cycle-counts/{id}/cancel` | Void open audit session without adjusting inventory |

### 6. Product Lots & Expiration Tracking (`/api/v1/lots`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/lots` | Paginated list of lots filtered by product, warehouse, status, or expiration |
| `GET` | `/api/v1/lots/{id}` | Retrieve specific batch lot details with warehouse metadata |
| `POST` | `/api/v1/lots` | Register a new lot batch and increment warehouse stock |
| `PUT` | `/api/v1/lots/{id}` | Update lot operational status (Quarantine/Active) or expiration date |
| `GET` | `/api/v1/lots/expiring` | Expiration risk report calculating units and valuation at risk within day window |
| `GET` | `/api/v1/lots/fefo-plan` | Compute FEFO picking recommendation allocating from earliest-expiring active lots |
| `POST` | `/api/v1/lots/dispatch-fefo` | Execute automated FEFO batch deduction, update lot balances, and log audits |

### 7. Inter-Warehouse Stock Transfers (`/api/v1/transfers`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/transfers` | Paginated list of transfers with status and facility filters |
| `GET` | `/api/v1/transfers/{id}` | Retrieve transfer order details, line items, and tracking numbers |
| `POST` | `/api/v1/transfers` | Initiate stock transfer and reserve inventory at source facility (`Pending`) |
| `POST` | `/api/v1/transfers/{id}/ship` | Ship transfer, deduct source inventory, and set status to `InTransit` |
| `POST` | `/api/v1/transfers/{id}/receive` | Confirm receipt, add destination inventory, and set status to `Received` |
| `POST` | `/api/v1/transfers/{id}/cancel` | Cancel order before shipment and release reserved inventory |

### 8. Barcodes & Handheld Scanner Resolution (`/api/v1/barcodes`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/barcodes/sku/{sku}` | Generates Code 128 linear vector SVG barcode with label metadata |
| `GET` | `/api/v1/barcodes/sku/{sku}/image` | Streams direct SVG image file for label printing |
| `GET` | `/api/v1/barcodes/qr/{sku}` | Generates 2D QR matrix SVG barcode for mobile scanners |
| `GET` | `/api/v1/barcodes/scan/{scannedCode}` | Mobile scanner lookup resolving SKU into stock on-hand and facility bin coordinates |

### 9. Bulk CSV Catalog Import & Export (`/api/v1/bulk`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/api/v1/bulk/import/products` | Bulk upload CSV spreadsheet with row-level validation and batch upsert |
| `GET` | `/api/v1/bulk/export/products` | Download entire product catalog as CSV spreadsheet |
| `GET` | `/api/v1/bulk/export/template` | Download blank starter CSV template for supplier/catalog onboarding |

### 10. Real-Time Webhooks (`/api/v1/webhooks`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/webhooks` | List all registered outbound webhook subscriptions |
| `GET` | `/api/v1/webhooks/{id}` | Retrieve webhook subscription by ID |
| `POST` | `/api/v1/webhooks` | Register a new webhook listener with HMAC secret |
| `PUT` | `/api/v1/webhooks/{id}` | Update webhook endpoint URL or subscribed event list |
| `DELETE` | `/api/v1/webhooks/{id}` | Remove webhook subscription |
| `GET` | `/api/v1/webhooks/{id}/deliveries` | View recent delivery attempt audit logs and HTTP status codes |
| `POST` | `/api/v1/webhooks/{id}/test` | Execute a live ping test with HMAC signature |

### 11. Suppliers & Procurement (`/api/v1/suppliers`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/suppliers` | List active suppliers with contact details, lead times, and payment terms |
| `GET` | `/api/v1/suppliers/{id}` | Retrieve supplier profile by ID |
| `GET` | `/api/v1/suppliers/code/{code}` | Retrieve supplier profile by vendor code |
| `POST` | `/api/v1/suppliers` | Register new supplier vendor |
| `PUT` | `/api/v1/suppliers/{id}` | Update supplier profile and lead times |

### 12. Automated Replenishment & Purchase Orders (`/api/v1/purchase-orders`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/purchase-orders` | Paginated list of purchase orders with status and vendor filters |
| `GET` | `/api/v1/purchase-orders/{id}` | Retrieve purchase order with line item quantities and progress |
| `GET` | `/api/v1/purchase-orders/suggestions` | Low-stock replenishment analysis with recommended reorder quantities |
| `POST` | `/api/v1/purchase-orders/auto-generate` | Automated batch engine grouping low-stock items by vendor and drafting POs |
| `POST` | `/api/v1/purchase-orders` | Manually draft a new purchase order |
| `POST` | `/api/v1/purchase-orders/{id}/submit` | Submit draft PO to vendor (`Submitted`) |
| `POST` | `/api/v1/purchase-orders/{id}/receive` | Receive shipment intake, increment warehouse stock, and recalculate unit costs |
| `POST` | `/api/v1/purchase-orders/{id}/cancel` | Cancel an open purchase order |

### 13. Stock Movements & Transactions (`/api/v1/inventory`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/api/v1/inventory/adjust` | Record stock variance adjustment with audit justification |
| `POST` | `/api/v1/inventory/restock` | Receive inbound stock from supplier (recalculates weighted unit cost) |
| `POST` | `/api/v1/inventory/dispatch` | Fulfill outbound order (validates stock availability, prevents negative inventory) |
| `GET` | `/api/v1/inventory/transactions` | Paginated transaction audit log with date range and product filtering |
| `GET` | `/api/v1/inventory/transactions/product/{productId}` | Retrieve transaction history for a specific product |

### 14. Business Intelligence & Analytics (`/api/v1/inventory/summary`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/inventory/summary` | Real-time total inventory valuation, retail value, gross margin, and category rollups |
| `GET` | `/health` or `/api/v1/health` | Service uptime and database connectivity probe |

## Sample `curl` Requests

### Inspecting BOM Recipe & Max Yield Analytics
```bash
curl -X GET "http://localhost:5000/api/v1/bom/product/9?warehouseId=1"
```

### Executing a Kit Assembly Production Run
```bash
curl -X POST "http://localhost:5000/api/v1/bom/assemble" \
  -H "Content-Type: application/json" \
  -d '{
    "kitProductId": 9,
    "warehouseId": 1,
    "quantity": 2,
    "laborCost": 30.00,
    "assembledBy": "lead_tech",
    "notes": "Assembly run for Q3 order fulfillment"
  }'
```

### Disassembling a Kit Back into Components
```bash
curl -X POST "http://localhost:5000/api/v1/bom/disassemble" \
  -H "Content-Type: application/json" \
  -d '{
    "kitProductId": 9,
    "warehouseId": 1,
    "quantity": 1,
    "disassembledBy": "clerk",
    "reason": "Customer cancellation decomposition"
  }'
```

## Running Tests

To run the full automated test suite:

```bash
dotnet test
```

## Architecture Notes

The Inventory Tracker API is architected around domain-driven rigor and clean separation of concerns. The Bill of Materials (BOM) engine enables light manufacturing and product kitting: parent kits dynamically compute rolled-up acquisition costs from child sub-components, maximum assemblable yield analytics identify inventory bottlenecks in real time, and assembly runs atomically consume sub-components while receiving finished goods into sellable stock.
