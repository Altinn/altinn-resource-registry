using Altinn.ServiceDefaults;
using Altinn.ServiceDefaults.Npgsql;
using Altinn.ServiceDefaults.Npgsql.DatabaseMigrator;
using CommunityToolkit.Diagnostics;
using HealthChecks.NpgSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenTelemetry.Metrics;
using System.Data.Common;

namespace Microsoft.Extensions.Hosting;

public static class AltinnServiceDefaultsNpgsqlExtensions
{
    private static readonly ObjectFactory<NpgsqlDatabaseHostedService> _hostedServiceFactory
        = ActivatorUtilities.CreateFactory<NpgsqlDatabaseHostedService>([typeof(NpgsqlDatabaseHostedService.Settings)]);

    private static string DefaultConfigSectionName(string connectionName)
        => $"Altinn:Npgsql:{connectionName}";

    public static INpgsqlDatabaseBuilder AddAltinnPostgresDataSource(
        this IHostApplicationBuilder builder,
        object? serviceKey = null,
        Action<NpgsqlSettings>? configureSettings = null,
        Action<NpgsqlDataSourceBuilder>? configureDataSourceBuilder = null)
    {
        var serviceDescriptor = builder.GetAltinnServiceDescriptor();

        return AddAltinnPostgresDataSource(builder, $"{serviceDescriptor.Name}", serviceKey, configureSettings, configureDataSourceBuilder);
    }

    public static INpgsqlDatabaseBuilder AddAltinnPostgresDataSource(
        this IHostApplicationBuilder builder,
        string connectionName,
        object? serviceKey = null,
        Action<NpgsqlSettings>? configureSettings = null,
        Action<NpgsqlDataSourceBuilder>? configureDataSourceBuilder = null)
        => AddAltinnPostgresDataSource(builder, serviceKey, DefaultConfigSectionName(connectionName), configureSettings, connectionName, configureDataSourceBuilder: configureDataSourceBuilder);

