using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SadcOrders.Infrastructure.Persistence;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;

namespace SadcOrders.IntegrationTests;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder().Build();
    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder().Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _msSqlContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();

        var sqlConnectionString = _msSqlContainer.GetConnectionString();
        var rabbitHost = _rabbitMqContainer.Hostname;
        var rabbitPort = _rabbitMqContainer.GetMappedPublicPort(5672);

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = sqlConnectionString,
                    ["RabbitMQ:Host"] = rabbitHost,
                    ["RabbitMQ:Port"] = rabbitPort.ToString(),
                    ["RabbitMQ:Username"] = "guest",
                    ["RabbitMQ:Password"] = "guest",
                    ["DevAuth:Enabled"] = "true",
                    ["DevAuth:Users:0:Username"] = "admin",
                    ["DevAuth:Users:0:Password"] = "Admin123!",
                    ["DevAuth:Users:0:Roles:0"] = "OrderAdmin",
                    ["DevAuth:Users:1:Username"] = "viewer",
                    ["DevAuth:Users:1:Password"] = "Viewer123!",
                    ["DevAuth:Users:1:Roles:0"] = "OrderReader",
                });
            });
            builder.ConfigureServices(services =>
            {
                var descriptors = services
                    .Where(descriptor =>
                        descriptor.ServiceType == typeof(DbContextOptions<SadcOrdersDbContext>)
                        || descriptor.ServiceType == typeof(SadcOrdersDbContext))
                    .ToList();

                foreach (var descriptor in descriptors)
                    services.Remove(descriptor);

                services.AddDbContext<SadcOrdersDbContext>(options =>
                    options.UseSqlServer(sqlConnectionString));
            });
        });

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SadcOrdersDbContext>();
            await db.Database.MigrateAsync();
        }

        Client = Factory.CreateClient();
        await AuthenticateAsync();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (Factory is not null)
            await Factory.DisposeAsync();

        await _rabbitMqContainer.DisposeAsync();
        await _msSqlContainer.DisposeAsync();
    }

    public async Task AuthenticateAsync(string username = "admin", string password = "Admin123!")
    {
        var tokenResponse = await Client.PostAsJsonAsync("/api/dev/login", new { username, password });
        tokenResponse.EnsureSuccessStatusCode();
        var payload = await tokenResponse.Content.ReadFromJsonAsync<DevLoginResponse>(IntegrationTestFixture.JsonOptions);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload!.Token);
    }

    public IServiceScope CreateScope() => Factory.Services.CreateScope();

    private sealed record DevLoginResponse(string Token, string Username, IReadOnlyList<string> Roles);
}

[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<IntegrationTestFixture>;
