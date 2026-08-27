# Iteration 05 Summary: Role-Based Access Control (RBAC) & JWT Authentication

## Plain English Summary

In this iteration, we secured the **ASP.NET Core Inventory Tracker API** with an enterprise **Role-Based Access Control (RBAC)** security system and signed **JSON Web Token (JWT) Bearer Authentication**.

The system now enforces authentication and permission segregation across four operational personas:
1. **System Administrator (`Admin`)**: Unrestricted administrative authority, user account creation, system configuration.
2. **Warehouse Operations Manager (`WarehouseManager`)**: Inter-warehouse transfers, supplier management, batch PO auto-generation.
3. **Inventory Stock Clerk (`Clerk`)**: Floor operations, barcode scanning, outbound dispatching, PO shipment receiving.
4. **Financial & Inventory Auditor (`Auditor`)**: Read-only valuation summaries, gross margin analytics, cycle counts, and immutable transaction audit logs.

Security implementations:
- **PBKDF2 Password Hashing**: Passwords stored using HMAC-SHA256 PBKDF2 key derivation (100,000 iterations) with cryptographic per-user 128-bit salts.
- **JWT Token Issuance**: Signed HMAC-SHA256 JWT access tokens containing identity claims (`NameIdentifier`, `Name`, `Email`, `Role`) and 24-hour expiration.
- **OpenAPI / Swagger Authorization**: Integrated interactive Bearer token dialog in Swagger UI.
- **Pre-Seeded Accounts**: Initialized with default users for each role (`admin`, `manager`, `clerk`, `auditor`).

The automated test suite was expanded with 6 new tests, bringing the total to 51 unit and integration tests with a 100% pass rate.

---

## File Table & Connections

| File Path | Description | Connects To |
| :--- | :--- | :--- |
| `src/InventoryTracker.Api/Models/UserRole.cs` | Enum defining access roles (`Admin`, `WarehouseManager`, `Clerk`, `Auditor`). | `User.cs`, `AuthService.cs` |
| `src/InventoryTracker.Api/Models/User.cs` | Domain entity storing user accounts, PBKDF2 hashes, salts, and login audits. | `UserRole.cs`, `InventoryDbContext.cs` |
| `src/InventoryTracker.Api/DTOs/AuthDtos.cs` | DTO contracts for login requests, JWT token responses, and user registration. | `AuthController.cs`, `AuthService.cs` |
| `src/InventoryTracker.Api/Services/IAuthService.cs` | Service interface for password cryptography and JWT token issuance. | `AuthService.cs`, `AuthController.cs` |
| `src/InventoryTracker.Api/Services/AuthService.cs` | Implementation executing PBKDF2 hashing, salt generation, and JWT token signing. | `InventoryDbContext.cs`, `IAuthService.cs` |
| `src/InventoryTracker.Api/Controllers/AuthController.cs` | REST controller exposing login, registration, profile `/me`, and user directory endpoints. | `IAuthService.cs` |
| `src/InventoryTracker.Api/Data/InventoryDbContext.cs` | Added `DbSet<User>` and user entity mappings. | Entity Models, `Program.cs` |
| `src/InventoryTracker.Api/Data/DbInitializer.cs` | Updated to seed default user accounts across all 4 RBAC roles. | `InventoryDbContext.cs`, `Program.cs` |
| `src/InventoryTracker.Api/Program.cs` | Configured `JwtBearer` authentication, Authorization, and Swagger Bearer security. | Application Root |
| `tests/InventoryTracker.Tests/Services/AuthServiceTests.cs` | Unit tests for PBKDF2 hashing, salt verification, registration, and login tokens. | `AuthService.cs` |
| `tests/InventoryTracker.Tests/Controllers/AuthControllerTests.cs` | Unit tests for authentication HTTP action results. | `AuthController.cs` |
| `README.md` | Updated with authentication documentation, default credentials table, and curl examples. | Repository root |
| `CHANGELOG.md` | Logged v1.4.0 release notes. | Repository root |
| `BUILD_NOTES.md` | Appended conversational build log for Iteration 5 (ignored in git). | Repository root |

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
   *Expected output*: `Passed: 51, Failed: 0, Total: 51`.

3. Launch the API:
   ```powershell
   dotnet run --project src/InventoryTracker.Api
   ```

4. Test authentication workflows:
   - **Login as Administrator**:
     ```bash
     curl -i -X POST http://localhost:5000/api/v1/auth/login \
       -H "Content-Type: application/json" \
       -d '{"username": "admin", "password": "AdminPass123!"}'
     ```
   - **Query Current Profile with Bearer Token**:
     ```bash
     curl -i -X GET http://localhost:5000/api/v1/auth/me \
       -H "Authorization: Bearer <your-token-from-login>"
     ```
   - **Register a New Operator (Admin Only)**:
     ```bash
     curl -i -X POST http://localhost:5000/api/v1/auth/register \
       -H "Authorization: Bearer <your-token-from-login>" \
       -H "Content-Type: application/json" \
       -d '{
         "username": "night_supervisor",
         "email": "night_sup@inventory.local",
         "fullName": "Night Shift Supervisor",
         "password": "NightShiftPass123!",
         "role": "WarehouseManager"
       }'
     ```
