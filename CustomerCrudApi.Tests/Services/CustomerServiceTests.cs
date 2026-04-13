using CustomerCrudApi.Models;
using CustomerCrudApi.Repositories;
using CustomerCrudApi.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CustomerCrudApi.Tests.Services;

public class CustomerServiceTests
{
    [Fact]
    public async Task GetAll_DelegatesToRepository()
    {
        var repo = new TestCustomerRepository();
        await repo.AddAsync(new Customer { Id = Guid.NewGuid(), FirstName = "A", LastName = "B", Email = "a@b.com" });
        var service = new CustomerService(repo, NullLogger<CustomerService>.Instance, TimeProvider.System);

        var result = await service.GetAllAsync(1, 10);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetById_WhenPresent_ReturnsCustomer()
    {
        var repo = new TestCustomerRepository();
        var id = Guid.NewGuid();
        await repo.AddAsync(new Customer { Id = id, FirstName = "A", LastName = "B", Email = "a@b.com" });
        var service = new CustomerService(repo, NullLogger<CustomerService>.Instance, TimeProvider.System);

        var customer = await service.GetByIdAsync(id);

        Assert.NotNull(customer);
        Assert.Equal(id, customer!.Id);
    }

    [Fact]
    public async Task Create_WhenDateOfBirthIsFuture_ReturnsValidationFailure()
    {
        var repo = new TestCustomerRepository();
        var service = new CustomerService(repo, NullLogger<CustomerService>.Instance, TimeProvider.System);
        var request = BuildCreateRequest();
        request.DateOfBirth = DateTime.UtcNow.Date.AddDays(1);

        var result = await service.CreateAsync(request);

        Assert.True(result.IsValidationFailure);
        Assert.Equal("DateOfBirth must be in the past.", result.ErrorMessage);
    }

    [Fact]
    public async Task Create_WhenEmailExists_ReturnsConflict()
    {
        var repo = new TestCustomerRepository();
        await repo.AddAsync(new Customer { Id = Guid.NewGuid(), FirstName = "X", LastName = "Y", Email = "john@example.com" });
        var service = new CustomerService(repo, NullLogger<CustomerService>.Instance, TimeProvider.System);

        var result = await service.CreateAsync(BuildCreateRequest());

        Assert.True(result.IsConflict);
        Assert.Equal("A customer with the same email already exists.", result.ErrorMessage);
    }

    [Fact]
    public async Task Create_WhenEmailExistsWithWhitespace_ReturnsConflict()
    {
        var repo = new TestCustomerRepository();
        await repo.AddAsync(new Customer { Id = Guid.NewGuid(), FirstName = "X", LastName = "Y", Email = "john@example.com" });
        var service = new CustomerService(repo, NullLogger<CustomerService>.Instance, TimeProvider.System);
        var request = BuildCreateRequest();
        request.Email = "  JOHN@example.com  ";

        var result = await service.CreateAsync(request);

        Assert.True(result.IsConflict);
        Assert.Equal("A customer with the same email already exists.", result.ErrorMessage);
    }

    [Fact]
    public async Task Create_WhenValid_AddsCustomerWithTrimmedValues()
    {
        var repo = new TestCustomerRepository();
        var service = new CustomerService(repo, NullLogger<CustomerService>.Instance, TimeProvider.System);
        var request = new CreateCustomerRequest
        {
            FirstName = "  John  ",
            LastName = "  Doe  ",
            Email = "  john@example.com  ",
            Phone = "  +91-1234567890  ",
            DateOfBirth = new DateTime(1993, 3, 3),
            Address = "  Some Street  "
        };

        var result = await service.CreateAsync(request);

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
    public async Task Update_WhenCustomerMissing_ReturnsNotFound()
    {
        var repo = new TestCustomerRepository();
        var service = new CustomerService(repo, NullLogger<CustomerService>.Instance, TimeProvider.System);

        var result = await service.UpdateAsync(Guid.NewGuid(), BuildUpdateRequest());

        Assert.True(result.IsNotFound);
        Assert.Equal("Customer was not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task Update_WhenDateOfBirthIsFuture_ReturnsValidationFailure()
    {
        var repo = new TestCustomerRepository();
        var id = Guid.NewGuid();
        await repo.AddAsync(new Customer { Id = id, FirstName = "A", LastName = "B", Email = "a@b.com" });
        var service = new CustomerService(repo, NullLogger<CustomerService>.Instance, TimeProvider.System);
        var request = BuildUpdateRequest();
        request.DateOfBirth = DateTime.UtcNow.Date.AddDays(1);

        var result = await service.UpdateAsync(id, request);

        Assert.True(result.IsValidationFailure);
        Assert.Equal("DateOfBirth must be in the past.", result.ErrorMessage);
    }

    [Fact]
    public async Task Update_WhenDuplicateEmailExists_ReturnsConflict()
    {
        var repo = new TestCustomerRepository();
        var id = Guid.NewGuid();
        await repo.AddAsync(new Customer { Id = id, FirstName = "A", LastName = "B", Email = "a@b.com" });
        await repo.AddAsync(new Customer { Id = Guid.NewGuid(), FirstName = "X", LastName = "Y", Email = "duplicate@example.com" });
        var service = new CustomerService(repo, NullLogger<CustomerService>.Instance, TimeProvider.System);
        var request = BuildUpdateRequest();
        request.Email = "duplicate@example.com";

        var result = await service.UpdateAsync(id, request);

        Assert.True(result.IsConflict);
        Assert.Equal("A customer with the same email already exists.", result.ErrorMessage);
    }

    [Fact]
    public async Task Update_WhenDuplicateEmailExistsWithWhitespace_ReturnsConflict()
    {
        var repo = new TestCustomerRepository();
        var id = Guid.NewGuid();
        await repo.AddAsync(new Customer { Id = id, FirstName = "A", LastName = "B", Email = "a@b.com" });
        await repo.AddAsync(new Customer { Id = Guid.NewGuid(), FirstName = "X", LastName = "Y", Email = "duplicate@example.com" });
        var service = new CustomerService(repo, NullLogger<CustomerService>.Instance, TimeProvider.System);
        var request = BuildUpdateRequest();
        request.Email = "  DuPlicate@example.com  ";

        var result = await service.UpdateAsync(id, request);

        Assert.True(result.IsConflict);
        Assert.Equal("A customer with the same email already exists.", result.ErrorMessage);
    }

    [Fact]
    public async Task Update_WhenRepositoryUpdateReturnsNull_ReturnsNotFound()
    {
        var repo = new TestCustomerRepository { ReturnNullOnUpdate = true };
        var id = Guid.NewGuid();
        await repo.AddAsync(new Customer { Id = id, FirstName = "A", LastName = "B", Email = "a@b.com" });
        var service = new CustomerService(repo, NullLogger<CustomerService>.Instance, TimeProvider.System);

        var result = await service.UpdateAsync(id, BuildUpdateRequest());

        Assert.True(result.IsNotFound);
        Assert.Equal("Customer was not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task Update_WhenValid_UpdatesCustomerAndReturnsSuccess()
    {
        var repo = new TestCustomerRepository();
        var id = Guid.NewGuid();
        await repo.AddAsync(new Customer
        {
            Id = id,
            FirstName = "Old",
            LastName = "Name",
            Email = "old@example.com",
            Phone = "111",
            DateOfBirth = new DateTime(1990, 1, 1),
            Address = "Old Address"
        });

        var service = new CustomerService(repo, NullLogger<CustomerService>.Instance, TimeProvider.System);
        var request = new UpdateCustomerRequest
        {
            FirstName = "  New  ",
            LastName = "  User  ",
            Email = "  new@example.com  ",
            Phone = "  222  ",
            DateOfBirth = new DateTime(1991, 2, 2),
            Address = "  New Address  "
        };

        var result = await service.UpdateAsync(id, request);

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
    public async Task Delete_DelegatesToRepository()
    {
        var repo = new TestCustomerRepository();
        var id = Guid.NewGuid();
        await repo.AddAsync(new Customer { Id = id, FirstName = "A", LastName = "B", Email = "a@b.com" });
        var service = new CustomerService(repo, NullLogger<CustomerService>.Instance, TimeProvider.System);

        var deleted = await service.DeleteAsync(id);

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

        public Task<IReadOnlyCollection<Customer>> GetAllAsync(int skip, int take, CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Customer> customers = _store.Values
                .OrderBy(c => c.FirstName)
                .ThenBy(c => c.LastName)
                .Skip(skip)
                .Take(take)
                .ToList();

            return Task.FromResult(customers);
        }

        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_store.Count);

        public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.TryGetValue(id, out var customer) ? customer : null);

        public Task<Customer> AddAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            _store[customer.Id] = customer;
            return Task.FromResult(customer);
        }

        public Task<Customer?> UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            if (ReturnNullOnUpdate)
            {
                return Task.FromResult<Customer?>(null);
            }

            if (!_store.ContainsKey(customer.Id))
            {
                return Task.FromResult<Customer?>(null);
            }

            _store[customer.Id] = customer;
            return Task.FromResult<Customer?>(customer);
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.Remove(id));

        public Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            var exists = _store.Values.Any(c =>
                (!excludeId.HasValue || c.Id != excludeId.Value) &&
                string.Equals(c.Email, email, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(exists);
        }
    }
}