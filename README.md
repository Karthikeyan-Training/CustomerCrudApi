# CustomerCrudApi

Customer CRUD Web API built with ASP.NET Core and .NET 8.
The application uses Entity Framework Core with SQLite for runtime persistence and keeps separate test-friendly repository implementations for unit testing.

## Features
- Customer CRUD endpoints using a controller-based Web API
- SQLite persistence through EF Core Code First migrations
- Automatic migration application on startup
- Paged customer listing
- Request normalization for trimmed string values and normalized email addresses
- Business validation for future dates of birth and duplicate emails
- RFC 7807-style error payloads via `ProblemDetails`
- Structured logging to console, debug output, and a SQLite-backed log sink
- Swagger/OpenAPI in Development
- Unit and repository tests, including SQLite-backed repository tests

## Tech Stack
- .NET 8
- ASP.NET Core Web API
- Entity Framework Core 8
- SQLite
- Swashbuckle / Swagger
- xUnit
- Moq

## Architecture
### Layers

1. Controller layer
	Handles routes, HTTP status codes, and API responses.

2. Service layer
	Applies business rules such as pagination normalization, duplicate email checks, request normalization, and validation of customer data.

3. Repository layer
	Encapsulates data access behind `ICustomerRepository`.

4. Data layer
	Configures EF Core mappings and SQLite-specific value conversions in `CustomerDbContext`.

### Runtime Implementations
- API entry point: `Program.cs`
- Controller: `Controllers/CustomersController.cs`
- Service: `Services/CustomerService.cs`
- Repository: `Repositories/SqliteCustomerRepository.cs`
- DbContext: `Data/CustomerDbContext.cs`
- SQLite logger sink: `Logging/SqliteLoggerProvider.cs`

### Test Implementations
- Mock repository: `Repositories/MockCustomerRepository.cs`
- Service test repository double: `CustomerCrudApi.Tests/Services/CustomerServiceTests.cs`

## Project Structure
- `Program.cs`
- `appsettings.json`
- `appsettings.Development.json`
- `Controllers/`
- `Models/`
- `Repositories/`
- `Services/`
- `Data/`
- `Logging/`
- `CustomerCrudApi.Tests/`

## Configuration
### Connection Strings

Production/default settings in `appsettings.json`:
```json
{
	"ConnectionStrings": {
		"DefaultConnection": "Data Source=customers.db"
	}
}
```

Development settings in `appsettings.Development.json`:
```json
{
	"ConnectionStrings": {
		"DefaultConnection": "Data Source=customers.development.db"
	}
}
```

Relative SQLite database paths are resolved against the application content root at startup.

### Logging Configuration
`appsettings.json` configures the default log levels:

```json
{
	"Logging": {
		"LogLevel": {
			"Default": "Information",
			"Microsoft.EntityFrameworkCore": "Warning",
			"Microsoft.AspNetCore": "Warning"
		}
	}
}
```

Development increases verbosity for EF Core:

```json
{
	"Logging": {
		"LogLevel": {
			"Default": "Debug",
			"Microsoft.EntityFrameworkCore": "Information",
			"Microsoft.AspNetCore": "Warning"
		}
	}
}
```

### Runtime Middleware and Services

The API currently enables the following runtime behavior:

- `ProblemDetails` support for standardized error responses
- CORS with the default policy allowing any origin, header, and method
- HTTPS redirection
- HSTS outside Development
- Automatic EF Core migration execution on startup
- Swagger UI in Development only
- DbContext pooling via `AddDbContextPool`
- `TimeProvider.System` registered for time-dependent service logic

### Current Security Posture

The API does not currently implement authentication or authorization. All endpoints are publicly accessible in the current codebase.

## Database and Migrations

### Prerequisites

1. Install the .NET 8 SDK
2. Install the EF Core CLI once if it is not already available

```powershell
dotnet tool install --global dotnet-ef
```

### Create a Migration

```powershell
dotnet ef migrations add YourMigrationName --output-dir Data/Migrations
```

### Apply Migrations Manually

```powershell
dotnet ef database update
```

### Startup Behavior

At runtime, the application applies pending migrations automatically using `Database.Migrate()`.

### SQLite Notes

- Customer data is stored in the configured SQLite database file
- Logs are also written to a `Logs` table in the same SQLite database
- The customer email column has a unique index
- `Guid` values are stored as lowercase text for SQLite compatibility
- `DateTime` values are normalized to UTC in the EF model configuration

## Running the API

```powershell
dotnet restore
dotnet build
dotnet run
```

Local URLs from launch settings:

- `https://localhost:7194`
- `http://localhost:5194`

Swagger UI is available in Development at:

- `https://localhost:7194/swagger`
- `http://localhost:5194/swagger`

## API Endpoints

