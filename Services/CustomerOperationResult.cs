using CustomerCrudApi.Models;

namespace CustomerCrudApi.Services;

public enum OperationStatus
{
    Success,
    NotFound,
    Conflict,
    ValidationFailure
}

public class CustomerOperationResult
{
    public OperationStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public Customer? Customer { get; init; }

    public bool IsSuccess => Status == OperationStatus.Success;
    public bool IsNotFound => Status == OperationStatus.NotFound;
    public bool IsConflict => Status == OperationStatus.Conflict;
    public bool IsValidationFailure => Status == OperationStatus.ValidationFailure;

    public static CustomerOperationResult Success(Customer customer) => new()
    {
        Status = OperationStatus.Success,
        Customer = customer
    };

    public static CustomerOperationResult NotFound(string message) => new()
    {
        Status = OperationStatus.NotFound,
        ErrorMessage = message
    };

    public static CustomerOperationResult Conflict(string message) => new()
    {
        Status = OperationStatus.Conflict,
        ErrorMessage = message
    };

    public static CustomerOperationResult ValidationFailure(string message) => new()
    {
        Status = OperationStatus.ValidationFailure,
        ErrorMessage = message
    };
}
