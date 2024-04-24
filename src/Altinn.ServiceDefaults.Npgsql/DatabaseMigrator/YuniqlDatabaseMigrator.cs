using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yuniql.Core;
using Yuniql.Extensibility;
using Yuniql.PostgreSql;

namespace Altinn.ServiceDefaults.Npgsql.DatabaseMigrator;

internal partial class YuniqlDatabaseMigrator 
    : INpgsqlDatabaseMigrator
{
    private readonly IOptionsMonitor<YuniqlDatabaseMigratorOptions> _options;
    private readonly ILogger<YuniqlDatabaseMigrator> _logger;
    private readonly string _connectionName;

    public YuniqlDatabaseMigrator(
        IOptionsMonitor<YuniqlDatabaseMigratorOptions> options,
        ILogger<YuniqlDatabaseMigrator> logger,
        string connectionName)
    {
        _options = options;
        _logger = logger;
        _connectionName = connectionName;
    }

    public Task MigrateDatabaseAsync(INpgsqlConnectionProvider connectionProvider, IServiceProvider services, CancellationToken cancellationToken)
        => Task.Factory.StartNew(
            () => MigrateDatabaseSync(connectionProvider.ConnectionString, services), 
            cancellationToken, 
            TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously, 
            TaskScheduler.Default);

    public void MigrateDatabaseSync(string connectionString, IServiceProvider services)
    {
        var options = _options.Get(_connectionName);

        var traceService = services.GetRequiredService<ITraceService>();
        var dataService = new PostgreSqlDataService(traceService);
        var bulkImportService = new PostgreSqlBulkImportService(traceService);

        // setup config
        var config = Configuration.Instance;
        config.Workspace = options.Workspace;
        config.IsDebug = false;
        config.Platform = SUPPORTED_DATABASES.POSTGRESQL;
        config.ConnectionString = connectionString;
        config.CommandTimeout = 600;
        config.TargetVersion = null;
        config.IsAutoCreateDatabase = false;
        config.Tokens = [.. options.Tokens];
        config.BulkSeparator = ",";
        config.BulkBatchSize = 0;
        config.Environment = options.Environment;
        config.MetaSchemaName = null;
        config.MetaTableName = null;
        config.TransactionMode = "session";
        config.IsContinueAfterFailure = null;
        config.IsRequiredClearedDraft = false;
        config.IsForced = false;
        config.IsVerifyOnly = false;
        config.AppliedByTool = "Altinn.ServiceDefaults.Npgsql";
        config.AppliedByToolVersion = typeof(YuniqlDatabaseMigrator).Assembly.GetName().Version?.ToString() ?? "";
        config.IsInitialized = false;

        var factory = new MigrationServiceFactory(traceService);
        var service = factory.Create(dataService, bulkImportService);

        Log.RunningYuniqlMigrations(_logger);
        service.Run();
        Log.YuniqlMigrationsComplete(_logger);
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Information, "Running Yuniql migrations.")]
        public static partial void RunningYuniqlMigrations(ILogger logger);

        [LoggerMessage(2, LogLevel.Information, "Yuniql migrations complete.")]
        public static partial void YuniqlMigrationsComplete(ILogger logger);
    }
}
