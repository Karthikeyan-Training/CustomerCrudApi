using CustomerCrudApi.Models;
using CustomerCrudApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CustomerCrudApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<Customer>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyCollection<Customer>> GetAll()
    {
        return Ok(_customerService.GetAll());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Customer), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Customer> GetById(Guid id)
    {
        var customer = _customerService.GetById(id);
        if (customer is null)
        {
            return NotFound();
        }

        return Ok(customer);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Customer), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult<Customer> Create([FromBody] CreateCustomerRequest request)
    {
        var result = _customerService.Create(request);

        if (result.IsConflict)
        {
            return Conflict(new { message = result.ErrorMessage });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Customer!.Id }, result.Customer);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Customer), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult<Customer> Update(Guid id, [FromBody] UpdateCustomerRequest request)
    {
        var result = _customerService.Update(id, request);

        if (result.IsNotFound)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        if (result.IsConflict)
        {
            return Conflict(new { message = result.ErrorMessage });
        }

        return Ok(result.Customer);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(Guid id)
    {
        var deleted = _customerService.Delete(id);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
