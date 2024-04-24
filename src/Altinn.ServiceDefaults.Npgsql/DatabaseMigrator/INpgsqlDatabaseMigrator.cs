namespace Altinn.ServiceDefaults.Npgsql.DatabaseMigrator;

public interface INpgsqlDatabaseMigrator
{
    Task MigrateDatabaseAsync(INpgsqlConnectionProvider connectionProvider, IServiceProvider scopedServices, CancellationToken cancellationToken);
}