    private static INpgsqlDatabaseBuilder AddAltinnPostgresDataSource(
        IHostApplicationBuilder builder,
        object? serviceKey,
        string configurationSectionName,
        Action<NpgsqlSettings>? configureSettings,
        string connectionName,
        Action<NpgsqlDataSourceBuilder>? configureDataSourceBuilder)
    {
        Guard.IsNotNull(builder);

        if (builder.Services.Any(s => s.ServiceType == typeof(NpgsqlDataSource) && s.ServiceKey == serviceKey))
        {
            return new NpgsqlDatabaseBuilder(builder.Services, connectionName, serviceKey);
        }

        NpgsqlSettings settings = new();
        builder.Configuration.GetSection(configurationSectionName).Bind(settings);

        if (builder.Configuration.GetConnectionString($"{connectionName}_db") is string connectionString)
        {
            settings.ConnectionString = connectionString;
            settings.Migrate.ConnectionString = connectionString;
            settings.Seed.ConnectionString = connectionString;
        }

        if (builder.Configuration.GetConnectionString($"{connectionName}_db_migrator") is string migratorConnectionString)
        {
            settings.Migrate.ConnectionString = migratorConnectionString;
            settings.Seed.ConnectionString = migratorConnectionString;
        }

        if (builder.Configuration.GetConnectionString($"{connectionName}_db_seed") is string seedConnectionString)
        {
            settings.Seed.ConnectionString = seedConnectionString;
        }

        if (builder.Configuration.GetConnectionString($"{connectionName}_db_server") is string serverConnectionString)
        {
            settings.Create.ConnectionString = serverConnectionString;
        }

        configureSettings?.Invoke(settings);

        builder.RegisterNpgsqlServices(settings, configurationSectionName, connectionName, serviceKey, configureDataSourceBuilder);

        if (settings.HealthChecks)
        {
            builder.TryAddHealthCheck(new HealthCheckRegistration(
                serviceKey is null ? "PostgreSql" : $"PostgreSql_{connectionName}",
                sp => new NpgSqlHealthCheck(
                    new NpgSqlHealthCheckOptions(serviceKey is null
                        ? sp.GetRequiredService<NpgsqlDataSource>()
                        : sp.GetRequiredKeyedService<NpgsqlDataSource>(serviceKey))),
                failureStatus: default,
                tags: default,
                timeout: default));
        }

        if (settings.Tracing)
        {
            builder.Services.AddOpenTelemetry()
                .WithTracing(traceProviderBuilder =>
                {
                    traceProviderBuilder.AddNpgsql();
                });
        }

        if (settings.Metrics)
        {
            builder.Services.AddOpenTelemetry()
                .WithMetrics(meterProviderBuilder =>
                {
                    double[] secondsBuckets = [0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10];

                    meterProviderBuilder
                        .AddMeter("Npgsql")
                        // Npgsql's histograms are in seconds, not milliseconds.
                        .AddView("db.client.commands.duration",
                            new ExplicitBucketHistogramConfiguration
                            {
                                Boundaries = secondsBuckets
                            })
                        .AddView("db.client.connections.create_time",
                            new ExplicitBucketHistogramConfiguration
                            {
                                Boundaries = secondsBuckets
                            });
                });
        }

        var dbBuilder = new NpgsqlDatabaseBuilder(builder.Services, connectionName, serviceKey);

        if (builder.Environment.IsDevelopment() && settings.Create.Enabled)
        {
            builder.Services.Configure<NpgsqlDatabaseHostedService.Options>(connectionName, options =>
            {
                options.CreateDatabase = true;
                options.CreateDatabaseConnectionString = settings.Create.ConnectionString;
            });

            if (string.IsNullOrEmpty(settings.Create.DatabaseName))
            {
                ThrowHelper.ThrowArgumentException("DatabaseName must be provided when Create.Enabled is true.");
            }

            foreach (var (key, role) in settings.Create.Roles)
            {
                if (string.IsNullOrEmpty(role.Name))
                {
                    ThrowHelper.ThrowArgumentException($"Role name must be provided when Create.Enabled is true. Role: {key}");
                }

                dbBuilder.CreateRole(role.Name, role.Password);
            }

            dbBuilder.CreateDatabase(settings.Create.DatabaseName!, settings.Create.DatabaseOwner);
        }

        if (settings.Migrate.Enabled)
        {
            builder.Services.Configure<NpgsqlDatabaseHostedService.Options>(connectionName, options =>
            {
                options.MigrateDatabase = true;
                options.MigrationConnectionString = settings.Migrate.ConnectionString;
            });

            //var connectionStringBuilder = new NpgsqlConnectionStringBuilder(settings.Migrate.ConnectionString);
            var migratorUser = new NpgsqlConnectionStringBuilder(settings.Migrate.ConnectionString).Username!;
            var appUser = new NpgsqlConnectionStringBuilder(settings.ConnectionString).Username!;

            builder.Services.Configure<NpgsqlDatabaseMigrationOptions>(connectionName, options =>
            {
                options.AppUser = appUser;
                options.MigratorUser = migratorUser;
            });
        }

        if (builder.Environment.IsDevelopment() && settings.Seed.Enabled)
        {
            builder.Services.Configure<NpgsqlDatabaseHostedService.Options>(connectionName, options =>
            {
                options.SeedDatabase = true;
                options.SeedConnectionString = settings.Seed.ConnectionString;
            });
        }

        return dbBuilder;
    }

    public static IServiceCollection AddNpgsqlHostedService(this IServiceCollection services, string connectionName, object? serviceKey = null)
    {
        var marker = services.SingleOrDefault(s => s.ServiceKey == serviceKey && s.ServiceType == typeof(NpgsqlDatabaseHostedServiceMarker));
        if (marker is not null)
        {
            return services;
        }

        services.AddKeyedSingleton(serviceKey, new NpgsqlDatabaseHostedServiceMarker());
        services.Add(ServiceDescriptor.Singleton<IHostedService>((s) =>
        {
            var settings = new NpgsqlDatabaseHostedService.Settings(serviceKey, connectionName);
            return _hostedServiceFactory(s, [settings]);
        }));

        return services;
    }

