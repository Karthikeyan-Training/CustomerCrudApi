using CustomerCrudApi.Models;
using CustomerCrudApi.Repositories;
using Microsoft.Extensions.Logging;

namespace CustomerCrudApi.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;
    private readonly ILogger<CustomerService> _logger;
    private const int MaxPageSize = 100;

    public CustomerService(ICustomerRepository repository, ILogger<CustomerService> logger)
    {
        _repository = repository;
        _logger = logger;
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
        if (IsFutureDate(request.DateOfBirth))
        {
            return CustomerOperationResult.Conflict("DateOfBirth must be in the past.");
        }

        if (await _repository.EmailExistsAsync(request.Email, cancellationToken: cancellationToken))
        {
            _logger.LogWarning("Customer create failed because email {Email} already exists.", request.Email);
            return CustomerOperationResult.Conflict("A customer with the same email already exists.");
        }

        var nowUtc = DateTime.UtcNow;
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            DateOfBirth = DateTime.SpecifyKind(request.DateOfBirth, DateTimeKind.Utc),
            Address = request.Address.Trim(),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };

        await _repository.AddAsync(customer, cancellationToken);
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

        if (IsFutureDate(request.DateOfBirth))
        {
            return CustomerOperationResult.Conflict("DateOfBirth must be in the past.");
        }

        if (await _repository.EmailExistsAsync(request.Email, excludeId: id, cancellationToken: cancellationToken))
        {
            _logger.LogWarning("Customer update failed because email {Email} already exists.", request.Email);
            return CustomerOperationResult.Conflict("A customer with the same email already exists.");
        }

        existing.FirstName = request.FirstName.Trim();
        existing.LastName = request.LastName.Trim();
        existing.Email = request.Email.Trim();
        existing.Phone = request.Phone.Trim();
        existing.DateOfBirth = DateTime.SpecifyKind(request.DateOfBirth, DateTimeKind.Utc);
        existing.Address = request.Address.Trim();
        existing.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(existing, cancellationToken);
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

    private static bool IsFutureDate(DateTime date)
    {
        return date.Date > DateTime.UtcNow.Date;
    }
}
