using CustomerCrudApi.Models;

namespace CustomerCrudApi.Repositories;

public interface ICustomerRepository
{
    IReadOnlyCollection<Customer> GetAll();
    Customer? GetById(Guid id);
    Customer Add(Customer customer);
    Customer? Update(Customer customer);
    bool Delete(Guid id);
    bool EmailExists(string email, Guid? excludeId = null);
}
