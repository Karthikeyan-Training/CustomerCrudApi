using System.Collections.Concurrent;
using CustomerCrudApi.Models;

namespace CustomerCrudApi.Repositories;

public class MockCustomerRepository : ICustomerRepository
{
    private readonly ConcurrentDictionary<Guid, Customer> _customers;

    public MockCustomerRepository()
    {
        _customers = new ConcurrentDictionary<Guid, Customer>(SeedCustomers().ToDictionary(c => c.Id));
    }

    public IReadOnlyCollection<Customer> GetAll()
    {
        return _customers.Values
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .ToList();
    }

    public Customer? GetById(Guid id)
    {
        _customers.TryGetValue(id, out var customer);
        return customer;
    }

    public Customer Add(Customer customer)
    {
        _customers[customer.Id] = customer;
        return customer;
    }

    public Customer? Update(Customer customer)
    {
        if (!_customers.ContainsKey(customer.Id))
        {
            return null;
        }

        _customers[customer.Id] = customer;
        return customer;
    }

    public bool Delete(Guid id)
    {
        return _customers.TryRemove(id, out _);
    }

    public bool EmailExists(string email, Guid? excludeId = null)
    {
        return _customers.Values.Any(c =>
            (!excludeId.HasValue || c.Id != excludeId.Value) &&
            string.Equals(c.Email, email, StringComparison.OrdinalIgnoreCase));
    }

    private static List<Customer> SeedCustomers()
    {
        var seedTime = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);

        return
        [
            NewCustomer("Aarav", "Sharma", "aarav.sharma@example.com", "+91-9000000001", new DateTime(1992, 2, 10), "12 MG Road, Bengaluru", seedTime),
            NewCustomer("Diya", "Nair", "diya.nair@example.com", "+91-9000000002", new DateTime(1994, 8, 21), "33 Park Avenue, Kochi", seedTime),
            NewCustomer("Kunal", "Verma", "kunal.verma@example.com", "+91-9000000003", new DateTime(1989, 12, 5), "101 Lake View, Pune", seedTime),
            NewCustomer("Meera", "Iyer", "meera.iyer@example.com", "+91-9000000004", new DateTime(1996, 3, 14), "7 Temple Street, Chennai", seedTime),
            NewCustomer("Rahul", "Kapoor", "rahul.kapoor@example.com", "+91-9000000005", new DateTime(1991, 11, 30), "88 Hill Road, Mumbai", seedTime),
            NewCustomer("Sneha", "Patel", "sneha.patel@example.com", "+91-9000000006", new DateTime(1993, 1, 25), "55 River Lane, Ahmedabad", seedTime),
            NewCustomer("Vikram", "Rao", "vikram.rao@example.com", "+91-9000000007", new DateTime(1988, 6, 19), "9 Residency Rd, Hyderabad", seedTime),
            NewCustomer("Ananya", "Singh", "ananya.singh@example.com", "+91-9000000008", new DateTime(1995, 9, 9), "41 Rose Colony, Jaipur", seedTime),
            NewCustomer("Nikhil", "Das", "nikhil.das@example.com", "+91-9000000009", new DateTime(1990, 4, 2), "23 Green Park, Kolkata", seedTime),
            NewCustomer("Priya", "Menon", "priya.menon@example.com", "+91-9000000010", new DateTime(1997, 7, 16), "64 Palm Grove, Thiruvananthapuram", seedTime)
        ];
    }

    private static Customer NewCustomer(
        string firstName,
        string lastName,
        string email,
        string phone,
        DateTime dob,
        string address,
        DateTime timestampUtc)
    {
        return new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            DateOfBirth = DateTime.SpecifyKind(dob, DateTimeKind.Utc),
            Address = address,
            CreatedAtUtc = timestampUtc,
            UpdatedAtUtc = timestampUtc
        };
    }
}
