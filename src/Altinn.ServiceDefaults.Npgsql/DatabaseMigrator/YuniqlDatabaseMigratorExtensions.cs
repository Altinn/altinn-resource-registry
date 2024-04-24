using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;
using Yuniql.Extensibility;

namespace Altinn.ServiceDefaults.Npgsql.DatabaseMigrator;

public static class YuniqlDatabaseMigratorExtensions
{
    private static readonly ObjectFactory<YuniqlDatabaseMigrator> _migratorFactory =
        ActivatorUtilities.CreateFactory<YuniqlDatabaseMigrator>([typeof(string)]);

    public static INpgsqlDatabaseBuilder AddYuniqlMigrations(
        this INpgsqlDatabaseBuilder builder,
        Action<IServiceProvider, YuniqlDatabaseMigratorOptions> configure)
    {
        if (builder.Services.Any(x => x.ServiceType == typeof(Marker) && x.ServiceKey == builder.ServiceKey))
        {
            return builder;
        }

        var name = builder.ConnectionName;
        builder.Services.AddKeyedSingleton<Marker>(builder.ServiceKey);
        builder.Services.AddNpgsqlHostedService(builder.ConnectionName, builder.ServiceKey);
        builder.Services.TryAddSingleton<ITraceService, YuniqlTraceService>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<YuniqlDatabaseMigratorOptions>, SetFromNpgsqlDatabaseMigrationOptions>());

        if (builder.ServiceKey is not null)
        {
            var serviceKey = builder.ServiceKey;
            builder.Services.Add(ServiceDescriptor.KeyedSingleton<INpgsqlDatabaseMigrator>(builder.ServiceKey, (services, _) =>
            {
                return _migratorFactory(services, [name]);
            }));

            builder.Services.AddOptions<YuniqlDatabaseMigratorOptions>(name)
                .Configure((YuniqlDatabaseMigratorOptions opts, IServiceProvider services) =>
                {
                    var env = services.GetRequiredService<IHostEnvironment>();
                    opts.Environment = env.EnvironmentName;

                    configure(services, opts);
                });
        }
        else
        {
            builder.Services.Add(ServiceDescriptor.Singleton<INpgsqlDatabaseMigrator>((services) =>
            {
                return _migratorFactory(services, [name]);
            }));

            builder.Services.AddOptions<YuniqlDatabaseMigratorOptions>(name)
                .Configure((YuniqlDatabaseMigratorOptions opts, IServiceProvider services) =>
                {
                    var env = services.GetRequiredService<IHostEnvironment>();
                    opts.Environment = env.EnvironmentName;

                    configure(services, opts);
                });
        }

        return builder;
    }

    private class SetFromNpgsqlDatabaseMigrationOptions
        : IConfigureNamedOptions<YuniqlDatabaseMigratorOptions>
    {
        private readonly IOptionsMonitor<NpgsqlDatabaseMigrationOptions> _inner;

        public SetFromNpgsqlDatabaseMigrationOptions(
            IOptionsMonitor<NpgsqlDatabaseMigrationOptions> inner)
        {
            _inner = inner;
        }

        public void Configure(string? name, YuniqlDatabaseMigratorOptions options)
        {
            var innerOptions = _inner.Get(name);

            Assign(options, innerOptions);
        }

        public void Configure(YuniqlDatabaseMigratorOptions options)
        {
            var innerOptions = _inner.CurrentValue;

            Assign(options, innerOptions);
        }

        private static void Assign(YuniqlDatabaseMigratorOptions options, NpgsqlDatabaseMigrationOptions innerOptions)
        {
            if (!string.IsNullOrEmpty(innerOptions.MigratorUser))
            {
                options.Tokens.TryAdd("YUNIQL-USER", innerOptions.MigratorUser);
            }

            if (!string.IsNullOrEmpty(innerOptions.AppUser))
            {
                options.Tokens.TryAdd("APP-USER", innerOptions.AppUser);
            }
        }
    }

    private class Marker 
    { 
    }
}
