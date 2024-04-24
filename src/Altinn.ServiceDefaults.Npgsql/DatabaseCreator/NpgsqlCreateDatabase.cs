using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text;

namespace Altinn.ServiceDefaults.Npgsql.DatabaseCreator;

internal partial class NpgsqlCreateDatabase 
    : INpgsqlDatabaseInitializer
{
    private readonly Settings _options;
    private readonly ILogger<NpgsqlCreateDatabase> _logger;

    public NpgsqlCreateDatabase(
        Settings options,
        ILogger<NpgsqlCreateDatabase> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task InitializeDatabaseAsync(INpgsqlConnectionProvider connectionProvider, IServiceProvider services, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_options.DatabaseName))
        {
            Log.MissingDatabaseName(_logger);
            return;
        }

        var conn = await connectionProvider.GetConnection(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = CreateSql();

        Log.CreatingDatabase(_logger, _options.DatabaseName);

        try
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == "42P04")
        {
            Log.DatabaseAlreadyExists(_logger, _options.DatabaseName);

            await using var updateCmd = conn.CreateCommand();
            updateCmd.CommandText = UpdateSql();

            await updateCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private string CreateSql()
    {
        var builder = new StringBuilder();
        builder.Append($"CREATE DATABASE \"{_options.DatabaseName}\"");

        if (_options.DatabaseOwner is not null)
        {
            builder.Append($" OWNER \"{_options.DatabaseOwner}\"");
        }

        return builder.ToString();
    }

    private string UpdateSql()
    {
        if (_options.DatabaseOwner is null)
        {
            return $"ALTER DATABASE \"{_options.DatabaseName}\" OWNER TO CURRENT_USER";
        }
        else
        {
            return $"ALTER DATABASE \"{_options.DatabaseName}\" OWNER TO \"{_options.DatabaseOwner}\"";
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Information, "Creating database {DatabaseName}.")]
        public static partial void CreatingDatabase(ILogger logger, string databaseName);

        [LoggerMessage(2, LogLevel.Error, "Database name is missing. Skipping database creation.")]
        public static partial void MissingDatabaseName(ILogger logger);

        [LoggerMessage(3, LogLevel.Information, "Database {DatabaseName} already exists. Skipping database creation.")]
        public static partial void DatabaseAlreadyExists(ILogger logger, string databaseName);
    }

    internal sealed record Settings(string DatabaseName, string? DatabaseOwner);
}
