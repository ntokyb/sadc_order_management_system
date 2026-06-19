using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SadcOrders.Infrastructure.Messaging;
using SadcOrders.Worker.Models;

namespace SadcOrders.Worker;

public class WorkerService(IConfiguration configuration, ILogger<WorkerService> logger) : BackgroundService
{
    private IConnection? _connection;
    private IModel? _channel;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = configuration["RabbitMQ:Username"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest",
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(
            RabbitMqTopology.ExchangeName,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        _channel.QueueDeclare(
            RabbitMqTopology.FulfillmentQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        _channel.QueueBind(
            RabbitMqTopology.FulfillmentQueueName,
            RabbitMqTopology.ExchangeName,
            RabbitMqTopology.OrderCreatedRoutingKey);

        _channel.BasicQos(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnMessageReceivedAsync;

        _channel.BasicConsume(RabbitMqTopology.FulfillmentQueueName, autoAck: false, consumer);

        logger.LogInformation(
            "Worker consuming queue {Queue} bound to exchange {Exchange} with routing key {RoutingKey}",
            RabbitMqTopology.FulfillmentQueueName,
            RabbitMqTopology.ExchangeName,
            RabbitMqTopology.OrderCreatedRoutingKey);

        stoppingToken.Register(Shutdown);

        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var channel = _channel!;
        OrderCreatedPayload? payload = null;

        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            payload = JsonSerializer.Deserialize<OrderCreatedPayload>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("OrderCreated payload was empty.");

            logger.LogInformation("Processing fulfillment for order {OrderId}", payload.OrderId);

            await Task.Delay(500);

            logger.LogInformation("Fulfillment complete for order {OrderId}", payload.OrderId);

            channel.BasicAck(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            var orderId = payload?.OrderId;
            logger.LogError(ex, "Fulfillment processing failed for order {OrderId}", orderId);
            channel.BasicNack(ea.DeliveryTag, false, requeue: false);
        }
    }

    private void Shutdown()
    {
        try
        {
            _channel?.Close();
            _connection?.Close();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error shutting down RabbitMQ consumer");
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
