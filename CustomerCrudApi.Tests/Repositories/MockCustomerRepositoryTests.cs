using CustomerCrudApi.Models;
using CustomerCrudApi.Repositories;

namespace CustomerCrudApi.Tests.Repositories;

public class MockCustomerRepositoryTests
{
    [Fact]
    public void Constructor_SeedsTenCustomers()
    {
        var repository = new MockCustomerRepository();

        var customers = repository.GetAll();

        Assert.Equal(10, customers.Count);
    }

    [Fact]
    public void GetAll_ReturnsCustomersSortedByFirstAndLastName()
    {
        var repository = new MockCustomerRepository();

        var customers = repository.GetAll().ToList();
        var sorted = customers
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .ToList();

        Assert.Equal(sorted.Select(c => c.Id), customers.Select(c => c.Id));
    }

    [Fact]
    public void Add_ThenGetById_ReturnsAddedCustomer()
    {
        var repository = new MockCustomerRepository();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "User",
            Email = "test.user@example.com",
            Phone = "+91-7777777777",
            DateOfBirth = new DateTime(1991, 1, 1),
            Address = "Address"
        };

        repository.Add(customer);
        var result = repository.GetById(customer.Id);

        Assert.NotNull(result);
        Assert.Equal(customer.Email, result!.Email);
    }

    [Fact]
    public void Update_WhenExistingCustomer_ReturnsUpdatedCustomer()
    {
        var repository = new MockCustomerRepository();
        var customer = repository.GetAll().First();
        customer.FirstName = "Updated";

        var result = repository.Update(customer);

        Assert.NotNull(result);
        Assert.Equal("Updated", result!.FirstName);
    }

    [Fact]
    public void Update_WhenMissingCustomer_ReturnsNull()
    {
        var repository = new MockCustomerRepository();
        var customer = new Customer { Id = Guid.NewGuid(), FirstName = "A", LastName = "B", Email = "a@b.com" };

        var result = repository.Update(customer);

        Assert.Null(result);
    }

    [Fact]
    public void Delete_ReturnsTrueForExistingAndFalseForMissing()
    {
        var repository = new MockCustomerRepository();
        var existingId = repository.GetAll().First().Id;

        var firstDelete = repository.Delete(existingId);
        var secondDelete = repository.Delete(existingId);

        Assert.True(firstDelete);
        Assert.False(secondDelete);
    }

    [Fact]
    public void EmailExists_IsCaseInsensitive_AndRespectsExcludeId()
    {
        var repository = new MockCustomerRepository();
        var customer = repository.GetAll().First();
        var upperEmail = customer.Email.ToUpperInvariant();

        var existsWithoutExclusion = repository.EmailExists(upperEmail);
        var existsWithExclusion = repository.EmailExists(upperEmail, customer.Id);

        Assert.True(existsWithoutExclusion);
        Assert.False(existsWithExclusion);
    }
}