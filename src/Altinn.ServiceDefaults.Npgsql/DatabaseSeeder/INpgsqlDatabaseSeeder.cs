using Npgsql;

namespace Altinn.ServiceDefaults.Npgsql.DatabaseSeeder;

public interface INpgsqlDatabaseSeeder
{
    Task SeedDatabaseAsync(
        INpgsqlConnectionProvider connectionProvider,
        IServiceProvider scopedServices,
        CancellationToken cancellationToken);
}
