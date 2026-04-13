using CustomerCrudApi.Models;

namespace CustomerCrudApi.Services;

public interface ICustomerService
{
    Task<PagedResult<Customer>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerOperationResult> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerOperationResult> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
