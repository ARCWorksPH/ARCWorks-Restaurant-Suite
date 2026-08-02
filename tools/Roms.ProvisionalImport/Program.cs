using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Roms.Infrastructure.Persistence;
using Roms.ProvisionalImport;

if (args.Length is < 2 or > 3 || args[0] is not ("preview" or "apply"))
{
    Console.Error.WriteLine(
        "Usage: Roms.ProvisionalImport preview <seed.json> | apply <seed.json> --confirm-empty-sandbox");
    return 2;
}

var command = args[0];
var sourcePath = Path.GetFullPath(args[1]);
if (!File.Exists(sourcePath))
{
    Console.Error.WriteLine("The seed JSON file does not exist.");
    return 2;
}

var (seed, sha256) = await ProvisionalSeedLoader.LoadAsync(sourcePath);
var preview = ProvisionalSeedValidator.Preview(seed, sha256);
Console.WriteLine(JsonSerializer.Serialize(preview, new JsonSerializerOptions { WriteIndented = true }));

if (command == "preview")
    return preview.IsValid ? 0 : 1;

if (args.Length != 3 || args[2] != "--confirm-empty-sandbox")
{
    Console.Error.WriteLine("Apply requires --confirm-empty-sandbox.");
    return 2;
}
if (!preview.IsValid)
{
    Console.Error.WriteLine("Apply refused because validation failed.");
    return 1;
}

var connectionString = Environment.GetEnvironmentVariable("ROMS_PROVISIONAL_IMPORT_CONNECTION");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("ROMS_PROVISIONAL_IMPORT_CONNECTION is required for apply.");
    return 2;
}

var connection = new MySqlConnectionStringBuilder(connectionString);
var localServers = new[] { "127.0.0.1", "localhost", "::1" };
if (!localServers.Contains(connection.Server, StringComparer.OrdinalIgnoreCase) ||
    !connection.Database.Contains("sandbox", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine(
        "Apply refused: the database must be local and its name must contain 'sandbox'.");
    return 2;
}

var options = new DbContextOptionsBuilder<RomsDbContext>()
    .UseMySQL(connection.ConnectionString, provider => provider.EnableRetryOnFailure())
    .AddInterceptors(new MariaDbMigrationLockInterceptor())
    .Options;
await using var db = new RomsDbContext(options);
await db.Database.MigrateAsync();
var result = await ProvisionalSeedImporter.ImportIntoEmptySandboxAsync(
    new CommandDbContextFactory(options),
    seed,
    preview,
    confirmEmptySandbox: true);
Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
return 0;

file sealed class CommandDbContextFactory(DbContextOptions<RomsDbContext> options)
    : IDbContextFactory<RomsDbContext>
{
    public RomsDbContext CreateDbContext() => new(options);

    public Task<RomsDbContext> CreateDbContextAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new RomsDbContext(options));
}
