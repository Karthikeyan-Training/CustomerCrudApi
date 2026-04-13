using CustomerCrudApi.Models;
using CustomerCrudApi.Services;

namespace CustomerCrudApi.Tests.Services.Results;

public class CustomerOperationResultTests
{
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        var customer = new Customer { Id = Guid.NewGuid(), FirstName = "A", LastName = "B", Email = "a@b.com" };

        var result = CustomerOperationResult.Success(customer);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsConflict);
        Assert.False(result.IsNotFound);
        Assert.Null(result.ErrorMessage);
        Assert.Same(customer, result.Customer);
    }

    [Fact]
    public void NotFound_CreatesNotFoundResult()
    {
        var result = CustomerOperationResult.NotFound("Missing");

        Assert.True(result.IsNotFound);
        Assert.False(result.IsSuccess);
        Assert.False(result.IsConflict);
        Assert.Equal("Missing", result.ErrorMessage);
        Assert.Null(result.Customer);
    }

    [Fact]
    public void Conflict_CreatesConflictResult()
    {
        var result = CustomerOperationResult.Conflict("Conflict");

        Assert.True(result.IsConflict);
        Assert.False(result.IsSuccess);
        Assert.False(result.IsNotFound);
        Assert.False(result.IsValidationFailure);
        Assert.Equal("Conflict", result.ErrorMessage);
        Assert.Null(result.Customer);
    }

    [Fact]
    public void ValidationFailure_CreatesValidationFailureResult()
    {
        var result = CustomerOperationResult.ValidationFailure("Invalid input");

        Assert.True(result.IsValidationFailure);
        Assert.False(result.IsSuccess);
        Assert.False(result.IsNotFound);
        Assert.False(result.IsConflict);
        Assert.Equal("Invalid input", result.ErrorMessage);
        Assert.Null(result.Customer);
    }
}