using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace SadcOrders.Infrastructure.Messaging;

public class RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger) : IRabbitMqPublisher
{
    public Task PublishAsync(string exchange, string routingKey, string payload, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var factory = CreateConnectionFactory();
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(exchange, ExchangeType.Direct, durable: true, autoDelete: false);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        var body = Encoding.UTF8.GetBytes(payload);
        channel.BasicPublish(exchange, routingKey, properties, body);

        logger.LogDebug(
            "Published message to exchange {Exchange} with routing key {RoutingKey}",
            exchange,
            routingKey);

        return Task.CompletedTask;
    }

    private ConnectionFactory CreateConnectionFactory()
    {
        var portSetting = configuration["RabbitMQ:Port"];
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = configuration["RabbitMQ:Username"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest",
            DispatchConsumersAsync = true
        };

        if (int.TryParse(portSetting, out var port))
            factory.Port = port;

        return factory;
    }
}
