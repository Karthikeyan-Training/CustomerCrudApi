using CustomerCrudApi.Data;
using CustomerCrudApi.Models;
using CustomerCrudApi.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CustomerCrudApi.Tests.Repositories;

public class SqliteCustomerRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<CustomerDbContext> _dbOptions;

    public SqliteCustomerRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var dbContext = new CustomerDbContext(_dbOptions);
        dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task AddAndGetById_ReturnsCustomer()
    {
        using var dbContext = new CustomerDbContext(_dbOptions);
        var repository = new SqliteCustomerRepository(dbContext);
        var customer = BuildCustomer("add@get.com");

        await repository.AddAsync(customer);
        var fetched = await repository.GetByIdAsync(customer.Id);

        Assert.NotNull(fetched);
        Assert.Equal(customer.Email, fetched!.Email);
    }

    [Fact]
    public async Task Update_PersistsChanges()
    {
        using var dbContext = new CustomerDbContext(_dbOptions);
        var repository = new SqliteCustomerRepository(dbContext);
        var customer = BuildCustomer("update@get.com");
        await repository.AddAsync(customer);

        customer.FirstName = "Updated";
        customer.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await repository.UpdateAsync(customer);

        Assert.NotNull(updated);
        Assert.Equal("Updated", updated!.FirstName);
    }

    [Fact]
    public async Task Delete_RemovesCustomer()
    {
        using var dbContext = new CustomerDbContext(_dbOptions);
        var repository = new SqliteCustomerRepository(dbContext);
        var customer = BuildCustomer("delete@get.com");
        await repository.AddAsync(customer);

        var deleted = await repository.DeleteAsync(customer.Id);
        var fetched = await repository.GetByIdAsync(customer.Id);

        Assert.True(deleted);
        Assert.Null(fetched);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedItems()
    {
        using var dbContext = new CustomerDbContext(_dbOptions);
        var repository = new SqliteCustomerRepository(dbContext);

        await repository.AddAsync(BuildCustomer("page1@get.com", "A", "One"));
        await repository.AddAsync(BuildCustomer("page2@get.com", "B", "Two"));
        await repository.AddAsync(BuildCustomer("page3@get.com", "C", "Three"));

        var page = await repository.GetAllAsync(skip: 1, take: 1);

        Assert.Single(page);
    }

    [Fact]
    public async Task EmailExistsAsync_WorksWithExcludeId()
    {
        using var dbContext = new CustomerDbContext(_dbOptions);
        var repository = new SqliteCustomerRepository(dbContext);
        var customer = BuildCustomer("exists@get.com");
        await repository.AddAsync(customer);

        var exists = await repository.EmailExistsAsync("exists@get.com");
        var existsWhenExcluded = await repository.EmailExistsAsync("exists@get.com", customer.Id);

        Assert.True(exists);
        Assert.False(existsWhenExcluded);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private static Customer BuildCustomer(string email, string firstName = "Test", string lastName = "User")
    {
        var now = DateTime.UtcNow;

        return new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = "+91-9999999999",
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Address = "Address",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }
}
