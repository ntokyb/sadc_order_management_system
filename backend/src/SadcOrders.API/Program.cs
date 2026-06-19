using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using SadcOrders.API.Auth;
using SadcOrders.API.Middleware;
using SadcOrders.API.Services;
using SadcOrders.API.Swagger;
using SadcOrders.Application;
using SadcOrders.Infrastructure;
using SadcOrders.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "SadcOrders.API")
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSadcOrdersSwagger();
builder.Services.AddMemoryCache();
builder.Services.Configure<DevAuthOptions>(builder.Configuration.GetSection(DevAuthOptions.SectionName));

var jwtKey = builder.Configuration["Jwt:Key"] ?? "SadcOrdersDevSigningKeyMustBeAtLeast32Chars!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "sadc-orders-dev";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
}
else
{
    builder.Services.AddApiInfrastructure(builder.Configuration);
    builder.Services.AddHostedService<OutboxPollingService>();
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Web", policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"])
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    Log.Information("Applying database migrations...");
    await app.Services.MigrateDatabaseAsync();
    Log.Information("Seeding sample data if needed...");
    await SampleDataSeeder.SeedIfEmptyAsync(app.Services);
}

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "SADC Orders API v1"));
}

app.UseCorrelationId();
app.UseIdempotencyKey();
app.UseExceptionHandling();
app.UseHttpMetrics();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId",
            httpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString() ?? httpContext.TraceIdentifier);
    };
});
app.UseCors("Web");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapMetrics();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .AllowAnonymous()
    .WithName("HealthCheck")
    .WithTags("Health")
    .Produces(StatusCodes.Status200OK);

Log.Information("SADC Orders API starting...");
app.Run();

public partial class Program;
