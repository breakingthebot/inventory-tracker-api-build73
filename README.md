# Inventory Tracker API (Build 73)

A production-grade RESTful Inventory Tracker service built with ASP.NET Core 8.0, Entity Framework Core, SQL Server / In-Memory persistence, transaction auditing, weighted average unit cost tracking, and real-time business valuation analytics.

## Stack

- **Language / Runtime**: C# 12 / .NET 8.0 SDK
- **Framework**: ASP.NET Core Web API
- **ORM / Persistence**: Entity Framework Core 8.0 (SQL Server & In-Memory providers)
- **API Documentation**: OpenAPI 3.0 / Swagger UI (`Swashbuckle.AspNetCore`)
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
| `Logging__LogLevel__Default` | Application logging threshold | `Information` |

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

### 1. Product Catalog (`/api/v1/products`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/products` | Paginated product listing with keyword search, category, and stock filters |
| `GET` | `/api/v1/products/{id}` | Retrieve product details by integer primary key |
| `GET` | `/api/v1/products/sku/{sku}` | Retrieve product by unique SKU code |
| `GET` | `/api/v1/products/low-stock` | List all items with on-hand stock at or below reorder threshold |
| `POST` | `/api/v1/products` | Create a new catalog product (enforces unique SKU) |
| `PUT` | `/api/v1/products/{id}` | Update product details, prices, and replenishment rules |
| `DELETE` | `/api/v1/products/{id}` | Delete product (fails if on-hand stock is positive) |

### 2. Stock Operations & Auditing (`/api/v1/inventory`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/api/v1/inventory/adjust` | Record stock variance adjustment with audit justification |
| `POST` | `/api/v1/inventory/restock` | Receive inbound stock from supplier (recalculates weighted unit cost) |
| `POST` | `/api/v1/inventory/dispatch` | Fulfill outbound order (validates stock availability, prevents negative inventory) |
| `GET` | `/api/v1/inventory/transactions` | Paginated transaction audit log with date range and product filtering |
| `GET` | `/api/v1/inventory/transactions/product/{productId}` | Retrieve transaction history for a specific product |

### 3. Business Intelligence & Analytics (`/api/v1/inventory/summary`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/inventory/summary` | Real-time total inventory valuation, retail value, gross margin, and category rollups |
| `GET` | `/health` or `/api/v1/health` | Service uptime and database connectivity probe |

## Sample `curl` Requests

### Create a Product
```bash
curl -X POST "http://localhost:5000/api/v1/products" \
  -H "Content-Type: application/json" \
  -d '{
    "sku": "ELEC-WEBCAM-4K",
    "name": "Ultra HD 4K Streaming Webcam",
    "description": "Auto-focus webcam with dual noise-canceling microphones",
    "categoryId": 1,
    "unitPrice": 129.99,
    "unitCost": 64.50,
    "initialQuantity": 30,
    "reorderThreshold": 10,
    "reorderQuantity": 40,
    "unitOfMeasure": "pcs"
  }'
```

### Inbound Restock
```bash
curl -X POST "http://localhost:5000/api/v1/inventory/restock" \
  -H "Content-Type: application/json" \
  -d '{
    "productId": 1,
    "quantity": 25,
    "unitCost": 205.00,
    "purchaseOrderNumber": "PO-2026-8812",
    "notes": "Q3 Restock shipment received from supplier"
  }'
```

### Outbound Dispatch
```bash
curl -X POST "http://localhost:5000/api/v1/inventory/dispatch" \
  -H "Content-Type: application/json" \
  -d '{
    "productId": 1,
    "quantity": 5,
    "salesOrderNumber": "SO-99214",
    "notes": "Direct customer order fulfillment"
  }'
```

### Get Financial Valuation & Category Summary
```bash
curl -X GET "http://localhost:5000/api/v1/inventory/summary"
```

## Running Tests

To run the full automated test suite:

```bash
dotnet test
```

## Data Handling & Privacy

- **Persistence**: All catalog records, stock quantities, and audit logs are retained solely for warehouse operations.
- **Redaction**: No personal customer data or financial payment card details are stored or logged in this service.
- **Audit Immutability**: Stock transactions are recorded as append-only audit entries to maintain accounting traceability.

## Architecture Notes

The Inventory Tracker API is structured following domain-driven separation of concerns and clean architecture principles. Domain models (`Product`, `Category`, `InventoryTransaction`) model real-world warehouse entities with strict data integrity rules (unique SKU constraints, decimal precision for currency amounts, and relational foreign keys). 

Mutations are isolated within specialized domain services (`ProductService`, `InventoryService`, `AnalyticsService`), ensuring that critical business rules—such as preventing stock from dropping below zero, tracking historical transactions upon balance alterations, and recalculating weighted average costs on restock—are centralized and testable independently of HTTP transport layers.

Requests flow through `GlobalExceptionMiddleware` for unified error normalization, `RequestLoggingMiddleware` for performance monitoring, and standard ASP.NET Core dependency injection. The database provider is decoupled, enabling in-memory execution for rapid local prototyping and test execution alongside production SQL Server compatibility.
