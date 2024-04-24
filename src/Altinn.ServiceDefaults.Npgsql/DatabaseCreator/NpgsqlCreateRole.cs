using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text;

namespace Altinn.ServiceDefaults.Npgsql.DatabaseCreator;

internal partial class NpgsqlCreateRole
    : INpgsqlDatabaseInitializer
{
    private readonly Settings _options;
    private readonly ILogger<NpgsqlCreateRole> _logger;

    public NpgsqlCreateRole(
        Settings options,
        ILogger<NpgsqlCreateRole> logger)
    {
        _options = options;
        _logger = logger;
    }

    public DatabaseCreationOrder Order => DatabaseCreationOrder.CreateRoles;

    public async Task InitializeDatabaseAsync(INpgsqlConnectionProvider connectionProvider, IServiceProvider services, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_options.Name))
        {
            Log.MissingRoleName(_logger);
            return;
        }

        var conn = await connectionProvider.GetConnection(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = CreateRoleSql();

        Log.CreatingRole(_logger, _options.Name);

        try
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == "42710")
        {
            Log.RoleAlreadyExists(_logger, _options.Name);
            await using var updateCmd = conn.CreateCommand();
            updateCmd.CommandText = UpdateRoleSql();

            await updateCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private string CreateRoleSql()
    {
        var builder = new StringBuilder();
        builder.Append($"CREATE ROLE \"{_options.Name}\"");

        if (!string.IsNullOrEmpty(_options.Password))
        {
            builder.Append($" WITH LOGIN PASSWORD '{_options.Password}'");
        } 
        else
        {
            builder.Append(" WITH NOLOGIN PASSWORD NULL");
        }

        return builder.ToString();
    }

    private string UpdateRoleSql()
    {
        var builder = new StringBuilder();
        builder.Append($"ALTER ROLE \"{_options.Name}\"");

        if (!string.IsNullOrEmpty(_options.Password))
        {
            builder.Append($" WITH LOGIN PASSWORD '{_options.Password}'");
        }
        else
        {
            builder.Append(" WITH NOLOGIN PASSWORD NULL");
        }

        return builder.ToString();
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Information, "Creating role {RoleName}.")]
        public static partial void CreatingRole(ILogger logger, string roleName);

        [LoggerMessage(2, LogLevel.Error, "Role name is missing. Skipping role creation.")]
        public static partial void MissingRoleName(ILogger logger);

        [LoggerMessage(3, LogLevel.Information, "Role {RoleName} already exists. Updating role instead.")]
        public static partial void RoleAlreadyExists(ILogger logger, string roleName);
    }

    internal sealed record Settings(string Name, string? Password);
}
