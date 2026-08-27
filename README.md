# Inventory Tracker API (Build 73)

A production-grade RESTful Inventory Tracker service built with ASP.NET Core 8.0, Entity Framework Core, SQL Server / In-Memory persistence, multi-warehouse location partitioning, inter-warehouse stock transfers, transaction auditing, and real-time business valuation analytics.

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

### 2. Warehouses & Facility Stock (`/api/v1/warehouses`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/warehouses` | List all physical facilities with storage capacity & utilization metrics |
| `GET` | `/api/v1/warehouses/{id}` | Retrieve facility details by database ID |
| `GET` | `/api/v1/warehouses/code/{code}` | Retrieve facility by code (e.g. `WH-EAST`) |
| `POST` | `/api/v1/warehouses` | Register a new warehouse facility |
| `PUT` | `/api/v1/warehouses/{id}` | Update facility metadata and storage capacity limits |
| `GET` | `/api/v1/warehouses/{id}/stock` | List product on-hand, reserved, and available stock lines per facility |
| `PUT` | `/api/v1/warehouses/{id}/stock/{productId}/bin` | Assign or update physical aisle/rack/shelf bin coordinates |

### 3. Inter-Warehouse Stock Transfers (`/api/v1/transfers`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/transfers` | Paginated list of transfers with status and facility filters |
| `GET` | `/api/v1/transfers/{id}` | Retrieve transfer order details, line items, and tracking numbers |
| `POST` | `/api/v1/transfers` | Initiate stock transfer and reserve inventory at source facility (`Pending`) |
| `POST` | `/api/v1/transfers/{id}/ship` | Ship transfer, deduct source inventory, and set status to `InTransit` |
| `POST` | `/api/v1/transfers/{id}/receive` | Confirm receipt, add destination inventory, and set status to `Received` |
| `POST` | `/api/v1/transfers/{id}/cancel` | Cancel order before shipment and release reserved inventory |

### 4. Stock Movements & Transactions (`/api/v1/inventory`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/api/v1/inventory/adjust` | Record stock variance adjustment with audit justification |
| `POST` | `/api/v1/inventory/restock` | Receive inbound stock from supplier (recalculates weighted unit cost) |
| `POST` | `/api/v1/inventory/dispatch` | Fulfill outbound order (validates stock availability, prevents negative inventory) |
| `GET` | `/api/v1/inventory/transactions` | Paginated transaction audit log with date range and product filtering |
| `GET` | `/api/v1/inventory/transactions/product/{productId}` | Retrieve transaction history for a specific product |

### 5. Business Intelligence & Analytics (`/api/v1/inventory/summary`)

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/inventory/summary` | Real-time total inventory valuation, retail value, gross margin, and category rollups |
| `GET` | `/health` or `/api/v1/health` | Service uptime and database connectivity probe |

## Sample `curl` Requests

### Register a Warehouse Facility
```bash
curl -X POST "http://localhost:5000/api/v1/warehouses" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "WH-SOUTH",
    "name": "Miami International Logistics Gateway",
    "address": "7700 NW 37th Ave",
    "city": "Miami",
    "state": "FL",
    "postalCode": "33147",
    "country": "USA",
    "capacityUnits": 18000
  }'
```

### Initiate an Inter-Warehouse Transfer
```bash
curl -X POST "http://localhost:5000/api/v1/transfers" \
  -H "Content-Type: application/json" \
  -d '{
    "sourceWarehouseId": 1,
    "destinationWarehouseId": 3,
    "requestedBy": "logistics_mgr",
    "notes": "Emergency stock rebalance to Dallas depot",
    "items": [
      {
        "productId": 1,
        "quantity": 10
      }
    ]
  }'
```

### Ship Transfer Order
```bash
curl -X POST "http://localhost:5000/api/v1/transfers/1/ship" \
  -H "Content-Type: application/json" \
  -d '{
    "trackingNumber": "FDX-998241029",
    "notes": "Loaded on Linehaul Trailer 40B"
  }'
```

### Receive Transfer at Destination Warehouse
```bash
curl -X POST "http://localhost:5000/api/v1/transfers/1/receive" \
  -H "Content-Type: application/json" \
  -d '{
    "notes": "Inspected and slotted into Bin C-05-10"
  }'
```

## Running Tests

To run the full automated test suite:

```bash
dotnet test
```

## Architecture Notes

The Inventory Tracker API is structured following domain-driven separation of concerns and clean architecture principles. Domain models (`Product`, `Category`, `Warehouse`, `WarehouseStock`, `StockTransfer`, `InventoryTransaction`) represent physical logistics structures with strict integrity constraints.

Inter-warehouse stock transfers follow a resilient multi-stage state machine (`Draft` -> `Pending` -> `InTransit` -> `Received` / `Cancelled`). Inventory is first reserved upon creation, deducted and audited as `StockOut` upon shipment, and incremented and audited as `StockIn` upon destination receiving. This prevents inventory double-counting while goods are physically on the road.

Requests flow through `GlobalExceptionMiddleware` for unified error normalization, `RequestLoggingMiddleware` for performance monitoring, and standard ASP.NET Core dependency injection. The database provider is decoupled, enabling in-memory execution for rapid local prototyping and test execution alongside production SQL Server compatibility.
