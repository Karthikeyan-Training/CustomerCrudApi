using CustomerCrudApi.Models;

namespace CustomerCrudApi.Services;

public class CustomerOperationResult
{
    public bool IsSuccess { get; init; }
    public bool IsNotFound { get; init; }
    public bool IsConflict { get; init; }
    public string? ErrorMessage { get; init; }
    public Customer? Customer { get; init; }

    public static CustomerOperationResult Success(Customer customer) => new()
    {
        IsSuccess = true,
        Customer = customer
    };

    public static CustomerOperationResult NotFound(string message) => new()
    {
        IsNotFound = true,
        ErrorMessage = message
    };

    public static CustomerOperationResult Conflict(string message) => new()
    {
        IsConflict = true,
        ErrorMessage = message
    };
}
