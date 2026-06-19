using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SadcOrders.API.Auth;
using SadcOrders.API.Middleware;
using SadcOrders.API.Models;
using SadcOrders.Application.Common;
using SadcOrders.Application.Dtos;
using SadcOrders.Application.Orders.Commands;
using SadcOrders.Application.Orders.Queries;
using SadcOrders.Domain.Enums;

namespace SadcOrders.API.Controllers;

/// <summary>
/// Order management endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RoleNames.ReadAccess)]
[Produces("application/json")]
public class OrdersController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Creates a new order for an existing customer.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.OrderAdmin)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderRequest body, CancellationToken ct)
    {
        var lineItems = body.LineItems
            .Select(l => new CreateOrderLineItemInput(l.ProductSku, l.Quantity, l.UnitPrice))
            .ToList();

        var created = await mediator.Send(new CreateOrderCommand(body.CustomerId, body.CurrencyCode, lineItems), ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Gets an order by identifier, including line items.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken ct)
    {
        var order = await mediator.Send(new GetOrderQuery(id), ct);
        return order is null ? NotFound() : Ok(order);
    }

    /// <summary>
    /// Lists orders with optional filters, sorting, and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderDto>>> List(
        [FromQuery] Guid? customerId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PaginationDefaults.DefaultPageSize,
        [FromQuery] string? sort = "createdAt_desc",
        CancellationToken ct = default)
    {
        OrderStatus? parsedStatus = Enum.TryParse<OrderStatus>(status, true, out var s) ? s : null;
        var result = await mediator.Send(new GetOrdersQuery(customerId, parsedStatus, page, pageSize, sort), ct);
        return Ok(result);
    }

    /// <summary>
    /// Updates order status using a valid lifecycle transition.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = RoleNames.OrderAdmin)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateOrderStatusRequest body,
        CancellationToken ct)
    {
        var idempotencyKey = HttpContext.GetIdempotencyKey();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new
            {
                error = "Idempotency-Key header is required.",
                correlationId = HttpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString()
            });
        }

        if (!Enum.TryParse<OrderStatus>(body.Status, true, out var newStatus))
        {
            return BadRequest(new
            {
                error = "Status must be one of: Paid, Fulfilled, Cancelled.",
                correlationId = HttpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString()
            });
        }

        var updated = await mediator.Send(
            new UpdateOrderStatusCommand(id, newStatus, idempotencyKey),
            ct);

        return Ok(updated);
    }

    /// <summary>
    /// Deletes a pending order.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleNames.OrderAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteOrderCommand(id), ct);
        return NoContent();
    }
}
