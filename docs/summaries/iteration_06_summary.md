# Iteration 06 Summary: Real-Time Webhook Notifications & Low-Stock Alerts via Email/Slack

## Plain English Summary

In this iteration, we expanded the **ASP.NET Core Inventory Tracker API** with an enterprise **Outbound Webhook Notification Engine** featuring cryptographic **HMAC-SHA256 Request Signing** and **Delivery Audit Logging**.

The system now enables external services (such as Slack channels, Microsoft Teams bots, external ERP systems, and 3PL fulfillment partners) to subscribe to real-time inventory lifecycle events:
1. **Event Types**:
   - `StockLow`: Triggered when product stock drops below safety thresholds.
   - `StockOut`: Triggered when product on-hand inventory reaches 0.
   - `TransferShipped`: Triggered when inter-warehouse stock transfer departs source dock.
   - `TransferReceived`: Triggered when stock transfer arrives and is received at destination.
   - `PurchaseOrderFulfilled`: Triggered when vendor shipment completes.
   - `StockAdjusted`: Triggered when inventory variance adjustments occur.
2. **Cryptographic HMAC Security**: Outbound HTTP POST payloads are signed with `X-Inventory-Signature-256: sha256={hex}` computed against the subscription's secret key, enabling listeners to verify authenticity.
3. **Delivery Logs & Health Metrics**: Tracks execution duration in milliseconds, HTTP response status codes, consecutive failure counts, and error messages (`GET /api/v1/webhooks/{id}/deliveries`).
4. **Live Ping Verification**: Operators can test endpoint connectivity on demand (`POST /api/v1/webhooks/{id}/test`).

The automated test suite was expanded with 5 new tests, bringing the total to 56 unit and integration tests with a 100% pass rate.

---

## File Table & Connections

| File Path | Description | Connects To |
| :--- | :--- | :--- |
| `src/InventoryTracker.Api/Models/WebhookEventType.cs` | Enum defining triggerable domain events. | `WebhookSubscription.cs`, `WebhookDeliveryLog.cs` |
| `src/InventoryTracker.Api/Models/WebhookSubscription.cs` | Domain entity storing endpoint URLs, secrets, and subscribed event filters. | `WebhookDeliveryLog.cs`, `InventoryDbContext.cs` |
| `src/InventoryTracker.Api/Models/WebhookDeliveryLog.cs` | Domain entity storing HTTP status codes, durations, and payloads. | `WebhookSubscription.cs`, `InventoryDbContext.cs` |
| `src/InventoryTracker.Api/DTOs/WebhookDtos.cs` | DTO contracts for webhook subscriptions, delivery logs, and payloads. | `WebhooksController.cs`, `WebhookService.cs` |
| `src/InventoryTracker.Api/Services/IWebhookService.cs` | Service interface for webhook subscription management, HMAC signing, and dispatching. | `WebhookService.cs`, `WebhooksController.cs` |
| `src/InventoryTracker.Api/Services/WebhookService.cs` | Implementation executing HMAC-SHA256 signature calculations and HTTP dispatching. | `InventoryDbContext.cs`, `IWebhookService.cs` |
| `src/InventoryTracker.Api/Controllers/WebhooksController.cs` | REST controller exposing subscription CRUD, delivery log audits, and test pings. | `IWebhookService.cs` |
| `src/InventoryTracker.Api/Data/InventoryDbContext.cs` | Added `WebhookSubscriptions` and `WebhookDeliveryLogs` mappings. | Entity Models, `Program.cs` |
| `src/InventoryTracker.Api/Program.cs` | Registered `HttpClient` and `IWebhookService` in dependency injection. | Application Root |
| `tests/InventoryTracker.Tests/Services/WebhookServiceTests.cs` | Unit tests for HMAC calculation, subscription management, and event publishing. | `WebhookService.cs` |
| `tests/InventoryTracker.Tests/Controllers/WebhooksControllerTests.cs` | Unit tests for webhook REST action endpoints. | `WebhooksController.cs` |
| `README.md` | Updated with Webhook documentation, security headers, and curl examples. | Repository root |
| `CHANGELOG.md` | Logged v1.5.0 release notes. | Repository root |
| `BUILD_NOTES.md` | Appended conversational build log for Iteration 6 (ignored in git). | Repository root |

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
   *Expected output*: `Passed: 56, Failed: 0, Total: 56`.

3. Launch the API:
   ```powershell
   dotnet run --project src/InventoryTracker.Api
   ```

4. Test webhook workflows:
   - **Register Webhook Subscription**:
     ```bash
     curl -i -X POST http://localhost:5000/api/v1/webhooks \
       -H "Content-Type: application/json" \
       -d '{
         "name": "Slack Channel Alert",
         "targetUrl": "https://httpbin.org/post",
         "secretKey": "super_secret_webhook_key_12345",
         "subscribedEvents": "StockLow,StockOut"
       }'
     ```
   - **Send Test Ping to Endpoint (assume ID 1)**:
     ```bash
     curl -i -X POST http://localhost:5000/api/v1/webhooks/1/test
     ```
   - **View Delivery Logs**:
     ```bash
     curl -i http://localhost:5000/api/v1/webhooks/1/deliveries
     ```
