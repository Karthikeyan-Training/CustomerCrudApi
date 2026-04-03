using CustomerCrudApi.Controllers;
using CustomerCrudApi.Models;
using CustomerCrudApi.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CustomerCrudApi.Tests.Controllers;

public class CustomersControllerTests
{
    private readonly Mock<ICustomerService> _serviceMock = new();

    [Fact]
    public void GetAll_ReturnsOkWithCustomers()
    {
        var customers = new List<Customer>
        {
            new() { Id = Guid.NewGuid(), FirstName = "Alice", LastName = "Jones", Email = "alice@example.com" }
        };
        _serviceMock.Setup(s => s.GetAll()).Returns(customers);
        var controller = new CustomersController(_serviceMock.Object);

        var response = controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var payload = Assert.IsAssignableFrom<IReadOnlyCollection<Customer>>(ok.Value);
        Assert.Single(payload);
    }

    [Fact]
    public void GetById_WhenCustomerExists_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var customer = new Customer { Id = id, FirstName = "A", LastName = "B", Email = "a@b.com" };
        _serviceMock.Setup(s => s.GetById(id)).Returns(customer);
        var controller = new CustomersController(_serviceMock.Object);

        var response = controller.GetById(id);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var payload = Assert.IsType<Customer>(ok.Value);
        Assert.Equal(id, payload.Id);
    }

    [Fact]
    public void GetById_WhenCustomerMissing_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetById(id)).Returns((Customer?)null);
        var controller = new CustomersController(_serviceMock.Object);

        var response = controller.GetById(id);

        Assert.IsType<NotFoundResult>(response.Result);
    }

    [Fact]
    public void Create_WhenConflict_ReturnsConflictWithMessage()
    {
        var request = BuildCreateRequest();
        _serviceMock
            .Setup(s => s.Create(request))
            .Returns(CustomerOperationResult.Conflict("Duplicate email."));
        var controller = new CustomersController(_serviceMock.Object);

        var response = controller.Create(request);

        var conflict = Assert.IsType<ConflictObjectResult>(response.Result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public void Create_WhenSuccessful_ReturnsCreatedAtAction()
    {
        var request = BuildCreateRequest();
        var customer = new Customer { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe", Email = "john@example.com" };
        _serviceMock
            .Setup(s => s.Create(request))
            .Returns(CustomerOperationResult.Success(customer));
        var controller = new CustomersController(_serviceMock.Object);

        var response = controller.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(response.Result);
        Assert.Equal(nameof(CustomersController.GetById), created.ActionName);
        var payload = Assert.IsType<Customer>(created.Value);
        Assert.Equal(customer.Id, payload.Id);
    }

    [Fact]
    public void Update_WhenNotFound_ReturnsNotFoundObject()
    {
        var id = Guid.NewGuid();
        var request = BuildUpdateRequest();
        _serviceMock
            .Setup(s => s.Update(id, request))
            .Returns(CustomerOperationResult.NotFound("Customer was not found."));
        var controller = new CustomersController(_serviceMock.Object);

        var response = controller.Update(id, request);

        var notFound = Assert.IsType<NotFoundObjectResult>(response.Result);
        Assert.Equal(404, notFound.StatusCode);
    }

    [Fact]
    public void Update_WhenConflict_ReturnsConflictObject()
    {
        var id = Guid.NewGuid();
        var request = BuildUpdateRequest();
        _serviceMock
            .Setup(s => s.Update(id, request))
            .Returns(CustomerOperationResult.Conflict("Duplicate email."));
        var controller = new CustomersController(_serviceMock.Object);

        var response = controller.Update(id, request);

        var conflict = Assert.IsType<ConflictObjectResult>(response.Result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public void Update_WhenSuccessful_ReturnsOkWithCustomer()
    {
        var id = Guid.NewGuid();
        var request = BuildUpdateRequest();
        var customer = new Customer { Id = id, FirstName = "Updated", LastName = "User", Email = "updated@example.com" };
        _serviceMock
            .Setup(s => s.Update(id, request))
            .Returns(CustomerOperationResult.Success(customer));
        var controller = new CustomersController(_serviceMock.Object);

        var response = controller.Update(id, request);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var payload = Assert.IsType<Customer>(ok.Value);
        Assert.Equal(id, payload.Id);
    }

    [Fact]
    public void Delete_WhenCustomerMissing_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.Delete(id)).Returns(false);
        var controller = new CustomersController(_serviceMock.Object);

        var response = controller.Delete(id);

        Assert.IsType<NotFoundResult>(response);
    }

    [Fact]
    public void Delete_WhenSuccessful_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.Delete(id)).Returns(true);
        var controller = new CustomersController(_serviceMock.Object);

        var response = controller.Delete(id);

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