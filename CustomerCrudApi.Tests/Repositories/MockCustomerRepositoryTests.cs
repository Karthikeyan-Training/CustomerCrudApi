using CustomerCrudApi.Models;
using CustomerCrudApi.Repositories;

namespace CustomerCrudApi.Tests.Repositories;

public class MockCustomerRepositoryTests
{
    [Fact]
    public async Task Constructor_SeedsTenCustomers()
    {
        var repository = new MockCustomerRepository();

        var customers = await repository.GetAllAsync(0, 50);

        Assert.Equal(10, customers.Count);
    }

    [Fact]
    public async Task GetAll_ReturnsCustomersSortedByFirstAndLastName()
    {
        var repository = new MockCustomerRepository();

        var customers = (await repository.GetAllAsync(0, 50)).ToList();
        var sorted = customers
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .ToList();

        Assert.Equal(sorted.Select(c => c.Id), customers.Select(c => c.Id));
    }

    [Fact]
    public async Task Add_ThenGetById_ReturnsAddedCustomer()
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

        await repository.AddAsync(customer);
        var result = await repository.GetByIdAsync(customer.Id);

        Assert.NotNull(result);
        Assert.Equal(customer.Email, result!.Email);
    }

    [Fact]
    public async Task Update_WhenExistingCustomer_ReturnsUpdatedCustomer()
    {
        var repository = new MockCustomerRepository();
        var customer = (await repository.GetAllAsync(0, 50)).First();
        customer.FirstName = "Updated";

        var result = await repository.UpdateAsync(customer);

        Assert.NotNull(result);
        Assert.Equal("Updated", result!.FirstName);
    }

    [Fact]
    public async Task Update_WhenMissingCustomer_ReturnsNull()
    {
        var repository = new MockCustomerRepository();
        var customer = new Customer { Id = Guid.NewGuid(), FirstName = "A", LastName = "B", Email = "a@b.com" };

        var result = await repository.UpdateAsync(customer);

        Assert.Null(result);
    }

    [Fact]
    public async Task Delete_ReturnsTrueForExistingAndFalseForMissing()
    {
        var repository = new MockCustomerRepository();
        var existingId = (await repository.GetAllAsync(0, 50)).First().Id;

        var firstDelete = await repository.DeleteAsync(existingId);
        var secondDelete = await repository.DeleteAsync(existingId);

        Assert.True(firstDelete);
        Assert.False(secondDelete);
    }

    [Fact]
    public async Task EmailExists_IsCaseInsensitive_AndRespectsExcludeId()
    {
        var repository = new MockCustomerRepository();
        var customer = (await repository.GetAllAsync(0, 50)).First();
        var upperEmail = customer.Email.ToUpperInvariant();

        var existsWithoutExclusion = await repository.EmailExistsAsync(upperEmail);
        var existsWithExclusion = await repository.EmailExistsAsync(upperEmail, customer.Id);

        Assert.True(existsWithoutExclusion);
        Assert.False(existsWithExclusion);
    }
}