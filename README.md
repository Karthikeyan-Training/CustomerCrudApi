# CustomerCrudApi

Customer CRUD Web API built with ASP.NET Core (.NET 8), now backed by SQLite through Entity Framework Core Code First.

The project keeps the in-memory mock repository implementation for unit testing while using SQLite for runtime persistence.

## Features

- CRUD operations for customers
- SQLite database with EF Core Code First
- Automatic migration apply on startup
- Async end-to-end data flow (controller, service, repository)
- Pagination for customer list endpoint
- Structured logging for key operations and failure paths
- Existing mock/in-memory path preserved for unit tests
- Swagger/OpenAPI support

## Tech Stack

- .NET 8 Web API (controller-based)
- Entity Framework Core 8 (SQLite provider)
- SQLite
- Swagger / OpenAPI
- xUnit + Moq for tests

## Architecture

### Layers

1. Controller layer
- Handles HTTP routes, status codes, and request/response shapes.
- File: Controllers/CustomersController.cs

2. Service layer
- Contains business rules (date validation, duplicate email checks, normalization).
- File: Services/CustomerService.cs

3. Repository layer
- Abstracts data access behind ICustomerRepository.
- Runtime implementation: Repositories/SqliteCustomerRepository.cs
- Test/mock implementation: Repositories/MockCustomerRepository.cs

4. Data layer
- EF Core DbContext and model mapping.
- File: Data/CustomerDbContext.cs

### Dependency Injection Strategy

- Runtime DI registration uses SqliteCustomerRepository.
- Unit tests continue to use mock/test-double repositories and do not depend on SQLite.

## Project Structure

- Program.cs
- appsettings.json
- appsettings.Development.json
- Controllers/CustomersController.cs
- Models/Customer.cs
- Models/CreateCustomerRequest.cs
- Models/UpdateCustomerRequest.cs
- Models/PagedResult.cs
- Repositories/ICustomerRepository.cs
- Repositories/SqliteCustomerRepository.cs
- Repositories/MockCustomerRepository.cs
- Services/ICustomerService.cs
- Services/CustomerService.cs
- Services/CustomerOperationResult.cs
- Data/CustomerDbContext.cs
- Data/Migrations/
- CustomerCrudApi.Tests/

## Configuration

### Connection Strings

appsettings.json

```json
"ConnectionStrings": {
	"DefaultConnection": "Data Source=customers.db"
}
```

appsettings.Development.json

```json
"ConnectionStrings": {
	"DefaultConnection": "Data Source=customers.development.db"
}
```

### Logging

Default log levels are configured in appsettings files, including EF Core-specific logging level overrides.

## Database and Migrations (EF Core Code First)

### Prerequisites

1. Install .NET 8 SDK
2. Install EF CLI tool (one-time)

```powershell
dotnet tool install --global dotnet-ef
```

### Create Migration

```powershell
dotnet ef migrations add InitialSqlite --output-dir Data/Migrations
```

### Update Database

```powershell
dotnet ef database update
```

### Runtime Behavior

Program.cs applies pending migrations at startup via Database.Migrate().

## Run the API

```powershell
dotnet restore
dotnet build
dotnet run
```

Swagger endpoints (local):

- https://localhost:7194/swagger
- http://localhost:5194/swagger

## API Endpoints

- GET /api/customers?pageNumber=1&pageSize=10
- GET /api/customers/{id}
- POST /api/customers
- PUT /api/customers/{id}
- DELETE /api/customers/{id}

## Pagination

GET /api/customers returns a paged result object:

```json
{
	"items": [
		{
			"id": "2f5e0f6f-6c4e-4c6d-8d5f-7a52b10671c2",
			"firstName": "John",
			"lastName": "Doe",
			"email": "john.doe@example.com",
			"phone": "+91-9876543210",
			"dateOfBirth": "1995-06-15T00:00:00Z",
			"address": "42 Main Street, Bengaluru",
			"createdAtUtc": "2026-04-10T10:15:00Z",
			"updatedAtUtc": "2026-04-10T10:15:00Z"
		}
	],
	"totalCount": 1,
	"pageNumber": 1,
	"pageSize": 10,
	"totalPages": 1
}
```

Pagination rules:

- pageNumber must be greater than 0
- pageSize must be greater than 0
- service clamps pageSize to max 100

## Request Payloads

POST / PUT sample:

```json
{
	"firstName": "John",
	"lastName": "Doe",
	"email": "john.doe@example.com",
	"phone": "+91-9876543210",
	"dateOfBirth": "1995-06-15T00:00:00Z",
	"address": "42 Main Street, Bengaluru"
}
```

## Validation and Business Rules

- FirstName, LastName, Email, Phone, Address are required
- Email must be valid format
- DateOfBirth must not be in the future
- Email must be unique (service check + DB unique index)

## Status Codes

- 200 OK: Successful GET/PUT
- 201 Created: Successful POST
- 204 No Content: Successful DELETE
- 400 Bad Request: Model validation or invalid paging values
- 404 Not Found: Customer does not exist
- 409 Conflict: Duplicate email or DateOfBirth business-rule conflict

## Testing Strategy

### Unit Tests (No Real Database)

- Repository mock tests target MockCustomerRepository directly.
- Service tests use in-memory test doubles for ICustomerRepository.
- Controller tests mock ICustomerService.

Run tests:

```powershell
dotnet test
```

### Integration with SQLite (Runtime)

- Runtime API uses SqliteCustomerRepository and CustomerDbContext.
- Database schema comes from EF migrations.

## Operational Notes

- SQLite DB files are created in the project root by default:
	- customers.db (default)
	- customers.development.db (development environment)
- Restarting API does not lose data because persistence is database-backed.

## Troubleshooting

1. dotnet ef command not found
- Install or update EF CLI tool:

```powershell
dotnet tool install --global dotnet-ef
```

2. Migration conflicts or model mismatch
- Verify latest migration exists in Data/Migrations.
- Run dotnet build before migration commands.

3. Duplicate email errors
- Expected behavior: unique index on Email plus service-level conflict handling.

## Future Enhancements

- Add dedicated integration test project with SQLite test database lifecycle
- Add filtering and sorting options to list endpoint
- Add API versioning
