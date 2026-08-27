# Inventory Tracker API (Build 73)

A production-grade RESTful Inventory Tracker service built with ASP.NET Core 8.0, Entity Framework Core, SQL Server / In-Memory persistence, multi-warehouse location partitioning, inter-warehouse stock transfers, automated replenishment purchase orders, vector SVG barcode/QR generation, mobile handheld scanner lookup, CSV bulk catalog import/export, outbound webhooks with HMAC-SHA256 signatures, batch lot & expiration tracking with FEFO dispatching, blind cycle counting & reconciliation audits, Bill of Materials (BOM) & kit assembly/disassembly, customer sales order processing & pick-pack-ship fulfillment, Role-Based Access Control (RBAC), JWT authentication, transaction auditing, and real-time business valuation analytics.

## Stack

- **Language / Runtime**: C# 12 / .NET 8.0 SDK
- **Framework**: ASP.NET Core Web API
- **Security / Authentication**: JWT Bearer Tokens, HMAC-SHA256 PBKDF2 Password Hashing, RBAC
- **Integration**: Outbound Webhooks with HMAC-SHA256 signature headers (`X-Inventory-Signature-256`)
- **Fulfillment Pipeline**: Customer Sales Orders with Inventory Allocation, Bin Pick Sheets, Packing, & Carrier Shipment
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

### 2. Customers & Purchasing Accounts (`/api/v1/customers`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/customers` | List all customer purchasing accounts |
| `GET` | `/api/v1/customers/{id}` | Retrieve specific customer account details |
| `POST` | `/api/v1/customers` | Register a new customer purchasing account |
| `PUT` | `/api/v1/customers/{id}` | Update customer profile, contact, and shipping address |

### 3. Customer Sales Orders & Fulfillment (`/api/v1/sales-orders`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/sales-orders` | Query sales orders with status, customer, and warehouse filters |
| `GET` | `/api/v1/sales-orders/{id}` | Retrieve sales order with line items and fulfillment history |
| `POST` | `/api/v1/sales-orders` | Draft a new customer sales order (`Draft`) |
| `POST` | `/api/v1/sales-orders/{id}/allocate` | Validate available stock and reserve quantities at warehouse (`Allocated`) |
| `GET` | `/api/v1/sales-orders/{id}/pick-list` | Generate bin-routed pick sheet for warehouse runners |
| `POST` | `/api/v1/sales-orders/{id}/pick` | Record warehouse item picking completion (`Picked`) |
| `POST` | `/api/v1/sales-orders/{id}/pack` | Record carton packing and assign carrier (`Packed`) |
| `POST` | `/api/v1/sales-orders/{id}/ship` | Dispatch shipment, deduct physical stock, clear reserves, and log audit (`Shipped`) |
| `POST` | `/api/v1/sales-orders/{id}/cancel` | Cancel order and release reserved stock |

### 4. Product Catalog (`/api/v1/products`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/products` | Paginated product listing with keyword search, category, and stock filters |
| `GET` | `/api/v1/products/{id}` | Retrieve product details by integer primary key |
| `GET` | `/api/v1/products/sku/{sku}` | Retrieve product by unique SKU code |
| `GET` | `/api/v1/products/low-stock` | List all items with on-hand stock at or below reorder threshold |
| `POST` | `/api/v1/products` | Create a new catalog product (enforces unique SKU) |
| `PUT` | `/api/v1/products/{id}` | Update product details, prices, and replenishment rules |
| `DELETE` | `/api/v1/products/{id}` | Delete product (fails if on-hand stock is positive) |

### 5. Warehouses & Facility Stock (`/api/v1/warehouses`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/warehouses` | List all physical facilities with storage capacity & utilization metrics |
| `GET` | `/api/v1/warehouses/{id}` | Retrieve facility details by database ID |
| `GET` | `/api/v1/warehouses/code/{code}` | Retrieve facility by code (e.g. `WH-EAST`) |
| `POST` | `/api/v1/warehouses` | Register a new warehouse facility |
| `PUT` | `/api/v1/warehouses/{id}` | Update facility metadata and storage capacity limits |
| `GET` | `/api/v1/warehouses/{id}/stock` | List product on-hand, reserved, and available stock lines per facility |
| `PUT` | `/api/v1/warehouses/{id}/stock/{productId}/bin` | Assign or update physical aisle/rack/shelf bin coordinates |

### 6. Bill of Materials (BOM) & Kitting (`/api/v1/bom`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/bom/product/{productId}` | Full BOM component tree, cost roll-ups, max yield analytics, and bottleneck SKU |
| `POST` | `/api/v1/bom/components` | Attach or update component requirement in parent kit recipe |
| `DELETE` | `/api/v1/bom/components` | Remove sub-component requirement from kit recipe |
| `POST` | `/api/v1/bom/assemble` | Execute kit assembly run, deduct components, receive finished goods, and update cost |
| `POST` | `/api/v1/bom/disassemble` | Disassemble finished kits back into raw sub-component inventory |

### 7. Cycle Counting & Audit Reconciliation (`/api/v1/cycle-counts`)

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

### 8. Product Lots & Expiration Tracking (`/api/v1/lots`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/lots` | Paginated list of lots filtered by product, warehouse, status, or expiration |
| `GET` | `/api/v1/lots/{id}` | Retrieve specific batch lot details with warehouse metadata |
| `POST` | `/api/v1/lots` | Register a new lot batch and increment warehouse stock |
| `PUT` | `/api/v1/lots/{id}` | Update lot operational status (Quarantine/Active) or expiration date |
| `GET` | `/api/v1/lots/expiring` | Expiration risk report calculating units and valuation at risk within day window |
| `GET` | `/api/v1/lots/fefo-plan` | Compute FEFO picking recommendation allocating from earliest-expiring active lots |
| `POST` | `/api/v1/lots/dispatch-fefo` | Execute automated FEFO batch deduction, update lot balances, and log audits |

