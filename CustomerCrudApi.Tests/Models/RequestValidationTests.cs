using System.ComponentModel.DataAnnotations;
using CustomerCrudApi.Models;

namespace CustomerCrudApi.Tests.Models;

public class RequestValidationTests
{
    [Fact]
    public void CreateCustomerRequest_WithValidData_IsValid()
    {
        var model = new CreateCustomerRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Phone = "+91-9999999999",
            DateOfBirth = new DateTime(1990, 1, 1),
            Address = "Address"
        };

        var results = ValidateModel(model);

        Assert.Empty(results);
    }

    [Fact]
    public void CreateCustomerRequest_WithInvalidEmail_IsInvalid()
    {
        var model = new CreateCustomerRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "invalid-email",
            Phone = "+91-9999999999",
            DateOfBirth = new DateTime(1990, 1, 1),
            Address = "Address"
        };

        var results = ValidateModel(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateCustomerRequest.Email)));
    }

    [Fact]
    public void UpdateCustomerRequest_WithMissingRequiredFields_IsInvalid()
    {
        var model = new UpdateCustomerRequest
        {
            FirstName = null!,
            LastName = null!,
            Email = "user@example.com",
            Phone = "+91-9999999999",
            DateOfBirth = new DateTime(1990, 1, 1),
            Address = null!
        };

        var results = ValidateModel(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateCustomerRequest.FirstName)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateCustomerRequest.LastName)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateCustomerRequest.Address)));
    }

    [Fact]
    public void UpdateCustomerRequest_WithTooLongFirstName_IsInvalid()
    {
        var model = new UpdateCustomerRequest
        {
            FirstName = new string('A', 101),
            LastName = "Doe",
            Email = "user@example.com",
            Phone = "+91-9999999999",
            DateOfBirth = new DateTime(1990, 1, 1),
            Address = "Address"
        };

        var results = ValidateModel(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateCustomerRequest.FirstName)));
    }

    private static List<ValidationResult> ValidateModel(object model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }
}