Base route: `/api/customers`

### GET /api/customers

Returns a paged collection of customers.

Query parameters:

- `pageNumber` default: `1`
- `pageSize` default: `10`

Rules:

- `pageNumber` must be greater than `0`
- `pageSize` must be greater than `0`
- `pageSize` values above `100` are capped to `100` by the service layer

Success response:

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

### GET /api/customers/{id}

Returns a single customer by ID.

Responses:

- `200 OK`
- `404 Not Found`

### POST /api/customers

Creates a customer.

Request body:

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

Responses:

- `201 Created`
- `400 Bad Request` for validation failures such as a future `dateOfBirth`
- `409 Conflict` when a customer with the same email already exists

### PUT /api/customers/{id}

Replaces the customer data for an existing customer.

Request body uses the same shape as `POST /api/customers`.

Responses:

- `200 OK`
- `400 Bad Request` for validation failures such as a future `dateOfBirth`
- `404 Not Found` if the customer does not exist
- `409 Conflict` when a different customer already uses the same email

### DELETE /api/customers/{id}

Deletes an existing customer.

Responses:

- `204 No Content`
- `404 Not Found`

## Validation and Business Rules

- `FirstName`, `LastName`, `Email`, `Phone`, `DateOfBirth`, and `Address` are required
- `Email` must be a valid email address
- `FirstName` and `LastName` have a maximum length of `100`
- `Email` has a maximum length of `200`
- `Phone` has a maximum length of `30`
- `Address` has a maximum length of `300`
- `DateOfBirth` must not be in the future
- Email addresses are trimmed and normalized to lowercase before persistence checks
- Duplicate emails are prevented by both service checks and a database unique index

## Error Response Format

Validation and conflict responses use `ProblemDetails`.

Example:

```json
{
	"type": "about:blank",
	"title": "Validation error",
	"status": 400,
	"detail": "DateOfBirth must be in the past."
}
```

Note:

- `GET /api/customers/{id}` and `DELETE /api/customers/{id}` currently return an empty `404 Not Found` body
- `POST` and `PUT` return `ProblemDetails` payloads for `400`, `404`, and `409` cases they handle explicitly

## Status Codes

- `200 OK` for successful reads and updates
- `201 Created` for successful creates
- `204 No Content` for successful deletes
- `400 Bad Request` for invalid paging values or validation failures
- `404 Not Found` when a requested customer does not exist
- `409 Conflict` for duplicate email violations

## Logging

The application logs to:

- Console
- Debug output
- SQLite via `SqliteLoggerProvider`

The SQLite sink:

- Ensures the `Logs` table exists on startup
- Queues log events in a bounded in-memory channel
- Writes logs to SQLite in batches
- Stores timestamp, level, category, message, and exception text

## Testing

The solution currently includes:

- Controller unit tests
- Service unit tests
- Request model validation tests
- Mock repository tests
- SQLite repository tests using an in-memory SQLite connection

Run all tests with:

```powershell
dotnet test
```

## Operational Notes

- Default SQLite files are created in the project root
- Development uses `customers.development.db`
- Non-development uses `customers.db`
- Data persists across application restarts because the runtime repository is database-backed
- Swagger is only enabled in Development

## Troubleshooting

### `dotnet ef` command not found

```powershell
dotnet tool install --global dotnet-ef
```

### Migration or model mismatch errors

- Build the project first
- Verify the latest migration exists under `Data/Migrations`
- Run `dotnet ef database update`

### Duplicate email errors

This is expected when attempting to create or update a customer with an email address already used by another customer.

### Swagger not available

Swagger is only enabled when `ASPNETCORE_ENVIRONMENT=Development`.

## CI Pipeline

The repository includes a GitHub Actions workflow at `.github/workflows/ci.yml`.

The workflow builds the solution and runs the automated tests on pushes and pull requests.

## Known Gaps

The current codebase does not yet include:

- Authentication or authorization
- API versioning
- Rate limiting
- Dedicated end-to-end integration tests through `WebApplicationFactory`
- A separate production log store outside the application SQLite database
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
## CI Pipeline (GitHub Actions)

This project includes a CI pipeline powered by **GitHub Actions**.

- Tests run automatically on every pull request targeting `main` and on every push to `main`.
- The workflow builds the solution and runs all xUnit tests.
- **Merging is blocked if any test fails** (requires branch protection to be enabled by a repo admin).

### Enabling Branch Protection

To enforce the "tests must pass before merge" rule, a repository admin must:

1. Go to **Settings → Branches** in the GitHub repository.
2. Add a branch protection rule for `main`.
3. Enable **"Require status checks to pass before merging"**.
4. Select the **`build-and-test`** status check.

Once enabled, GitHub will prevent merging any PR where the `build-and-test` check has not passed.
