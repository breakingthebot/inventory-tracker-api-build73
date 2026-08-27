# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
