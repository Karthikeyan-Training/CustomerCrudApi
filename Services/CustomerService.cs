using CustomerCrudApi.Models;
using CustomerCrudApi.Repositories;

namespace CustomerCrudApi.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;

    public CustomerService(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyCollection<Customer> GetAll()
    {
        return _repository.GetAll();
    }

    public Customer? GetById(Guid id)
    {
        return _repository.GetById(id);
    }

    public CustomerOperationResult Create(CreateCustomerRequest request)
    {
        if (IsFutureDate(request.DateOfBirth))
        {
            return CustomerOperationResult.Conflict("DateOfBirth must be in the past.");
        }

        if (_repository.EmailExists(request.Email))
        {
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

        _repository.Add(customer);
        return CustomerOperationResult.Success(customer);
    }

    public CustomerOperationResult Update(Guid id, UpdateCustomerRequest request)
    {
        var existing = _repository.GetById(id);
        if (existing is null)
        {
            return CustomerOperationResult.NotFound("Customer was not found.");
        }

        if (IsFutureDate(request.DateOfBirth))
        {
            return CustomerOperationResult.Conflict("DateOfBirth must be in the past.");
        }

        if (_repository.EmailExists(request.Email, excludeId: id))
        {
            return CustomerOperationResult.Conflict("A customer with the same email already exists.");
        }

        existing.FirstName = request.FirstName.Trim();
        existing.LastName = request.LastName.Trim();
        existing.Email = request.Email.Trim();
        existing.Phone = request.Phone.Trim();
        existing.DateOfBirth = DateTime.SpecifyKind(request.DateOfBirth, DateTimeKind.Utc);
        existing.Address = request.Address.Trim();
        existing.UpdatedAtUtc = DateTime.UtcNow;

        var updated = _repository.Update(existing);
        if (updated is null)
        {
            return CustomerOperationResult.NotFound("Customer was not found.");
        }

        return CustomerOperationResult.Success(updated);
    }

    public bool Delete(Guid id)
    {
        return _repository.Delete(id);
    }

    private static bool IsFutureDate(DateTime date)
    {
        return date.Date > DateTime.UtcNow.Date;
    }
}