### 9. Inter-Warehouse Stock Transfers (`/api/v1/transfers`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/transfers` | Paginated list of transfers with status and facility filters |
| `GET` | `/api/v1/transfers/{id}` | Retrieve transfer order details, line items, and tracking numbers |
| `POST` | `/api/v1/transfers` | Initiate stock transfer and reserve inventory at source facility (`Pending`) |
| `POST` | `/api/v1/transfers/{id}/ship` | Ship transfer, deduct source inventory, and set status to `InTransit` |
| `POST` | `/api/v1/transfers/{id}/receive` | Confirm receipt, add destination inventory, and set status to `Received` |
| `POST` | `/api/v1/transfers/{id}/cancel` | Cancel order before shipment and release reserved inventory |

### 10. Barcodes & Handheld Scanner Resolution (`/api/v1/barcodes`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/barcodes/sku/{sku}` | Generates Code 128 linear vector SVG barcode with label metadata |
| `GET` | `/api/v1/barcodes/sku/{sku}/image` | Streams direct SVG image file for label printing |
| `GET` | `/api/v1/barcodes/qr/{sku}` | Generates 2D QR matrix SVG barcode for mobile scanners |
| `GET` | `/api/v1/barcodes/scan/{scannedCode}` | Mobile scanner lookup resolving SKU into stock on-hand and facility bin coordinates |

### 11. Bulk CSV Catalog Import & Export (`/api/v1/bulk`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/api/v1/bulk/import/products` | Bulk upload CSV spreadsheet with row-level validation and batch upsert |
| `GET` | `/api/v1/bulk/export/products` | Download entire product catalog as CSV spreadsheet |
| `GET` | `/api/v1/bulk/export/template` | Download blank starter CSV template for supplier/catalog onboarding |

### 12. Real-Time Webhooks (`/api/v1/webhooks`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/webhooks` | List all registered outbound webhook subscriptions |
| `GET` | `/api/v1/webhooks/{id}` | Retrieve webhook subscription by ID |
| `POST` | `/api/v1/webhooks` | Register a new webhook listener with HMAC secret |
| `PUT` | `/api/v1/webhooks/{id}` | Update webhook endpoint URL or subscribed event list |
| `DELETE` | `/api/v1/webhooks/{id}` | Remove webhook subscription |
| `GET` | `/api/v1/webhooks/{id}/deliveries` | View recent delivery attempt audit logs and HTTP status codes |
| `POST` | `/api/v1/webhooks/{id}/test` | Execute a live ping test with HMAC signature |

### 13. Suppliers & Procurement (`/api/v1/suppliers`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/suppliers` | List active suppliers with contact details, lead times, and payment terms |
| `GET` | `/api/v1/suppliers/{id}` | Retrieve supplier profile by ID |
| `GET` | `/api/v1/suppliers/code/{code}` | Retrieve supplier profile by vendor code |
| `POST` | `/api/v1/suppliers` | Register new supplier vendor |
| `PUT` | `/api/v1/suppliers/{id}` | Update supplier profile and lead times |

### 14. Automated Replenishment & Purchase Orders (`/api/v1/purchase-orders`)

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

### 15. Stock Movements & Transactions (`/api/v1/inventory`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/api/v1/inventory/adjust` | Record stock variance adjustment with audit justification |
| `POST` | `/api/v1/inventory/restock` | Receive inbound stock from supplier (recalculates weighted unit cost) |
| `POST` | `/api/v1/inventory/dispatch` | Fulfill outbound order (validates stock availability, prevents negative inventory) |
| `GET` | `/api/v1/inventory/transactions` | Paginated transaction audit log with date range and product filtering |
| `GET` | `/api/v1/inventory/transactions/product/{productId}` | Retrieve transaction history for a specific product |

### 16. Business Intelligence & Analytics (`/api/v1/inventory/summary`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/inventory/summary` | Real-time total inventory valuation, retail value, gross margin, and category rollups |
| `GET` | `/health` or `/api/v1/health` | Service uptime and database connectivity probe |

## Sample `curl` Requests

### Drafting and Allocating a Sales Order
```bash
curl -X POST "http://localhost:5000/api/v1/sales-orders" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": 1,
    "warehouseId": 1,
    "shippingFee": 15.00,
    "taxAmount": 30.00,
    "notes": "Expedited hospital shipment",
    "items": [
      { "productId": 1, "quantityOrdered": 2 }
    ]
  }'

# Allocate/Reserve inventory for Order 2
curl -X POST "http://localhost:5000/api/v1/sales-orders/2/allocate"
```

### Retrieving Bin Pick List
```bash
curl -X GET "http://localhost:5000/api/v1/sales-orders/2/pick-list"
```

### Shipping Order & Deducting Physical Inventory
```bash
curl -X POST "http://localhost:5000/api/v1/sales-orders/2/ship" \
  -H "Content-Type: application/json" \
  -d '{
    "trackingNumber": "FDX-1234567890",
    "shippingCarrier": "FedEx Ground",
    "shippedBy": "shipping_clerk"
  }'
```

## Running Tests

To run the full automated test suite:

```bash
dotnet test
```

## Architecture Notes

The Inventory Tracker API implements a full pick-pack-ship order fulfillment pipeline: sales orders reserve uncommitted inventory upon allocation (`QuantityReserved`), warehouse runner pick sheets optimize aisle routing, packing verifies cartonization, and shipment dispatch atomically clears reservation balances while decrementing physical on-hand stocks and logging immutable movement audits.
