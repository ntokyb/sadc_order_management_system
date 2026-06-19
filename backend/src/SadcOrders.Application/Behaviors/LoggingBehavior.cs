using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace SadcOrders.Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var commandName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            logger.LogInformation(
                "Command {CommandName} completed in {DurationMs}ms",
                commandName,
                stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception)
        {
            logger.LogInformation(
                "Command {CommandName} failed after {DurationMs}ms",
                commandName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
