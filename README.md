# License Management System
### Government-Style · .NET 8 · Microservices · CQRS · JWT · MVC

---

## Architecture

```
Browser (MVC UI :5003)
    │
    ▼
API Gateway (Ocelot :5000)   ← JWT validation on every request
    │
    ├── /api/auth/*    → LicenseService (:5001)   [Auth endpoints — no JWT required]
    ├── /api/license/* → LicenseService (:5001)   [CQRS via MediatR]
    └── /api/upload/*  → DocumentService (:5002)  [File storage]
```

---

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 8.0+ |
| SQL Server | 2019/2022 or LocalDB |
| Visual Studio | 2022 (or VS Code + C# Dev Kit) |

---

## Quick Start

### 1. Clone / Extract

```bash
cd LicenseManagementSystem
```

### 2. Configure Connection String

Edit `src/LicenseService/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=LicenseManagementDB;Trusted_Connection=True"
}
```

> For full SQL Server: `Server=.;Database=LicenseManagementDB;Trusted_Connection=True;TrustServerCertificate=True`

### 3. Apply Database Migrations

```bash
dotnet ef migrations add InitialCreate --project src/LicenseService
dotnet ef database update --project src/LicenseService
```

> **Or skip this step** — `Program.cs` calls `db.Database.Migrate()` on startup automatically.

### 4. Run All Services

Open **4 terminal windows** and run each service:

```bash
# Terminal 1 — API Gateway
cd src/ApiGateway && dotnet run

# Terminal 2 — License Service
cd src/LicenseService && dotnet run

# Terminal 3 — Document Service
cd src/DocumentService && dotnet run

# Terminal 4 — Web UI
cd src/WebUI && dotnet run
```

### 5. Open the App

Navigate to: **http://localhost:5003**

---

## Default Credentials

| Role | Username | Password |
|------|----------|----------|
| Admin | `admin` | `Admin@123` |
| User | *(register a new account)* | — |

---

## Service Ports

| Service | Port |
|---------|------|
| API Gateway | 5000 |
| License Service | 5001 |
| Document Service | 5002 |
| Web UI (MVC) | 5003 |

---

## Project Structure

```
LicenseManagementSystem.sln
├── src/
│   ├── ApiGateway/                  Ocelot gateway + JWT validation
│   │   ├── Program.cs
│   │   └── ocelot.json              Route configuration
│   │
│   ├── LicenseService/              Core service — CQRS pattern
│   │   ├── Commands/                ApplyLicenseCommand, UpdateLicenseStatusCommand
│   │   ├── Queries/                 GetLicensesByUser, GetAllLicenses, GetStats
│   │   ├── Handlers/                MediatR command & query handlers
│   │   ├── Controllers/             AuthController, LicenseController
│   │   ├── Data/                    AppDbContext, EF Core migrations
│   │   ├── Models/                  User, License entities
│   │   └── DTOs/                    Request/Response DTOs
│   │
│   ├── DocumentService/             File upload microservice
│   │   ├── Controllers/             UploadController
│   │   └── Services/                IFileStorageService, LocalFileStorageService
│   │
│   ├── NotificationService/         Pluggable notification service
│   │   └── NotificationService.cs   INotificationService, Console mock, SMTP stub
│   │
│   └── WebUI/                       ASP.NET Core MVC frontend
│       ├── Controllers/             AccountController, DashboardController, HomeController
│       ├── Models/                  ViewModels (Login, Register, Apply, Dashboard)
│       ├── Services/                GatewayClient (HTTP wrapper)
│       ├── Views/
│       │   ├── Account/             Login.cshtml, Register.cshtml
│       │   ├── Dashboard/           AdminDashboard.cshtml, UserDashboard.cshtml, Apply.cshtml
│       │   ├── Home/                Error.cshtml
│       │   └── Shared/              _Layout.cshtml, _ValidationScriptsPartial.cshtml
│       └── wwwroot/
│           ├── css/gov.css          Government-style stylesheet
│           └── js/gov.js            Sidebar toggle, file preview
│
└── tests/
    └── LicenseService.UnitTests/    xUnit + Moq tests for all handlers
        └── HandlerTests.cs
```

---

## Running Unit Tests

```bash
cd tests/LicenseService.UnitTests
dotnet test --verbosity normal
```

Tests cover:
- `ApplyLicenseCommandHandler` — creates license, triggers notification
- `UpdateLicenseStatusCommandHandler` — valid/invalid status, unknown ID
- `GetLicensesByUserQueryHandler` — user isolation, ordering
- `GetAllLicensesAdminQueryHandler` — all licenses, status filter
- `GetAdminDashboardStatsQueryHandler` — correct counts
- `ConsoleNotificationService` — no-throw guarantee

---

## Key Design Decisions

### CQRS via MediatR
All write operations dispatch a `Command`; all reads dispatch a `Query`.
No controller ever accesses `AppDbContext` directly — always through MediatR.

### JWT Flow
```
UI → POST /api/auth/login → Gateway → LicenseService
                                           ↓
                                    BCrypt.Verify()
                                           ↓
                                    JWT generated (HS256)
                                           ↓
UI ← { token, role, username, userId }
```
Token stored in **HttpOnly Secure cookie** on the MVC side.
The `GatewayClient` reads the cookie and attaches `Authorization: Bearer {token}`.

### File Upload Security
1. MIME type whitelist check (PDF, JPEG, PNG, DOCX)
2. Magic bytes validation (server-side, ignores `Content-Type` header)
3. GUID-renamed files (original filename never preserved)
4. 10 MB limit enforced at both controller and middleware level

### Notification Service
Registered via DI as `INotificationService`.
Swap `ConsoleNotificationService` → `SmtpNotificationService` in `Program.cs` for production:
```csharp
// Program.cs (LicenseService)
builder.Services.AddScoped<INotificationService, SmtpNotificationService>();
```

---

## Environment Configuration

All secrets are in `appsettings.json` for development convenience.
**For production**, move these to environment variables or Azure Key Vault:

| Key | Description |
|-----|-------------|
| `Jwt:Secret` | HS256 signing key (min 32 chars) |
| `Jwt:Issuer` | Token issuer |
| `Jwt:Audience` | Token audience |
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |

---

## License

Internal Use Only — Government Technology Division
