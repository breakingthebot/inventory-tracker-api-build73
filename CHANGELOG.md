# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
