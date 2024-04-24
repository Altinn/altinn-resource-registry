using Microsoft.Extensions.DependencyInjection;

namespace Altinn.ServiceDefaults.Npgsql;

public interface INpgsqlDatabaseBuilder
{
    public IServiceCollection Services { get; }

    public string ConnectionName { get; }

    public object? ServiceKey { get; }
}
