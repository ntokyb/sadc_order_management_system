using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SadcOrders.Infrastructure.Messaging;
using SadcOrders.Infrastructure.Persistence;

namespace SadcOrders.API.Services;

public class OutboxPollingService(
    IServiceScopeFactory scopeFactory,
    IRabbitMqPublisher publisher,
    ILogger<OutboxPollingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox polling cycle failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task PollAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SadcOrdersDbContext>();

        var messages = await db.OutboxMessages
            .Where(x => x.PublishedAt == null)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

        if (messages.Count == 0)
            return;

        var publishedCount = 0;

        foreach (var message in messages)
        {
            var routingKey = ResolveRoutingKey(message.EventType);

            try
            {
                await publisher.PublishAsync(
                    RabbitMqTopology.ExchangeName,
                    routingKey,
                    message.Payload,
                    ct);

                message.PublishedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
                publishedCount++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to publish outbox message {MessageId} with event type {EventType}",
                    message.Id,
                    message.EventType);
            }
        }

        if (publishedCount > 0)
            logger.LogInformation("Published {PublishedCount} outbox message(s)", publishedCount);
    }

    private static string ResolveRoutingKey(string eventType) =>
        eventType switch
        {
            "OrderCreated" => RabbitMqTopology.OrderCreatedRoutingKey,
            _ => eventType.ToLowerInvariant()
        };
}
