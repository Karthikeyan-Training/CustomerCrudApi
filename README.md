# CustomerCrudApi

ASP.NET Core Web API for Customer CRUD operations using in-project mock data.

## Tech Stack

- .NET 8 Web API (controller-based)
- Swagger / OpenAPI
- In-memory repository seeded with 10 customers

## Project Structure

- Controllers/CustomersController.cs
- Models/Customer.cs
- Models/CreateCustomerRequest.cs
- Models/UpdateCustomerRequest.cs
- Repositories/ICustomerRepository.cs
- Repositories/MockCustomerRepository.cs
- Services/ICustomerService.cs
- Services/CustomerService.cs

## Run the API

1. Install .NET 8 SDK.
2. From project root, run:

```powershell
dotnet restore
dotnet build
dotnet run
```

3. Open Swagger UI:

- https://localhost:7194/swagger
- http://localhost:5194/swagger

## API Endpoints

- GET /api/customers
- GET /api/customers/{id}
- POST /api/customers
- PUT /api/customers/{id}
- DELETE /api/customers/{id}

## Sample Request Body (POST / PUT)

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

## Response and Validation Behavior

- 200 OK: Successful GET and PUT
- 201 Created: Successful POST with Location header
- 204 No Content: Successful DELETE
- 400 Bad Request: Invalid request model
- 404 Not Found: Customer id does not exist
- 409 Conflict: Duplicate email or invalid future DateOfBirth

## Mock Data Behavior

- Repository starts with 10 seeded customers.
- Data is stored in memory only.
- Restarting the app resets data back to the seeded set.
