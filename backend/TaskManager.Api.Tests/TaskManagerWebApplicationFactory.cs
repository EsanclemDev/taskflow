
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskManager.Api.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace TaskManager.Api.Tests;

public class TaskManagerWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection =
        new("Data Source=:memory:");

    public TaskManagerWebApplicationFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the original DbContext registration
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            // Remove PostgreSQL provider-specific services
            var postgresServices = services
                .Where(descriptor =>
                    descriptor.ServiceType == typeof(IDatabaseProvider))
                .ToList();

            foreach (var service in postgresServices)
            {
                services.Remove(service);
            }

            // Register SQLite for integration tests
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(_connection));
        });
    }

protected override Microsoft.Extensions.Hosting.IHost CreateHost(
    Microsoft.Extensions.Hosting.IHostBuilder builder)
{
    var host = base.CreateHost(builder);

    using var scope = host.Services.CreateScope();

    var db = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    db.Database.EnsureCreated();

    return host;
}

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}
