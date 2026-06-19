using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SadcOrders.Application.Dtos;
using SadcOrders.Domain.Enums;
using SadcOrders.Infrastructure.Persistence;

namespace SadcOrders.IntegrationTests;

[Collection("Integration")]
public class ApiIntegrationTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _client = fixture.Client;

    [Fact]
    public async Task PostCustomers_CreatesValidCustomerAndCanBeRetrieved()
    {
        var email = $"customer-{Guid.NewGuid():N}@example.co.za";
        var createResponse = await _client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            "Integration Customer",
            email,
            "ZA"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerDto>(IntegrationTestFixture.JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("ZA", created!.CountryCode);

        var getResponse = await _client.GetAsync($"/api/customers/{created.Id}");
        getResponse.EnsureSuccessStatusCode();
        var fetched = await getResponse.Content.ReadFromJsonAsync<CustomerDto>(IntegrationTestFixture.JsonOptions);

        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal(email, fetched.Email);
    }

    [Fact]
    public async Task PostOrders_WithInvalidCurrencyForCountry_ReturnsBadRequestWithClearMessage()
    {
        var customer = await CreateCustomerAsync("ZA");
        var orderResponse = await _client.PostAsJsonAsync("/api/orders", new CreateOrderRequest(
            customer.Id,
            "BWP",
            [new CreateOrderLineItemRequest("SKU-INVALID", 1, 100m)]));

        Assert.Equal(HttpStatusCode.BadRequest, orderResponse.StatusCode);
        var error = await orderResponse.Content.ReadFromJsonAsync<ErrorResponse>(IntegrationTestFixture.JsonOptions);
        Assert.NotNull(error?.Error);
        Assert.Contains("BWP", error!.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ZA", error.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OrderStatusTransition_FollowsLifecycleRules()
    {
        var order = await CreatePendingOrderAsync();

        var paidResponse = await UpdateStatusAsync(order.Id, OrderStatus.Paid, Guid.NewGuid().ToString());
        paidResponse.EnsureSuccessStatusCode();
        var paidOrder = await paidResponse.Content.ReadFromJsonAsync<OrderDto>(IntegrationTestFixture.JsonOptions);
        Assert.Equal(OrderStatus.Paid, paidOrder!.Status);

        var fulfilledResponse = await UpdateStatusAsync(order.Id, OrderStatus.Fulfilled, Guid.NewGuid().ToString());
        fulfilledResponse.EnsureSuccessStatusCode();
        var fulfilledOrder = await fulfilledResponse.Content.ReadFromJsonAsync<OrderDto>(IntegrationTestFixture.JsonOptions);
        Assert.Equal(OrderStatus.Fulfilled, fulfilledOrder!.Status);

        var cancelledResponse = await UpdateStatusAsync(order.Id, OrderStatus.Cancelled, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.BadRequest, cancelledResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateOrderStatus_WithSameIdempotencyKey_ReturnsIdenticalResponse()
    {
        var order = await CreatePendingOrderAsync();
        const string idempotencyKey = "test-key-1";

        var firstResponse = await UpdateStatusAsync(order.Id, OrderStatus.Paid, idempotencyKey);
        firstResponse.EnsureSuccessStatusCode();
        var firstBody = await firstResponse.Content.ReadAsStringAsync();

        var secondResponse = await UpdateStatusAsync(order.Id, OrderStatus.Paid, idempotencyKey);
        secondResponse.EnsureSuccessStatusCode();
        var secondBody = await secondResponse.Content.ReadAsStringAsync();

        Assert.Equal(firstBody, secondBody);

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SadcOrdersDbContext>();
        var persisted = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == order.Id);
        Assert.Equal(OrderStatus.Paid, persisted.Status);
    }

    [Fact]
    public async Task CreateOrder_WritesOutboxMessageWithOrderCreatedEvent()
    {
        var order = await CreatePendingOrderAsync();

        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SadcOrdersDbContext>();
        var messages = await db.OutboxMessages
            .Where(message => message.EventType == "OrderCreated" && message.Payload.Contains(order.Id.ToString()))
            .ToListAsync();

        Assert.Single(messages);
        Assert.Null(messages[0].PublishedAt);
    }

    [Fact]
    public async Task OrderReader_CannotCreateCustomer_ReturnsForbidden()
    {
        await fixture.AuthenticateAsync("viewer", "Viewer123!");

        var response = await _client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            "Read Only Attempt",
            $"viewer-{Guid.NewGuid():N}@example.co.za",
            "ZA"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        await fixture.AuthenticateAsync();
    }

    [Fact]
    public async Task PutCustomer_UpdatesCustomerDetails()
    {
        var created = await CreateCustomerAsync("ZA");

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/customers/{created.Id}",
            new UpdateCustomerRequest("Updated Name", created.Email, "BW"));

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<CustomerDto>(IntegrationTestFixture.JsonOptions);
        Assert.Equal("Updated Name", updated!.Name);
        Assert.Equal("BW", updated.CountryCode);
    }

    [Fact]
    public async Task DeletePendingOrder_RemovesOrder()
    {
        var order = await CreatePendingOrderAsync();

        var deleteResponse = await _client.DeleteAsync($"/api/orders/{order.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/orders/{order.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<CustomerDto> CreateCustomerAsync(string countryCode)
    {
        var response = await _client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            $"Customer {Guid.NewGuid():N}",
            $"user-{Guid.NewGuid():N}@example.com",
            countryCode));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CustomerDto>(IntegrationTestFixture.JsonOptions))!;
    }

    private async Task<OrderDto> CreatePendingOrderAsync()
    {
        var customer = await CreateCustomerAsync("ZA");
        var response = await _client.PostAsJsonAsync("/api/orders", new CreateOrderRequest(
            customer.Id,
            "ZAR",
            [new CreateOrderLineItemRequest("SKU-1", 2, 50m)]));
        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<OrderDto>(IntegrationTestFixture.JsonOptions);
        Assert.Equal(OrderStatus.Pending, order!.Status);
        Assert.Equal(100m, order.TotalAmount);
        return order;
    }

    private Task<HttpResponseMessage> UpdateStatusAsync(Guid orderId, OrderStatus status, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/orders/{orderId}/status")
        {
            Content = JsonContent.Create(new UpdateOrderStatusRequest(status.ToString()))
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return _client.SendAsync(request);
    }

    private sealed record CreateCustomerRequest(string Name, string Email, string CountryCode);

    private sealed record UpdateCustomerRequest(string Name, string Email, string CountryCode);

    private sealed record CreateOrderLineItemRequest(string ProductSku, int Quantity, decimal UnitPrice);

    private sealed record CreateOrderRequest(
        Guid CustomerId,
        string CurrencyCode,
        IReadOnlyList<CreateOrderLineItemRequest> LineItems);

    private sealed record UpdateOrderStatusRequest(string Status);

    private sealed record ErrorResponse(string Error);
}
