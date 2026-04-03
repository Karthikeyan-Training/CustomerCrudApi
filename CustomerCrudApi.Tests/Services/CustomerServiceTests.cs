using CustomerCrudApi.Models;
using CustomerCrudApi.Repositories;
using CustomerCrudApi.Services;

namespace CustomerCrudApi.Tests.Services;

public class CustomerServiceTests
{
    [Fact]
    public void GetAll_DelegatesToRepository()
    {
        var repo = new TestCustomerRepository();
        repo.Add(new Customer { Id = Guid.NewGuid(), FirstName = "A", LastName = "B", Email = "a@b.com" });
        var service = new CustomerService(repo);

        var result = service.GetAll();

        Assert.Single(result);
    }

    [Fact]
    public void GetById_WhenPresent_ReturnsCustomer()
    {
        var repo = new TestCustomerRepository();
        var id = Guid.NewGuid();
        repo.Add(new Customer { Id = id, FirstName = "A", LastName = "B", Email = "a@b.com" });
        var service = new CustomerService(repo);

        var customer = service.GetById(id);

        Assert.NotNull(customer);
        Assert.Equal(id, customer!.Id);
    }

    [Fact]
    public void Create_WhenDateOfBirthIsFuture_ReturnsConflict()
    {
        var repo = new TestCustomerRepository();
        var service = new CustomerService(repo);
        var request = BuildCreateRequest();
        request.DateOfBirth = DateTime.UtcNow.Date.AddDays(1);

        var result = service.Create(request);

        Assert.True(result.IsConflict);
        Assert.Equal("DateOfBirth must be in the past.", result.ErrorMessage);
    }

    [Fact]
    public void Create_WhenEmailExists_ReturnsConflict()
    {
        var repo = new TestCustomerRepository();
        repo.Add(new Customer { Id = Guid.NewGuid(), FirstName = "X", LastName = "Y", Email = "john@example.com" });
        var service = new CustomerService(repo);

        var result = service.Create(BuildCreateRequest());

        Assert.True(result.IsConflict);
        Assert.Equal("A customer with the same email already exists.", result.ErrorMessage);
    }

    [Fact]
    public void Create_WhenValid_AddsCustomerWithTrimmedValues()
    {
        var repo = new TestCustomerRepository();
        var service = new CustomerService(repo);
        var request = new CreateCustomerRequest
        {
            FirstName = "  John  ",
            LastName = "  Doe  ",
            Email = "  john@example.com  ",
            Phone = "  +91-1234567890  ",
            DateOfBirth = new DateTime(1993, 3, 3),
            Address = "  Some Street  "
        };

        var result = service.Create(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Customer);
        Assert.Equal("John", result.Customer!.FirstName);
        Assert.Equal("Doe", result.Customer.LastName);
        Assert.Equal("john@example.com", result.Customer.Email);
        Assert.Equal("+91-1234567890", result.Customer.Phone);
        Assert.Equal("Some Street", result.Customer.Address);
        Assert.Equal(DateTimeKind.Utc, result.Customer.DateOfBirth.Kind);
        Assert.Equal(1, repo.Count);
    }

    [Fact]
    public void Update_WhenCustomerMissing_ReturnsNotFound()
    {
        var repo = new TestCustomerRepository();
        var service = new CustomerService(repo);

        var result = service.Update(Guid.NewGuid(), BuildUpdateRequest());

        Assert.True(result.IsNotFound);
        Assert.Equal("Customer was not found.", result.ErrorMessage);
    }

    [Fact]
    public void Update_WhenDateOfBirthIsFuture_ReturnsConflict()
    {
        var repo = new TestCustomerRepository();
        var id = Guid.NewGuid();
        repo.Add(new Customer { Id = id, FirstName = "A", LastName = "B", Email = "a@b.com" });
        var service = new CustomerService(repo);
        var request = BuildUpdateRequest();
        request.DateOfBirth = DateTime.UtcNow.Date.AddDays(1);

        var result = service.Update(id, request);

        Assert.True(result.IsConflict);
        Assert.Equal("DateOfBirth must be in the past.", result.ErrorMessage);
    }

