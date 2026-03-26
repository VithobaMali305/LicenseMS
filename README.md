
## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 8.0+ |
| SQL Server | 2019/2022 or LocalDB |
| Visual Studio | 2022 |

---
 
## Quick Start

### 1. Clone / Extract
https://github.com/VithobaMali305/LicenseMS.git

### 2. Configure Connection String
Edit `src/LicenseService/appsettings.json`:
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=LicenseManagementDB;Trusted_Connection=True"
}
> For full SQL Server: `Server=.;Database=LicenseManagementDB;Trusted_Connection=True;TrustServerCertificate=True`

### 3. Apply Database Migrations
dotnet ef migrations add InitialCreate --project src/LicenseService
dotnet ef database update --project src/LicenseService

### 4. Run All Services

### 5. Open the App

Navigate to: **http://localhost:5003**

## Default Credentials

| Role | Username | Password |
|------|----------|----------|
| Admin | `admin` | `Admin@123` |
| User | *(register a new account)* | — |
