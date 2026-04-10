using CustomerCrudApi.Models;
using CustomerCrudApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CustomerCrudApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Customer>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<Customer>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (pageNumber <= 0 || pageSize <= 0)
        {
            return BadRequest(new { message = "pageNumber and pageSize must be positive integers." });
        }

        var result = await _customerService.GetAllAsync(pageNumber, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Customer), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Customer>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _customerService.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            _logger.LogWarning("Customer {CustomerId} was not found.", id);
            return NotFound();
        }

        return Ok(customer);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Customer), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Customer>> Create([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _customerService.CreateAsync(request, cancellationToken);

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
    public async Task<ActionResult<Customer>> Update(Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _customerService.UpdateAsync(id, request, cancellationToken);

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
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await _customerService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