    [Fact]
    public void Update_WhenDuplicateEmailExists_ReturnsConflict()
    {
        var repo = new TestCustomerRepository();
        var id = Guid.NewGuid();
        repo.Add(new Customer { Id = id, FirstName = "A", LastName = "B", Email = "a@b.com" });
        repo.Add(new Customer { Id = Guid.NewGuid(), FirstName = "X", LastName = "Y", Email = "duplicate@example.com" });
        var service = new CustomerService(repo);
        var request = BuildUpdateRequest();
        request.Email = "duplicate@example.com";

        var result = service.Update(id, request);

        Assert.True(result.IsConflict);
        Assert.Equal("A customer with the same email already exists.", result.ErrorMessage);
    }

    [Fact]
    public void Update_WhenRepositoryUpdateReturnsNull_ReturnsNotFound()
    {
        var repo = new TestCustomerRepository { ReturnNullOnUpdate = true };
        var id = Guid.NewGuid();
        repo.Add(new Customer { Id = id, FirstName = "A", LastName = "B", Email = "a@b.com" });
        var service = new CustomerService(repo);

        var result = service.Update(id, BuildUpdateRequest());

        Assert.True(result.IsNotFound);
        Assert.Equal("Customer was not found.", result.ErrorMessage);
    }

    [Fact]
    public void Update_WhenValid_UpdatesCustomerAndReturnsSuccess()
    {
        var repo = new TestCustomerRepository();
        var id = Guid.NewGuid();
        repo.Add(new Customer
        {
            Id = id,
            FirstName = "Old",
            LastName = "Name",
            Email = "old@example.com",
            Phone = "111",
            DateOfBirth = new DateTime(1990, 1, 1),
            Address = "Old Address"
        });

        var service = new CustomerService(repo);
        var request = new UpdateCustomerRequest
        {
            FirstName = "  New  ",
            LastName = "  User  ",
            Email = "  new@example.com  ",
            Phone = "  222  ",
            DateOfBirth = new DateTime(1991, 2, 2),
            Address = "  New Address  "
        };

        var result = service.Update(id, request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Customer);
        Assert.Equal("New", result.Customer!.FirstName);
        Assert.Equal("User", result.Customer.LastName);
        Assert.Equal("new@example.com", result.Customer.Email);
        Assert.Equal("222", result.Customer.Phone);
        Assert.Equal("New Address", result.Customer.Address);
        Assert.Equal(DateTimeKind.Utc, result.Customer.DateOfBirth.Kind);
    }

    [Fact]
    public void Delete_DelegatesToRepository()
    {
        var repo = new TestCustomerRepository();
        var id = Guid.NewGuid();
        repo.Add(new Customer { Id = id, FirstName = "A", LastName = "B", Email = "a@b.com" });
        var service = new CustomerService(repo);

        var deleted = service.Delete(id);

        Assert.True(deleted);
        Assert.Equal(0, repo.Count);
    }

    private static CreateCustomerRequest BuildCreateRequest() => new()
    {
        FirstName = "John",
        LastName = "Doe",
        Email = "john@example.com",
        Phone = "+91-9999999999",
        DateOfBirth = new DateTime(1990, 1, 1),
        Address = "Address"
    };

    private static UpdateCustomerRequest BuildUpdateRequest() => new()
    {
        FirstName = "Jane",
        LastName = "Doe",
        Email = "jane@example.com",
        Phone = "+91-8888888888",
        DateOfBirth = new DateTime(1992, 2, 2),
        Address = "Address"
    };

    private sealed class TestCustomerRepository : ICustomerRepository
    {
        private readonly Dictionary<Guid, Customer> _store = new();

        public bool ReturnNullOnUpdate { get; set; }

        public int Count => _store.Count;

        public IReadOnlyCollection<Customer> GetAll() => _store.Values.ToList();

        public Customer? GetById(Guid id) => _store.TryGetValue(id, out var customer) ? customer : null;

        public Customer Add(Customer customer)
        {
            _store[customer.Id] = customer;
            return customer;
        }

        public Customer? Update(Customer customer)
        {
            if (ReturnNullOnUpdate)
            {
                return null;
            }

            if (!_store.ContainsKey(customer.Id))
            {
                return null;
            }

            _store[customer.Id] = customer;
            return customer;
        }

        public bool Delete(Guid id) => _store.Remove(id);

        public bool EmailExists(string email, Guid? excludeId = null)
        {
            return _store.Values.Any(c =>
                (!excludeId.HasValue || c.Id != excludeId.Value) &&
                string.Equals(c.Email, email, StringComparison.OrdinalIgnoreCase));
        }
    }
}