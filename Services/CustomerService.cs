using CustomerCrudApi.Models;
using CustomerCrudApi.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerCrudApi.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;
    private readonly ILogger<CustomerService> _logger;
    private readonly TimeProvider _timeProvider;
    private const int MaxPageSize = 100;

    public CustomerService(ICustomerRepository repository, ILogger<CustomerService> logger, TimeProvider timeProvider)
    {
        _repository = repository;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<PagedResult<Customer>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var normalizedPageNumber = Math.Max(pageNumber, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var skip = (normalizedPageNumber - 1) * normalizedPageSize;

        _logger.LogInformation("Fetching customers page {PageNumber} with page size {PageSize}.", normalizedPageNumber, normalizedPageSize);

        var totalCount = await _repository.CountAsync(cancellationToken);
        var customers = await _repository.GetAllAsync(skip, normalizedPageSize, cancellationToken);

        return new PagedResult<Customer>
        {
            Items = customers,
            TotalCount = totalCount,
            PageNumber = normalizedPageNumber,
            PageSize = normalizedPageSize
        };
    }

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<CustomerOperationResult> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeRequest(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Phone,
            request.DateOfBirth,
            request.Address);

        if (IsFutureDate(normalized.DateOfBirth))
        {
            return CustomerOperationResult.ValidationFailure("DateOfBirth must be in the past.");
        }

        if (await _repository.EmailExistsAsync(normalized.Email, cancellationToken: cancellationToken))
        {
            _logger.LogWarning("Customer create failed because email already exists.");
            return CustomerOperationResult.Conflict("A customer with the same email already exists.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = normalized.FirstName,
            LastName = normalized.LastName,
            Email = normalized.Email,
            Phone = normalized.Phone,
            DateOfBirth = normalized.DateOfBirth,
            Address = normalized.Address,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };

        try
        {
            await _repository.AddAsync(customer, cancellationToken);
        }
        catch (DbUpdateException)
        {
            _logger.LogWarning("Customer create failed due to a database constraint violation.");
            return CustomerOperationResult.Conflict("A customer with the same email already exists.");
        }

        _logger.LogInformation("Customer {CustomerId} created successfully.", customer.Id);
        return CustomerOperationResult.Success(customer);
    }

    public async Task<CustomerOperationResult> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            _logger.LogWarning("Customer update failed because customer {CustomerId} was not found.", id);
            return CustomerOperationResult.NotFound("Customer was not found.");
        }

        var normalized = NormalizeRequest(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Phone,
            request.DateOfBirth,
            request.Address);

        if (IsFutureDate(normalized.DateOfBirth))
        {
            return CustomerOperationResult.ValidationFailure("DateOfBirth must be in the past.");
        }

        if (await _repository.EmailExistsAsync(normalized.Email, excludeId: id, cancellationToken: cancellationToken))
        {
            _logger.LogWarning("Customer update failed because email already exists.");
            return CustomerOperationResult.Conflict("A customer with the same email already exists.");
        }

        existing.FirstName = normalized.FirstName;
        existing.LastName = normalized.LastName;
        existing.Email = normalized.Email;
        existing.Phone = normalized.Phone;
        existing.DateOfBirth = normalized.DateOfBirth;
        existing.Address = normalized.Address;
        existing.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        Customer? updated;
        try
        {
            updated = await _repository.UpdateAsync(existing, cancellationToken);
        }
        catch (DbUpdateException)
        {
            _logger.LogWarning("Customer update failed for customer {CustomerId} due to a database constraint violation.", id);
            return CustomerOperationResult.Conflict("A customer with the same email already exists.");
        }

        if (updated is null)
        {
            _logger.LogWarning("Customer update failed for customer {CustomerId} because repository update returned null.", id);
            return CustomerOperationResult.NotFound("Customer was not found.");
        }

        _logger.LogInformation("Customer {CustomerId} updated successfully.", id);
        return CustomerOperationResult.Success(updated);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await _repository.DeleteAsync(id, cancellationToken);

        if (deleted)
        {
            _logger.LogInformation("Customer {CustomerId} deleted successfully.", id);
        }
        else
        {
            _logger.LogWarning("Customer delete failed because customer {CustomerId} was not found.", id);
        }

        return deleted;
    }

    private bool IsFutureDate(DateTime date)
    {
        return date.Date > _timeProvider.GetUtcNow().Date;
    }

    private static NormalizedRequest NormalizeRequest(
        string firstName,
        string lastName,
        string email,
        string phone,
        DateTime dateOfBirth,
        string address)
    {
        return new NormalizedRequest(
            firstName.Trim(),
            lastName.Trim(),
            email.Trim().ToLowerInvariant(),
            phone.Trim(),
            DateTime.SpecifyKind(dateOfBirth, DateTimeKind.Utc),
            address.Trim());
    }

    private sealed record NormalizedRequest(
        string FirstName,
        string LastName,
        string Email,
        string Phone,
        DateTime DateOfBirth,
        string Address);
}
