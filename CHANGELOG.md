# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.9.0] - 2026-08-27

### Added
- **Customer Sales Order Processing & Pick-Pack-Ship Pipeline (`SalesOrdersController` & `CustomersController`)**:
  - `Customer`, `SalesOrder`, `SalesOrderItem`, and `SalesOrderStatus` domain models (`Draft`, `Allocated`, `Picked`, `Packed`, `Shipped`, `Delivered`, `Cancelled`).
  - `GET /api/v1/customers`: List active customer accounts.
  - `GET /api/v1/customers/{id}`: Detailed customer profile retrieval.
  - `POST /api/v1/customers`: Register new customer purchasing profile.
  - `PUT /api/v1/customers/{id}`: Update customer profile and shipping address.
  - `GET /api/v1/sales-orders`: Query sales orders with status, customer, and warehouse filters.
  - `GET /api/v1/sales-orders/{id}`: Detailed order retrieval with line items.
  - `POST /api/v1/sales-orders`: Draft new customer sales order with line item calculations.
  - `POST /api/v1/sales-orders/{id}/allocate`: Validate available inventory and reserve stock at fulfillment facility (`Draft` -> `Allocated`).
  - `GET /api/v1/sales-orders/{id}/pick-list`: Generate bin-routed warehouse runner pick sheet.
  - `POST /api/v1/sales-orders/{id}/pick`: Record physical warehouse picking completion (`Allocated` -> `Picked`).
  - `POST /api/v1/sales-orders/{id}/pack`: Carton packing verification with carrier assignment (`Picked` -> `Packed`).
  - `POST /api/v1/sales-orders/{id}/ship`: Dispatch shipment, deduct physical inventory, clear reserved stock, assign tracking number, log immutable transaction audits, and fire outbound webhooks (`Packed` -> `Shipped`).
  - `POST /api/v1/sales-orders/{id}/cancel`: Void open sales orders and release any reserved inventory back to sellable stock.
- **Seeded Customers & Order**: Initialized realistic corporate customer accounts (`CUST-1001`, `CUST-1002`) and historical shipped order (`SO-20260826-0001`) in `DbInitializer`.
- **Test Suite Expansion**: Added 4 new unit and controller tests in `SalesOrderServiceTests` and `SalesOrdersControllerTests`, expanding test coverage to 72 unit and integration tests with a 100% pass rate.

## [1.8.0] - 2026-08-27

### Added
- **Bill of Materials (BOM) & Kitting / Assembly Decomposition (`BomController`)**:
  - `BillOfMaterials` and `KitAssemblyOrder` domain models.
  - Added `IsBundleOrKit`, `BomComponents`, `UsedInBoms`, and `AssemblyOrders` navigation collections on `Product` and `Warehouse`.
  - `GET /api/v1/bom/product/{productId}`: Full BOM recipe inspection, component cost roll-up calculation, and maximum assemblable yield analytics with bottleneck component identification (`LimitingComponentSku`).
  - `POST /api/v1/bom/components`: Attach or modify sub-component requirements with scrap allowances.
  - `DELETE /api/v1/bom/components`: Remove component requirements from kit recipes.
  - `POST /api/v1/bom/assemble`: Execute atomic kit assembly runs deducting required sub-components, receiving finished kit units, recalculating weighted unit acquisition cost (including direct labor allocation), and logging audit transactions.
  - `POST /api/v1/bom/disassemble`: Disassemble finished kits back into sub-component raw materials and restock warehouse balances.
- **Seeded Product Kit**: Added `KIT-DESK-PRO` ("Professional Workstation Setup Kit") bundled with 4K Monitor, USB-C Hub, and Desk Mat in `DbInitializer`.
- **Test Suite Expansion**: Added 4 new unit and controller tests in `BomServiceTests` and `BomControllerTests`, expanding test coverage to 68 unit and integration tests with a 100% pass rate.

## [1.7.0] - 2026-08-27

