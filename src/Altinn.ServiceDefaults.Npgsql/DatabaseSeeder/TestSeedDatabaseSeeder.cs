using Altinn.TestSeed;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.ServiceDefaults.Npgsql.DatabaseSeeder;

internal class TestSeedDatabaseSeeder
    : INpgsqlDatabaseSeeder
{
    private readonly Settings _settings;

    public TestSeedDatabaseSeeder(
        Settings settings)
    {
        _settings = settings;
    }

    public async Task SeedDatabaseAsync(INpgsqlConnectionProvider connectionProvider, IServiceProvider scopedServices, CancellationToken cancellationToken)
    {
        var conn = await connectionProvider.GetConnection(cancellationToken);
        var seeder = scopedServices.GetRequiredKeyedService<IDatabaseSeeder>(_settings.ServiceKey);
        await seeder.SeedDatabase(conn, scopedServices, cancellationToken);
    }

    internal sealed record Settings(
        object? ServiceKey,
        string? ConnectionName);
}
