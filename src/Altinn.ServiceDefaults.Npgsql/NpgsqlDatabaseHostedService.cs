using Altinn.ServiceDefaults.Npgsql.DatabaseCreator;
using Altinn.ServiceDefaults.Npgsql.DatabaseMigrator;
using Altinn.ServiceDefaults.Npgsql.DatabaseSeeder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Collections;
using System.Collections.Immutable;

namespace Altinn.ServiceDefaults.Npgsql;

internal partial class NpgsqlDatabaseHostedService
    : IHostedService, IHostedLifecycleService
{
    readonly Settings _settings;
    readonly AltinnServiceDescription _serviceDescription;
    readonly IHostEnvironment _environment;
    readonly IServiceScopeFactory _serviceScopeFactory;
    readonly IOptionsMonitor<Options> _options;
    readonly ImmutableArray<INpgsqlDatabaseInitializer> _databaseCreators;
    readonly ImmutableArray<INpgsqlDatabaseMigrator> _migrators;
    readonly ImmutableArray<INpgsqlDatabaseSeeder> _seeders;
    readonly ILogger<NpgsqlDatabaseHostedService> _logger;

    public NpgsqlDatabaseHostedService(
        Settings settings,
        AltinnServiceDescription serviceDescription,
        IHostEnvironment environment,
        IOptionsMonitor<Options> options,
        IServiceScopeFactory serviceScopeFactory,
        IServiceProvider services,
        ILogger<NpgsqlDatabaseHostedService> logger)
    {
        _settings = settings;
        _serviceDescription = serviceDescription;
        _environment = environment;
        _serviceScopeFactory = serviceScopeFactory;
        _options = options;
        _databaseCreators = services.GetKeyedServices<INpgsqlDatabaseInitializer>(settings.ServiceKey).OrderBy(x => x.Order).ToImmutableArray();
        _migrators = services.GetKeyedServices<INpgsqlDatabaseMigrator>(settings.ServiceKey).ToImmutableArray();
        _seeders = services.GetKeyedServices<INpgsqlDatabaseSeeder>(settings.ServiceKey).ToImmutableArray();
        _logger = logger;
    }

    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        var options = _options.Get(_settings.ConnectionName);
        using var scope = _logger.BeginScope(new LogScope(this));
        Log.StartingDatabaseService(_logger, _settings.ConnectionName!);

        if (!_environment.IsDevelopment())
        {
            Log.SkippingDatabaseCreationNotDevEnv(_logger);
        }
        else if (!options.CreateDatabase)
        {
            Log.SkippingDatabaseCreationConfig(_logger);
        }
        else
        {
            Log.CreatingDatabase(_logger);
            await CreateDatabases(options, cancellationToken);
        }

        if (!options.MigrateDatabase)
        {
            Log.SkippingDatabaseMigrationConfig(_logger);
        }
        else
        {
            Log.MigratingDatabase(_logger);
            await MigrateDatabases(options, cancellationToken);
        }

        if (!_environment.IsDevelopment())
        {
            Log.SkippingDatabaseSeedNotDevEnv(_logger);
        }
        else if (!options.CreateDatabase)
        {
            Log.SkippingDatabaseSeedConfig(_logger);
        }
        else
        {
            Log.SeedDatabase(_logger);
            await SeedDatabases(options, cancellationToken);
        }
    }

    private async Task CreateDatabases(Options options, CancellationToken cancellationToken)
    {
        if (_databaseCreators.Length == 0)
        {
            Log.NoDatabaseCreators(_logger);
            return;
        }

        var dbServerConnectionString = options.CreateDatabaseConnectionString;
        if (string.IsNullOrEmpty(dbServerConnectionString))
        {
            Log.NoLocalDatabaseServerConnectionStringFound(_logger);
            return;
        }

        await using var connectionProvider = new TempSharedNonPooledNpgsqlConnectionProvider(dbServerConnectionString);
        foreach (var creator in _databaseCreators)
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            cancellationToken.ThrowIfCancellationRequested();
            await creator.InitializeDatabaseAsync(connectionProvider, scope.ServiceProvider, cancellationToken);
        }
    }

    private async Task MigrateDatabases(Options options, CancellationToken cancellationToken)
    {
        if (_migrators.Length == 0)
        {
            Log.NoMigrators(_logger);
            return;
        }

        var migrationConnectionString = options.MigrationConnectionString;
        if (string.IsNullOrEmpty(migrationConnectionString))
        {
            Log.NoMigrationConnectionStringFound(_logger);
            return;
        }

        await using var connectionProvider = new TempSharedNonPooledNpgsqlConnectionProvider(migrationConnectionString);
        foreach (var migrator in _migrators)
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            cancellationToken.ThrowIfCancellationRequested();
            await migrator.MigrateDatabaseAsync(connectionProvider, scope.ServiceProvider, cancellationToken);
        }
    }

    private async Task SeedDatabases(Options options, CancellationToken cancellationToken)
    {
        if (_seeders.Length == 0)
        {
            Log.NoSeeders(_logger);
            return;
        }

        var seedConnectionString = options.SeedConnectionString;
        if (string.IsNullOrEmpty(seedConnectionString))
        {
            Log.NoLocalDatabaseSeedConnectionStringFound(_logger);
            return;
        }

        await using var connectionProvider = new TempSharedNonPooledNpgsqlConnectionProvider(seedConnectionString);
        foreach (var seeder in _seeders)
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            cancellationToken.ThrowIfCancellationRequested();
            await seeder.SeedDatabaseAsync(connectionProvider, scope.ServiceProvider, cancellationToken);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Information, "No local database server connection string found. Skipping database creation.")]
        public static partial void NoLocalDatabaseServerConnectionStringFound(ILogger logger);

        [LoggerMessage(2, LogLevel.Information, "No database creators found. Skipping database creation.")]
        public static partial void NoDatabaseCreators(ILogger logger);

        [LoggerMessage(3, LogLevel.Information, "No migrators found. Skipping database migration.")]
        public static partial void NoMigrators(ILogger logger);

        [LoggerMessage(4, LogLevel.Information, "No seeders found. Skipping database seeding.")]
        public static partial void NoSeeders(ILogger logger);

        [LoggerMessage(5, LogLevel.Information, "No database seed connection string found. Skipping database seeding.")]
        public static partial void NoLocalDatabaseSeedConnectionStringFound(ILogger logger);

        [LoggerMessage(6, LogLevel.Debug, "Skipping database creation as the environment is not development.")]
        public static partial void SkippingDatabaseCreationNotDevEnv(ILogger logger);

        [LoggerMessage(7, LogLevel.Debug, "Skipping database creation as the configuration is set to false.")]
        public static partial void SkippingDatabaseCreationConfig(ILogger logger);

        [LoggerMessage(8, LogLevel.Information, "Creating database.")]
        public static partial void CreatingDatabase(ILogger logger);

        [LoggerMessage(9, LogLevel.Debug, "Skipping database migration as the configuration is set to false.")]
        public static partial void SkippingDatabaseMigrationConfig(ILogger logger);

        [LoggerMessage(10, LogLevel.Information, "Migrating database.")]
        public static partial void MigratingDatabase(ILogger logger);

        [LoggerMessage(11, LogLevel.Debug, "Skipping database seeding as the environment is not development.")]
        public static partial void SkippingDatabaseSeedNotDevEnv(ILogger logger);

        [LoggerMessage(12, LogLevel.Debug, "Skipping database seeding as the configuration is set to false.")]
        public static partial void SkippingDatabaseSeedConfig(ILogger logger);

        [LoggerMessage(13, LogLevel.Information, "Seeding database.")]
        public static partial void SeedDatabase(ILogger logger);

        [LoggerMessage(14, LogLevel.Information, "No database migration connection string found. Skipping database migration")]
        public static partial void NoMigrationConnectionStringFound(ILogger logger);

        [LoggerMessage(15, LogLevel.Information, "Starting database service for connection {ConnectionName}.")]
        public static partial void StartingDatabaseService(ILogger logger, string connectionName);
    }

    internal sealed record Settings(
        object? ServiceKey,
        string? ConnectionName);

    internal class Options
    {
        public bool CreateDatabase { get; set; }

        public bool MigrateDatabase { get; set; }

        public bool SeedDatabase { get; set; }

        public string? CreateDatabaseConnectionString { get; set; }

        public string? MigrationConnectionString { get; set; }

        public string? SeedConnectionString { get; set; }
    }

    private readonly struct LogScope(NpgsqlDatabaseHostedService self)
        : IEnumerable<KeyValuePair<string, object?>>
    {
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            yield return KeyValuePair.Create("ConnectionName", (object?)self?._settings?.ConnectionName);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