    private static void RegisterNpgsqlServices(
        this IHostApplicationBuilder builder,
        NpgsqlSettings settings,
        string configurationSectionName,
        string connectionName,
        object? serviceKey,
        Action<NpgsqlDataSourceBuilder>? configureDataSourceBuilder)
    {
        var dataSourceLifetime = ServiceLifetime.Singleton;
        var connectionLifetime = ServiceLifetime.Transient;

        var services = builder.Services;


        if (serviceKey is not null)
        {
            services.TryAdd(
                new ServiceDescriptor(
                    typeof(NpgsqlMultiHostDataSource),
                    serviceKey,
                    (sp, key) =>
                    {
                        ValidateConnection();

                        var dataSourceBuilder = new NpgsqlDataSourceBuilder(settings.ConnectionString);
                        dataSourceBuilder.UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>());

                        var setup = sp.GetKeyedServices<IConfigureOptions<NpgsqlDataSourceBuilder>>(serviceKey);
                        var post = sp.GetKeyedServices<IPostConfigureOptions<NpgsqlDataSourceBuilder>>(serviceKey);
                        var validators = sp.GetKeyedServices<IValidateOptions<NpgsqlDataSourceBuilder>>(serviceKey);

                        ConfigureDataSourceBuilder(dataSourceBuilder, setup, post, validators, configureDataSourceBuilder);
                        return dataSourceBuilder.BuildMultiHost();
                    },
                    dataSourceLifetime));

            services.TryAdd(
                new ServiceDescriptor(
                    typeof(NpgsqlDataSource),
                    serviceKey,
                    (sp, key) => sp.GetRequiredKeyedService<NpgsqlMultiHostDataSource>(key),
                    dataSourceLifetime));

            services.TryAdd(
                new ServiceDescriptor(
                    typeof(NpgsqlConnection),
                    serviceKey,
                    (sp, key) => sp.GetRequiredKeyedService<NpgsqlDataSource>(key).CreateConnection(),
                    connectionLifetime));

            services.TryAdd(
                new ServiceDescriptor(
                    typeof(DbDataSource),
                    serviceKey,
                    (sp, key) => sp.GetRequiredKeyedService<NpgsqlDataSource>(key),
                    dataSourceLifetime));

            services.TryAdd(
                new ServiceDescriptor(
                    typeof(DbConnection),
                    serviceKey,
                    (sp, key) => sp.GetRequiredKeyedService<NpgsqlConnection>(key),
                    connectionLifetime));
        }
        else
        {
            services.TryAdd(
                new ServiceDescriptor(
                    typeof(NpgsqlMultiHostDataSource),
                    serviceKey,
                    (sp, key) =>
                    {
                        ValidateConnection();

                        var dataSourceBuilder = new NpgsqlDataSourceBuilder(settings.ConnectionString);
                        dataSourceBuilder.UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>());

                        var setup = sp.GetServices<IConfigureOptions<NpgsqlDataSourceBuilder>>();
                        var post = sp.GetServices<IPostConfigureOptions<NpgsqlDataSourceBuilder>>();
                        var validators = sp.GetServices<IValidateOptions<NpgsqlDataSourceBuilder>>();

                        ConfigureDataSourceBuilder(dataSourceBuilder, setup, post, validators, configureDataSourceBuilder);
                        return dataSourceBuilder.BuildMultiHost();
                    },
                    dataSourceLifetime));

            services.TryAdd(
                new ServiceDescriptor(
                    typeof(NpgsqlDataSource),
                    (sp) => sp.GetRequiredService<NpgsqlMultiHostDataSource>(),
                    dataSourceLifetime));

            services.TryAdd(
                new ServiceDescriptor(
                    typeof(NpgsqlConnection),
                    sp => sp.GetRequiredService<NpgsqlDataSource>().CreateConnection(),
                    connectionLifetime));

            services.TryAdd(
                new ServiceDescriptor(
                    typeof(DbDataSource),
                    sp => sp.GetRequiredService<NpgsqlDataSource>(),
                    dataSourceLifetime));

            services.TryAdd(
                new ServiceDescriptor(
                    typeof(DbConnection),
                    sp => sp.GetRequiredService<NpgsqlConnection>(),
                    connectionLifetime));
        }

        void ValidateConnection()
        {
            if (string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                ThrowHelper.ThrowInvalidOperationException(
                    $"ConnectionString is missing. It should be provided in 'ConnectionStrings:{connectionName}_db' or under the 'ConnectionString' key in '{configurationSectionName}' configuration section.");
            }
        }

        static void ConfigureDataSourceBuilder(
            NpgsqlDataSourceBuilder builder,
            IEnumerable<IConfigureOptions<NpgsqlDataSourceBuilder>> setups,
            IEnumerable<IPostConfigureOptions<NpgsqlDataSourceBuilder>> post,
            IEnumerable<IValidateOptions<NpgsqlDataSourceBuilder>> validators,
            Action<NpgsqlDataSourceBuilder>? configureDataSourceBuilder)
        {
            foreach (var setup in setups)
            {
                setup.Configure(builder);
            }

            configureDataSourceBuilder?.Invoke(builder);

            foreach (var postConfigure in post)
            {
                postConfigure.PostConfigure(Options.Options.DefaultName, builder);
            }

            List<string>? failures = null;
            foreach (var validate in validators)
            {
                ValidateOptionsResult result = validate.Validate(Options.Options.DefaultName, builder);
                if (result is not null && result.Failed)
                {
                    failures ??= new List<string>();
                    failures.AddRange(result.Failures);
                }

                if (failures is { Count: > 0 })
                {
                    throw new OptionsValidationException(Options.Options.DefaultName, typeof(NpgsqlDataSourceBuilder), failures);
                }
            }
        }
    }

    private class NpgsqlDatabaseHostedServiceMarker
    {
    }
}
