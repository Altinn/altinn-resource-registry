using Npgsql;

namespace Altinn.ServiceDefaults.Npgsql.DatabaseCreator;

public interface INpgsqlDatabaseInitializer
{
    DatabaseCreationOrder Order => DatabaseCreationOrder.CreateDatabases;

    Task InitializeDatabaseAsync(
        INpgsqlConnectionProvider connectionProvider,
        IServiceProvider scopedServices,
        CancellationToken cancellationToken);
}
