using Microsoft.EntityFrameworkCore;
using Npgsql;
using TaskManager.Api.Data;
using Xunit;

namespace TaskManager.Api.Tests;

/// <summary>
/// Cada ejecución de esta clase usa una base NUEVA, aleatoria y local.
/// Nunca borra ni modifica taskmanager_beta ni taskmanager_refresh_tests.
/// </summary>
public sealed class PostgresTestDatabase : IAsyncLifetime
{
    private readonly string _databaseName =
        $"taskflow_test_{Guid.NewGuid():N}";

    private readonly NpgsqlConnectionStringBuilder _adminConnection;
    private readonly NpgsqlConnectionStringBuilder _testConnection;
    private bool _created;

    public PostgresTestDatabase()
    {
        // Las credenciales locales no deben quedar en código ni en Git.
        var configured = Environment.GetEnvironmentVariable("TASKFLOW_TEST_POSTGRES");
        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new InvalidOperationException(
                "Configura TASKFLOW_TEST_POSTGRES con la conexión local de pruebas " +
                "antes de ejecutar RefreshTokenPostgresTests.");
        }

        _adminConnection = new NpgsqlConnectionStringBuilder(configured);

        // Barrera de seguridad: jamás abrir una conexión de test a un host remoto.
        if (!string.Equals(_adminConnection.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            && _adminConnection.Host != "127.0.0.1"
            && _adminConnection.Host != "::1")
        {
            throw new InvalidOperationException(
                "Los tests de PostgreSQL solo pueden ejecutarse contra localhost.");
        }

        _adminConnection.Database = "postgres";
        _adminConnection.Pooling = false;

        _testConnection = new NpgsqlConnectionStringBuilder(_adminConnection.ConnectionString)
        {
            Database = _databaseName,
            Pooling = false
        };
    }

    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_testConnection.ConnectionString)
            .Options;
        return new ApplicationDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await using (var admin = new NpgsqlConnection(_adminConnection.ConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand(
                $"CREATE DATABASE \"{_databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
            _created = true;
        }

        // Migrate, no EnsureCreated: incluye IsDemo y DemoExpiresAt.
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (!_created)
            return;

        // Solo se borra la BD generada por esta instancia.
        await using var admin = new NpgsqlConnection(_adminConnection.ConnectionString);
        await admin.OpenAsync();
        await using var drop = new NpgsqlCommand(
            $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }
}
