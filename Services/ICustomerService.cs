using CustomerCrudApi.Models;

namespace CustomerCrudApi.Services;

public interface ICustomerService
{
    IReadOnlyCollection<Customer> GetAll();
    Customer? GetById(Guid id);
    CustomerOperationResult Create(CreateCustomerRequest request);
    CustomerOperationResult Update(Guid id, UpdateCustomerRequest request);
    bool Delete(Guid id);
}
