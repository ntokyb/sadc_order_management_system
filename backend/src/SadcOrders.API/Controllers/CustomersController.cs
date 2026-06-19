using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SadcOrders.API.Auth;
using SadcOrders.Application.Common;
using SadcOrders.Application.Customers.Commands;
using SadcOrders.Application.Customers.Queries;
using SadcOrders.Application.Dtos;
using SadcOrders.API.Models;

namespace SadcOrders.API.Controllers;

/// <summary>
/// Customer management endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RoleNames.ReadAccess)]
[Produces("application/json")]
public class CustomersController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Creates a new customer in a supported SADC country.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.OrderAdmin)]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerRequest body, CancellationToken ct)
    {
        var created = await mediator.Send(new CreateCustomerCommand(body.Name, body.Email, body.CountryCode), ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Gets a customer by identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> GetById(Guid id, CancellationToken ct)
    {
        var customer = await mediator.Send(new GetCustomerQuery(id), ct);
        return customer is null ? NotFound() : Ok(customer);
    }

    /// <summary>
    /// Searches customers with optional text filter and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CustomerDto>>> Search(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PaginationDefaults.DefaultPageSize,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetCustomersQuery(search, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Updates an existing customer.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleNames.OrderAdmin)]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerDto>> Update(Guid id, [FromBody] UpdateCustomerRequest body, CancellationToken ct)
    {
        var updated = await mediator.Send(
            new UpdateCustomerCommand(id, body.Name, body.Email, body.CountryCode),
            ct);
        return Ok(updated);
    }

    /// <summary>
    /// Deletes a customer with no orders.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleNames.OrderAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteCustomerCommand(id), ct);
        return NoContent();
    }
}
