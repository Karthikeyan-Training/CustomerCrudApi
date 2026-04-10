using CustomerCrudApi.Data;
using CustomerCrudApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomerCrudApi.Repositories;

public class SqliteCustomerRepository : ICustomerRepository
{
    private readonly CustomerDbContext _dbContext;

    public SqliteCustomerRepository(CustomerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Customer>> GetAllAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .AsNoTracking()
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.Customers.CountAsync(cancellationToken);
    }

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Customer> AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return customer;
    }

    public async Task<Customer?> UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Customers.SingleOrDefaultAsync(c => c.Id == customer.Id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        existing.FirstName = customer.FirstName;
        existing.LastName = customer.LastName;
        existing.Email = customer.Email;
        existing.Phone = customer.Phone;
        existing.DateOfBirth = customer.DateOfBirth;
        existing.Address = customer.Address;
        existing.UpdatedAtUtc = customer.UpdatedAtUtc;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Customers.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        _dbContext.Customers.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim();

        return _dbContext.Customers.AnyAsync(c =>
            (!excludeId.HasValue || c.Id != excludeId.Value) &&
            c.Email.ToLower() == normalizedEmail.ToLower(),
            cancellationToken);
    }
}
