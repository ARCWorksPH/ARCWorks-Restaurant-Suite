using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Roms.Infrastructure.Persistence;
using Testcontainers.MariaDb;

namespace Roms.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class MariaDbCollection : ICollectionFixture<MariaDbFixture>
{
    public const string Name = "MariaDB";
}

public sealed class MariaDbFixture : IAsyncLifetime
{
    private readonly MariaDbContainer container = new MariaDbBuilder("mariadb:11.4")
        .WithDatabase("roms_fixture")
        .WithUsername("root")
        .WithPassword($"roms-{Guid.NewGuid():N}")
        .Build();

    public Task InitializeAsync() => container.StartAsync();

    public async Task DisposeAsync() => await container.DisposeAsync();

    public async Task<MariaDbTestDatabase> CreateDatabaseAsync()
    {
        var databaseName = $"roms_{Guid.NewGuid():N}";
        await using (var connection = new MySqlConnection(container.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";
            await command.ExecuteNonQueryAsync();
        }

        var builder = new MySqlConnectionStringBuilder(container.GetConnectionString())
        {
            Database = databaseName,
            SslMode = MySqlSslMode.Disabled
        };
        var database = new MariaDbTestDatabase(container.GetConnectionString(), databaseName, builder.ConnectionString);
        await using var db = database.CreateContext();
        await db.Database.MigrateAsync();
        return database;
    }
}

public sealed class MariaDbTestDatabase(
    string administrativeConnectionString,
    string databaseName,
    string connectionString) : IAsyncDisposable
{
    private readonly DbContextOptions<RomsDbContext> options = new DbContextOptionsBuilder<RomsDbContext>()
        .UseMySQL(connectionString, provider => provider.EnableRetryOnFailure())
        .AddInterceptors(new MariaDbMigrationLockInterceptor())
        .Options;

    public string ConnectionString { get; } = connectionString;

    public RomsDbContext CreateContext() => new(options);

    public IDbContextFactory<RomsDbContext> CreateFactory() => new TestContextFactory(options);

    public async ValueTask DisposeAsync()
    {
        await using var connection = new MySqlConnection(administrativeConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS `{databaseName}`";
        await command.ExecuteNonQueryAsync();
    }

    private sealed class TestContextFactory(DbContextOptions<RomsDbContext> options)
        : IDbContextFactory<RomsDbContext>
    {
        public RomsDbContext CreateDbContext() => new(options);

        public Task<RomsDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new RomsDbContext(options));
    }
}
