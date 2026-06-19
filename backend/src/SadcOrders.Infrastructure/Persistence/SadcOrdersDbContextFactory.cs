using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SadcOrders.Infrastructure.Persistence;

public class SadcOrdersDbContextFactory : IDesignTimeDbContextFactory<SadcOrdersDbContext>
{
    public SadcOrdersDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../SadcOrders.API"))
            .AddJsonFile("appsettings.json")
            .Build();

        var options = new DbContextOptionsBuilder<SadcOrdersDbContext>()
            .UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
            .Options;

        return new SadcOrdersDbContext(options);
    }
}
