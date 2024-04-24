namespace Altinn.ServiceDefaults.Npgsql;

/// <summary>
/// Provides the client configuration settings for connecting to a PostgreSQL database using Npgsql.
/// </summary>
public sealed class NpgsqlSettings
{
    /// <summary>
    /// The connection string of the PostgreSQL database to connect to.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the database health check is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool HealthChecks { get; set; } = true;

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool Tracing { get; set; } = true;

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry metrics are enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool Metrics { get; set; } = true;

    /// <summary>
    /// Gets ot sets the settings for creating the database.
    /// </summary>
    public NpgsqlCreateDatabaseSettings Create { get; set; } = new();

    /// <summary>
    /// Gets or sets the settings for migrating the database.
    /// </summary>
    public NpgsqlMigrateDatabaseSettings Migrate { get; set; } = new();

    /// <summary>
    /// Gets or sets the settings for seeding the database.
    /// </summary>
    public NpgsqlSeedDatabaseSettings Seed { get; set; } = new();
}

public sealed class NpgsqlSeedDatabaseSettings
{
    public bool Enabled { get; set; }

    public string? ConnectionString { get; set; }
}

public sealed class NpgsqlMigrateDatabaseSettings
{
    public bool Enabled { get; set; } = true;

    public string? ConnectionString { get; set; }
}

public sealed class NpgsqlCreateDatabaseSettings
{
    public bool Enabled { get; set; }

    public string? ConnectionString { get; set; }

    public string? DatabaseName { get; set; }

    public string? DatabaseOwner { get; set; }

    public IDictionary<string, NpgsqlCreateRoleSettings> Roles { get; set; } = new Dictionary<string, NpgsqlCreateRoleSettings>();
}

public sealed class NpgsqlCreateRoleSettings
{
    public string? Name { get; set; }

    public string? Password { get; set; }
}