### Added
- **Inventory Cycle Counting & Audit Reconciliation (`CycleCountsController`)**:
  - `CycleCount`, `CycleCountItem`, and `CycleCountStatus` domain models (`Draft`, `InProgress`, `UnderReview`, `Reconciled`, `Cancelled`).
  - `GET /api/v1/cycle-counts`: Paginated audit sessions with status and warehouse facility filters.
  - `GET /api/v1/cycle-counts/{id}`: Detailed session retrieval with line items and count history.
  - `POST /api/v1/cycle-counts`: Initiate audit session snapshotting current on-hand stock per facility.
  - `POST /api/v1/cycle-counts/{id}/record-counts`: Batch blind count submission for warehouse floor clerks.
  - `POST /api/v1/cycle-counts/{id}/submit-review`: Submit completed counts for supervisor review.
  - `GET /api/v1/cycle-counts/{id}/variance-report`: Detailed variance report comparing counted vs system stock with net/absolute cost variances and inventory accuracy percentage.
  - `POST /api/v1/cycle-counts/{id}/reconcile`: Approve discrepancies, post automated balancing inventory transactions, and update stock levels.
  - `POST /api/v1/cycle-counts/{id}/cancel`: Void open audit session.
- **Seeded Cycle Count**: Initialized realistic reconciled cycle count session (`CC-20260826-001`) in `DbInitializer`.
- **Test Suite Expansion**: Added 4 new unit and controller tests in `CycleCountServiceTests` and `CycleCountsControllerTests`, expanding test coverage to 64 unit and integration tests with a 100% pass rate.

## [1.6.0] - 2026-08-27

### Added
- **Product Lots & Expiration Tracking (`LotsController`)**:
  - `ProductLot` entity with manufacturing date, expiration date, on-hand/reserved quantities per facility, and `LotStatus` (`Active`, `Quarantine`, `Expired`, `Depleted`).
  - `GET /api/v1/lots`: Paginated product lot listing with filters for product, facility, status, and expired items.
  - `GET /api/v1/lots/{id}`: Detailed lot retrieval with product and warehouse metadata.
  - `POST /api/v1/lots`: Register new batch lot and increment warehouse stock.
  - `PUT /api/v1/lots/{id}`: Update lot operational status (quarantine hold/release) or expiration date.
  - `GET /api/v1/lots/expiring`: Real-time expiration risk report aggregating units and financial valuation at risk within configurable day thresholds.
  - `GET /api/v1/lots/fefo-plan`: First-Expired, First-Out (FEFO) recommendation algorithm allocating units from oldest / soonest-expiring active lots.
  - `POST /api/v1/lots/dispatch-fefo`: Automated batch dispatch deducting quantities across lots following FEFO rules, updating balances, and recording audit transactions.
- **Product Model Updates**: Added `IsLotTracked` flag and `ProductLots` navigation collections.
- **Seeded Lot Batches**: Initialized realistic batch lots for sensitive items (`APP-GLV-NIT100`, `OFF-PAP-A4500`).
- **Test Suite Expansion**: Added 4 new unit and controller tests in `LotTrackingServiceTests` and `LotsControllerTests`, expanding test coverage to 60 unit and integration tests with a 100% pass rate.

## [1.5.0] - 2026-08-27

### Added
- **Outbound Webhooks & Event Notifications (`WebhooksController`)**:
  - `WebhookSubscription` and `WebhookDeliveryLog` models.
  - `GET /api/v1/webhooks`: List all webhook listener subscriptions.
  - `GET /api/v1/webhooks/{id}`: Detailed webhook subscription retrieval.
  - `POST /api/v1/webhooks`: Register new webhook listener with HMAC secret.
  - `PUT /api/v1/webhooks/{id}`: Update endpoint URL or subscribed event filters.
  - `DELETE /api/v1/webhooks/{id}`: Remove webhook subscription.
  - `GET /api/v1/webhooks/{id}/deliveries`: View recent delivery attempt audit logs, HTTP status codes, and execution duration.
  - `POST /api/v1/webhooks/{id}/test`: Execute live verification ping with HMAC signature.
- **HMAC-SHA256 Security**: Outbound payloads signed with `X-Inventory-Signature-256` and `X-Inventory-Event` headers.
- **Test Suite Expansion**: Added 5 new unit and controller tests in `WebhookServiceTests` and `WebhooksControllerTests`, expanding test coverage to 56 unit and integration tests with a 100% pass rate.

## [1.4.0] - 2026-08-27

### Added
- **Role-Based Access Control (RBAC) & User Security**:
  - `User` and `UserRole` models (`Admin`, `WarehouseManager`, `Clerk`, `Auditor`).
  - Salted PBKDF2 HMAC-SHA256 password hashing (100,000 iterations).
  - JWT Bearer token generation with claims (`NameIdentifier`, `Name`, `Email`, `Role`) and 24-hour expiration.
  - Swagger UI Bearer token authorization dialog integration.
