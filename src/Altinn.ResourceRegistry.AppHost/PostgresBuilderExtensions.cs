using Npgsql;
using System.Security.Cryptography;

namespace Altinn.ResourceRegistry.AppHost;


public static class PostgresBuilderExtensions
{
    public static IResourceBuilder<T> WithPersistentData<T>(this IResourceBuilder<T> builder, string? volumeName = null)
        where T: ContainerResource, IPostgresParentResource
    {
        volumeName ??= $"{builder.Resource.Name}-data";
        return builder.WithVolumeMount(volumeName, "/var/lib/postgresql/data", type: VolumeMountType.Named, isReadOnly: false);
    }

    public static IResourceBuilder<AltinnPostgresDatabaseResource> WithAltinnDatabase<T>(
        this IResourceBuilder<T> builder, 
        string databaseName, 
        string? resourceName = null, 
        DbUser applicationUser = default,
        DbUser migratorUser = default)
        where T: IPostgresParentResource
    {
        var postgresDatabase = new AltinnPostgresDatabaseResource(resourceName, builder.Resource, databaseName, applicationUser, migratorUser);
        return builder.ApplicationBuilder.AddResource(postgresDatabase);
    }

    public static IResourceBuilder<TDestination> WithReference<TDestination>(
        this IResourceBuilder<TDestination> builder, 
        IResourceBuilder<AltinnPostgresDatabaseResource> source)
        where TDestination : IResourceWithEnvironment
    {
        builder.WithEnvironment(ctx =>
        {
            source.Resource.AddToEnv(ctx.EnvironmentVariables);
        });

        return builder;
    }
}

public class AltinnPostgresDatabaseResource 
    : Resource
{
    private const string DbEnvironmentName = "Altinn__Npgsql__";
    private const string ConnectionStringEnvironmentName = "ConnectionStrings__";

    private readonly IPostgresParentResource _parent;
    private readonly string _databaseName;
    private readonly DbUser _applicationUser;
    private readonly DbUser _migratorUser;

    public AltinnPostgresDatabaseResource(
        string? resourceName,
        IPostgresParentResource postgreParentResource,
        string databaseName,
        DbUser applicationUser,
        DbUser migratorUser)
        : base(resourceName ?? $"{databaseName}.db")
    {
        _parent = postgreParentResource;
        _databaseName = databaseName;
        _applicationUser = EnsureUser(applicationUser, databaseName, "app");
        _migratorUser = EnsureUser(migratorUser, databaseName, "migrator");
    }

    public void AddToEnv(IDictionary<string, string> env, string? configName = null)
    {
        configName ??= _databaseName;

        var connectionString = _parent.GetConnectionString() ?? throw new DistributedApplicationException("Missing parent connection string");
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var dbPrefix = $"{DbEnvironmentName}{configName}__";

        env.Add($"{ConnectionStringEnvironmentName}{configName}_db_server", connectionString);

        env.Add($"{dbPrefix}Seed__Enabled", "true");

        env.Add($"{dbPrefix}Create__Enabled", "true");
        env.Add($"{dbPrefix}Create__DatabaseName", _databaseName);
        env.Add($"{dbPrefix}Create__DatabaseOwner", _migratorUser.Username!);
        env.Add($"{dbPrefix}Create__Roles__app__Name", _applicationUser.Username!);
        env.Add($"{dbPrefix}Create__Roles__app__Password", _applicationUser.Password!);
        env.Add($"{dbPrefix}Create__Roles__migrator__Name", _migratorUser.Username!);
        env.Add($"{dbPrefix}Create__Roles__migrator__Password", _migratorUser.Password!);

        builder.Database = _databaseName;
        builder.Username = _applicationUser.Username;
        builder.Password = _applicationUser.Password;
        env.Add($"{ConnectionStringEnvironmentName}{configName}_db", builder.ConnectionString);

        builder.Username = _migratorUser.Username;
        builder.Password = _migratorUser.Password;
        env.Add($"{ConnectionStringEnvironmentName}{configName}_db_migrator", builder.ConnectionString);
    }

    static DbUser EnsureUser(DbUser user, string databaseName, string defaultSuffix)
    {
        var username = user.Username ?? $"{databaseName}-{defaultSuffix}";
        var password = user.Password ?? HashPassword($"{databaseName}:{username}:{defaultSuffix}");

        return new DbUser(username, password);
    }

    static string HashPassword(string input)
    {
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash);
    }
}

public readonly struct DbUser(string username, string? password = null)
{
    public string? Username { get; } = username;

    public string? Password { get; } = password;
}
