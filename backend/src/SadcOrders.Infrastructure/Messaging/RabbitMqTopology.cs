namespace SadcOrders.Infrastructure.Messaging;

public static class RabbitMqTopology
{
    public const string ExchangeName = "sadc.orders";
    public const string OrderCreatedRoutingKey = "order.created";
    public const string FulfillmentQueueName = "sadc.fulfillment";
}