- **Authentication Endpoints (`AuthController`)**:
  - `POST /api/v1/auth/login`: Authenticates credentials and returns signed JWT Bearer access token.
  - `POST /api/v1/auth/register`: Creates new operator account with specified role (`Admin` required).
  - `GET /api/v1/auth/me`: Retrieves current authenticated user profile from token claims.
  - `GET /api/v1/auth/users`: Lists all system operator accounts (`Admin` required).
- **Default Seed Accounts**:
  - `admin` (Role: `Admin`, Password: `AdminPass123!`)
  - `manager` (Role: `WarehouseManager`, Password: `ManagerPass123!`)
  - `clerk` (Role: `Clerk`, Password: `ClerkPass123!`)
  - `auditor` (Role: `Auditor`, Password: `AuditorPass123!`)
- **Test Suite Expansion**: Added 6 new unit and controller tests in `AuthServiceTests` and `AuthControllerTests`, expanding test coverage to 51 unit and integration tests with a 100% pass rate.

## [1.3.0] - 2026-08-27

### Added
- **Barcode & QR Code Generation (`BarcodesController`)**:
  - `GET /api/v1/barcodes/sku/{sku}`: Generates linear Code 128 / Code 39 vector SVG barcodes with human-readable text labels.
  - `GET /api/v1/barcodes/sku/{sku}/image`: Streams direct SVG image files for barcode label printing.
  - `GET /api/v1/barcodes/qr/{sku}`: Generates 2D QR matrix SVGs with finder patterns for mobile warehouse scanner reading.
  - `GET /api/v1/barcodes/scan/{scannedCode}`: Resolves scanned barcode strings into instant product metadata, stock on-hand, and per-facility bin coordinates.
- **Bulk CSV Import & Export (`BulkController`)**:
  - `POST /api/v1/bulk/import/products`: High-throughput CSV catalog parser with row-level validation error tracking, automatic category assignment, and batch product upserts.
  - `GET /api/v1/bulk/export/products`: Exports complete catalog snapshot as a downloadable CSV spreadsheet.
  - `GET /api/v1/bulk/export/template`: Downloads blank starter CSV template with example records for supplier/catalog onboarding.
- **Test Suite Expansion**: Added 11 new tests in `BarcodeServiceTests`, `BulkDataServiceTests`, `BarcodesControllerTests`, and `BulkControllerTests`, expanding test coverage to 45 unit and integration tests with a 100% pass rate.

## [1.2.0] - 2026-08-27

### Added
- **Supplier & Purchase Order Models**: Added `Supplier`, `PurchaseOrder`, and `PurchaseOrderItem` domain entities with relational foreign keys, tracking numbers, and decimal price precision.
- **Product Supplier Sourcing**: Updated `Product` model with `PrimarySupplierId` and lead time awareness.
- **Supplier Management Endpoints (`SuppliersController`)**:
  - `GET /api/v1/suppliers`: List active vendors with lead times and payment terms.
  - `GET /api/v1/suppliers/{id}`: Detailed supplier retrieval by ID.
  - `GET /api/v1/suppliers/code/{code}`: Supplier retrieval by unique code.
  - `POST /api/v1/suppliers`: Register new vendor profile.
  - `PUT /api/v1/suppliers/{id}`: Update vendor contact and terms.
- **Automated Purchase Order Workflows (`PurchaseOrdersController`)**:
  - `GET /api/v1/purchase-orders/suggestions`: Real-time replenishment analysis identifying low-stock and out-of-stock catalog deficits.
  - `POST /api/v1/purchase-orders/auto-generate`: Automated engine grouping low-stock items by supplier and drafting POs with projected delivery dates.
  - `POST /api/v1/purchase-orders`: Manual purchase order drafting.
  - `POST /api/v1/purchase-orders/{id}/submit`: Transmit order to vendor (`Draft` -> `Submitted`).
  - `POST /api/v1/purchase-orders/{id}/receive`: Record full/partial goods receipt, increment warehouse and product stock, log immutable `StockIn` transaction, and recalculate weighted average unit costs.
  - `POST /api/v1/purchase-orders/{id}/cancel`: Void open purchase orders.
- **Test Suite Expansion**: Added 7 new tests in `SupplierServiceTests`, `PurchaseOrderServiceTests`, and `PurchaseOrdersControllerTests`, reaching 34 passing tests with 100% success rate.

## [1.1.0] - 2026-08-27

