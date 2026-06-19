namespace SadcOrders.Infrastructure.Messaging;

public interface IRabbitMqPublisher
{
    Task PublishAsync(string exchange, string routingKey, string payload, CancellationToken ct);
}
