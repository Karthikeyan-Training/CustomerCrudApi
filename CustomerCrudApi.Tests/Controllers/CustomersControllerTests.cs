using CustomerCrudApi.Controllers;
using CustomerCrudApi.Models;
using CustomerCrudApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CustomerCrudApi.Tests.Controllers;

public class CustomersControllerTests
{
    private readonly Mock<ICustomerService> _serviceMock = new();

    [Fact]
    public async Task GetAll_ReturnsOkWithCustomers()
    {
        var customers = new PagedResult<Customer>
        {
            Items = new List<Customer>
            {
                new() { Id = Guid.NewGuid(), FirstName = "Alice", LastName = "Jones", Email = "alice@example.com" }
            },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };
        _serviceMock.Setup(s => s.GetAllAsync(1, 10, It.IsAny<CancellationToken>())).ReturnsAsync(customers);
        var controller = new CustomersController(_serviceMock.Object, NullLogger<CustomersController>.Instance);

        var response = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var payload = Assert.IsType<PagedResult<Customer>>(ok.Value);
        Assert.Single(payload.Items);
    }

    [Fact]
    public async Task GetAll_WhenPageNumberIsZero_ReturnsBadRequest()
    {
        var controller = new CustomersController(_serviceMock.Object, NullLogger<CustomersController>.Instance);

        var response = await controller.GetAll(pageNumber: 0, pageSize: 10);

        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    [Fact]
    public async Task GetAll_WhenPageSizeIsNegative_ReturnsBadRequest()
    {
        var controller = new CustomersController(_serviceMock.Object, NullLogger<CustomersController>.Instance);

        var response = await controller.GetAll(pageNumber: 1, pageSize: -1);

        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    [Fact]
    public async Task GetById_WhenCustomerExists_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var customer = new Customer { Id = id, FirstName = "A", LastName = "B", Email = "a@b.com" };
        _serviceMock.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        var controller = new CustomersController(_serviceMock.Object, NullLogger<CustomersController>.Instance);

        var response = await controller.GetById(id);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var payload = Assert.IsType<Customer>(ok.Value);
        Assert.Equal(id, payload.Id);
    }

    [Fact]
    public async Task GetById_WhenCustomerMissing_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);
        var controller = new CustomersController(_serviceMock.Object, NullLogger<CustomersController>.Instance);

        var response = await controller.GetById(id);

        Assert.IsType<NotFoundResult>(response.Result);
    }

    [Fact]
    public async Task Create_WhenConflict_ReturnsConflictWithMessage()
    {
        var request = BuildCreateRequest();
        _serviceMock
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CustomerOperationResult.Conflict("Duplicate email."));
        var controller = new CustomersController(_serviceMock.Object, NullLogger<CustomersController>.Instance);

        var response = await controller.Create(request);

        var conflict = Assert.IsType<ConflictObjectResult>(response.Result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task Create_WhenValidationFailure_ReturnsBadRequest()
    {
        var request = BuildCreateRequest();
        _serviceMock
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CustomerOperationResult.ValidationFailure("DateOfBirth must be in the past."));
        var controller = new CustomersController(_serviceMock.Object, NullLogger<CustomersController>.Instance);

        var response = await controller.Create(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task Create_WhenSuccessful_ReturnsCreatedAtAction()
    {
        var request = BuildCreateRequest();
        var customer = new Customer { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe", Email = "john@example.com" };
        _serviceMock
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CustomerOperationResult.Success(customer));
        var controller = new CustomersController(_serviceMock.Object, NullLogger<CustomersController>.Instance);

        var response = await controller.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(response.Result);
        Assert.Equal(nameof(CustomersController.GetById), created.ActionName);
        var payload = Assert.IsType<Customer>(created.Value);
        Assert.Equal(customer.Id, payload.Id);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFoundObject()
    {
        var id = Guid.NewGuid();
        var request = BuildUpdateRequest();
        _serviceMock
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CustomerOperationResult.NotFound("Customer was not found."));
        var controller = new CustomersController(_serviceMock.Object, NullLogger<CustomersController>.Instance);

        var response = await controller.Update(id, request);

        var notFound = Assert.IsType<NotFoundObjectResult>(response.Result);
        Assert.Equal(404, notFound.StatusCode);
    }

    [Fact]
    public async Task Update_WhenConflict_ReturnsConflictObject()
    {
        var id = Guid.NewGuid();
        var request = BuildUpdateRequest();
        _serviceMock
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CustomerOperationResult.Conflict("Duplicate email."));
        var controller = new CustomersController(_serviceMock.Object, NullLogger<CustomersController>.Instance);

        var response = await controller.Update(id, request);

        var conflict = Assert.IsType<ConflictObjectResult>(response.Result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task Update_WhenValidationFailure_ReturnsBadRequest()
    {
        var id = Guid.NewGuid();
        var request = BuildUpdateRequest();
        _serviceMock
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CustomerOperationResult.ValidationFailure("DateOfBirth must be in the past."));
        var controller = new CustomersController(_serviceMock.Object, NullLogger<CustomersController>.Instance);

        var response = await controller.Update(id, request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsOkWithCustomer()
    {
        var id = Guid.NewGuid();
        var request = BuildUpdateRequest();
        var customer = new Customer { Id = id, FirstName = "Updated", LastName = "User", Email = "updated@example.com" };
        _serviceMock
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CustomerOperationResult.Success(customer));
        var controller = new CustomersController(_serviceMock.Object, NullLogger<CustomersController>.Instance);

        var response = await controller.Update(id, request);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var payload = Assert.IsType<Customer>(ok.Value);
        Assert.Equal(id, payload.Id);
    }

    [Fact]
    public async Task Delete_WhenCustomerMissing_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var controller = new CustomersController(_serviceMock.Object, NullLogger<CustomersController>.Instance);

        var response = await controller.Delete(id);

        Assert.IsType<NotFoundResult>(response);
    }

    [Fact]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var controller = new CustomersController(_serviceMock.Object, NullLogger<CustomersController>.Instance);

        var response = await controller.Delete(id);

        Assert.IsType<NoContentResult>(response);
    }

    private static CreateCustomerRequest BuildCreateRequest() => new()
    {
        FirstName = "John",
        LastName = "Doe",
        Email = "john@example.com",
        Phone = "+91-9999999999",
        DateOfBirth = new DateTime(1990, 1, 1),
        Address = "Some Address"
    };

    private static UpdateCustomerRequest BuildUpdateRequest() => new()
    {
        FirstName = "Jane",
        LastName = "Doe",
        Email = "jane@example.com",
        Phone = "+91-8888888888",
        DateOfBirth = new DateTime(1991, 2, 2),
        Address = "Another Address"
    };
}