### Added
- **Multi-Warehouse Domain Models**: Added `Warehouse`, `WarehouseStock` (per-facility on-hand balance, reservations, and bin locations), `StockTransfer`, and `StockTransferItem` entities with relational navigation in `InventoryDbContext`.
- **Facility Management Endpoints (`WarehousesController`)**:
  - `GET /api/v1/warehouses`: List all warehouse facilities with capacity and utilization rollups.
  - `GET /api/v1/warehouses/{id}`: Detailed facility retrieval by primary ID.
  - `GET /api/v1/warehouses/code/{code}`: Facility retrieval by unique code (e.g. `WH-EAST`).
  - `POST /api/v1/warehouses`: Register new warehouse facility.
  - `PUT /api/v1/warehouses/{id}`: Update warehouse metadata and storage volume capacity.
  - `GET /api/v1/warehouses/{id}/stock`: List on-hand and available stock lines per warehouse.
  - `PUT /api/v1/warehouses/{id}/stock/{productId}/bin`: Update aisle/rack/shelf bin coordinates.
- **Inter-Warehouse Transfer Workflows (`TransfersController`)**:
  - `GET /api/v1/transfers`: Query transfer orders with status/facility filters and pagination.
  - `GET /api/v1/transfers/{id}`: Get transfer order with line items and tracking metadata.
  - `POST /api/v1/transfers`: Create transfer order and reserve source stock (`Pending`).
  - `POST /api/v1/transfers/{id}/ship`: Mark transfer `InTransit`, deduct source inventory, and record outbound transaction.
  - `POST /api/v1/transfers/{id}/receive`: Mark transfer `Received`, add destination inventory, and record inbound transaction.
  - `POST /api/v1/transfers/{id}/cancel`: Cancel transfer before shipment and release reserved inventory.
- **Seeder Expansion**: Seeded 3 regional fulfillment facilities (`WH-EAST` Atlanta, `WH-WEST` Reno, `WH-CENTRAL` Dallas) with realistic distributed inventory balances and bin coordinates.
- **Extended Test Suite**: Added 11 new tests in `WarehouseServiceTests`, `TransferServiceTests`, and `TransfersControllerTests`, expanding coverage to 27 tests with 100% pass rate.

## [1.0.0] - 2026-08-26

### Added
- **Domain Models & Entity Framework Core**: Configured `Product`, `Category`, and `InventoryTransaction` entity models with decimal precision, unique SKU indexes, and relational mapping in `InventoryDbContext`.
- **Database Seeder**: Added `DbInitializer` seeding 5 categories, 8 product items, and opening inventory balance audit records.
- **Product Catalog Endpoints (`ProductsController`)**:
  - `GET /api/v1/products`: Multi-field keyword search, category filtering, stock status filters, sorting, and pagination.
  - `GET /api/v1/products/{id}`: Single product retrieval by database ID.
  - `GET /api/v1/products/sku/{sku}`: Single product retrieval by unique SKU.
  - `GET /api/v1/products/low-stock`: Filter for products needing replenishment.
  - `POST /api/v1/products`: Product creation with unique SKU constraint validation.
  - `PUT /api/v1/products/{id}`: Product metadata and pricing updates.
  - `DELETE /api/v1/products/{id}`: Safe deletion validating zero on-hand stock.
- **Inventory Stock Operations (`InventoryController`)**:
  - `POST /api/v1/inventory/adjust`: Variance adjustment with audit logging.
  - `POST /api/v1/inventory/restock`: Inbound replenishment with weighted average cost calculation.
  - `POST /api/v1/inventory/dispatch`: Outbound fulfillment with stock shortage prevention.
  - `GET /api/v1/inventory/transactions`: Paginated audit history with date range and product filtering.
  - `GET /api/v1/inventory/transactions/product/{productId}`: Product-specific stock history.
- **Business Intelligence & Analytics (`AnalyticsController`)**:
  - `GET /api/v1/inventory/summary`: Inventory valuation, retail potential, gross margin, low/out-of-stock counts, and category breakdown.
- **Diagnostics & Health (`HealthController`)**:
  - `GET /health` & `GET /api/v1/health`: Service uptime, environment, and DB connectivity checks.
- **HTTP Middleware**:
  - `GlobalExceptionMiddleware`: Standardized JSON error response formatting for unhandled exceptions.
  - `RequestLoggingMiddleware`: Performance timing and request logging.
- **OpenAPI / Swagger**: Interactive UI served at application root `/`.
- **Testing Suite**: 16 unit tests covering catalog CRUD, stock movements, business valuation, and controller endpoints using xUnit, InMemory EF Core, and Moq.
- **CI Pipeline**: Automated GitHub Actions CI workflow for .NET 8.
