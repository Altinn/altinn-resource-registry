using Microsoft.Extensions.DependencyInjection;

namespace Altinn.ServiceDefaults.Npgsql;

internal class NpgsqlDatabaseBuilder : INpgsqlDatabaseBuilder
{
    public IServiceCollection Services { get; }

    public string ConnectionName { get; }

    public object? ServiceKey { get; }

    public NpgsqlDatabaseBuilder(IServiceCollection services, string connectionName, object? serviceKey)
    {
        Services = services;
        ConnectionName = connectionName;
        ServiceKey = serviceKey;
    }
}
