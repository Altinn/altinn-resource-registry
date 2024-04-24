namespace Altinn.ServiceDefaults.Npgsql.DatabaseMigrator;

public class YuniqlDatabaseMigratorOptions
{
    public string? Environment { get; set; }

    public string? Workspace { get; set; }

    public Dictionary<string, string> Tokens { get; set; } = [];
}
