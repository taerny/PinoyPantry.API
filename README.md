# PinoyPantry API

A .NET 8 Web API backend for the PinoyPantry online store — an authentic Filipino grocery e-commerce platform.

## Tech Stack

- **.NET 8** — Web API
- **Entity Framework Core** — ORM
- **SQL Server Express** — Database
- **AutoMapper** — DTO mapping
- **FluentValidation** — Request validation
- **Swagger / Swashbuckle** — API documentation

## Architecture

```
Controllers → Services → Repositories → EF Core → SQL Server
```

- **Controllers** — HTTP endpoints, accepts/returns DTOs
- **Services** — Business logic layer
- **Repositories** — Data access layer
- **DTOs** — Separate request/response models from database models
- **Middleware** — Global exception handling

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/products` | Get all products (paginated, filterable) |
| GET | `/api/products/{id}` | Get product by ID |
| POST | `/api/products` | Create a product |
| PUT | `/api/products/{id}` | Update a product |
| DELETE | `/api/products/{id}` | Delete a product |

### Query Parameters (GET /api/products)

| Param | Type | Example |
|-------|------|---------|
| page | int | `?page=1` |
| limit | int | `?limit=12` |
| category | string | `?category=Condiments` |
| search | string | `?search=soy sauce` |

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server Express (2012 or later)

### Setup

1. Clone the repo
2. Update the connection string in `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=PinoyPantryDb;Trusted_Connection=True;TrustServerCertificate=True;"
   }
   ```
3. Run migrations:
   ```
   dotnet ef database update
   ```
4. Run the API:
   ```
   dotnet run
   ```
5. Open Swagger UI at `https://localhost:7136/swagger`

## Related

- Frontend: [PinoyPantry.Client](https://github.com/taerny/PinoyPantry.Client) — React + Vite + TypeScript
