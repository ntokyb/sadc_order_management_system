namespace SadcOrders.API.Middleware;

public class IdempotencyMiddleware(RequestDelegate next)
{
    public const string HeaderName = "Idempotency-Key";
    public const string ItemKey = "IdempotencyKey";

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var values))
        {
            var key = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(key))
                context.Items[ItemKey] = key.Trim();
        }

        await next(context);
    }
}

public static class IdempotencyMiddlewareExtensions
{
    public static IApplicationBuilder UseIdempotencyKey(this IApplicationBuilder app) =>
        app.UseMiddleware<IdempotencyMiddleware>();

    public static string? GetIdempotencyKey(this HttpContext context) =>
        context.Items.TryGetValue(IdempotencyMiddleware.ItemKey, out var value)
            ? value as string
            : null;
}
