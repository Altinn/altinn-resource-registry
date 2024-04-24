using Altinn.ServiceDefaults.Npgsql.DatabaseCreator;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Altinn.ServiceDefaults.Npgsql;

public static class NpgsqlDatabaseCreatorServiceCollectionExtensions 
{
    private static readonly ObjectFactory<NpgsqlCreateDatabase> _dbFactory = ActivatorUtilities.CreateFactory<NpgsqlCreateDatabase>([typeof(NpgsqlCreateDatabase.Settings)]);
    private static readonly ObjectFactory<NpgsqlCreateRole> _roleFactory = ActivatorUtilities.CreateFactory<NpgsqlCreateRole>([typeof(NpgsqlCreateRole.Settings)]);

    public static INpgsqlDatabaseBuilder CreateDatabase(this INpgsqlDatabaseBuilder builder, Action<IServiceProvider, CreateDatabaseSettings> configure)
    {
        builder.Services.AddNpgsqlHostedService(builder.ConnectionName, builder.ServiceKey);
        builder.Services.AddKeyedSingleton<INpgsqlDatabaseInitializer>(builder.ServiceKey, (services, key) =>
        {
            CreateDatabaseSettings settings = new();
            configure(services, settings);

            return _dbFactory(services, [settings.IntoSettings()]);
        });

        return builder;
    }

    public static INpgsqlDatabaseBuilder CreateDatabase(this INpgsqlDatabaseBuilder builder, Action<CreateDatabaseSettings> configure)
    {
        return builder.CreateDatabase((_, options) => configure(options));
    }

    public static INpgsqlDatabaseBuilder CreateDatabase(this INpgsqlDatabaseBuilder builder, string databaseName, string? databaseOwner)
    {
        Guard.IsNotNullOrEmpty(databaseName);

        return builder.CreateDatabase(options =>
        {
            options.DatabaseName = databaseName;
            options.DatabaseOwner = databaseOwner;
        });
    }

    public static INpgsqlDatabaseBuilder CreateRole(this INpgsqlDatabaseBuilder builder, Action<IServiceProvider, CreateRoleSettings> configure)
    {
        builder.Services.AddNpgsqlHostedService(builder.ConnectionName, builder.ServiceKey);
        builder.Services.AddKeyedSingleton<INpgsqlDatabaseInitializer>(builder.ServiceKey, (services, key) =>
        {
            CreateRoleSettings settings = new();
            configure(services, settings);

            return _roleFactory(services, [settings.IntoSettings()]);
        });

        return builder;
    }

    public static INpgsqlDatabaseBuilder CreateRole(this INpgsqlDatabaseBuilder builder, Action<CreateRoleSettings> configure)
    {
        return builder.CreateRole((_, options) => configure(options));
    }

    public static INpgsqlDatabaseBuilder CreateRole(this INpgsqlDatabaseBuilder builder, string name, string? password)
    {
        Guard.IsNotNullOrEmpty(name);

        return builder.CreateRole(options =>
        {
            options.Name = name;
            options.Password = password;
        });
    }

    public sealed class CreateDatabaseSettings
    {
        public string? DatabaseName { get; set; }

        public string? DatabaseOwner { get; set; }

        internal NpgsqlCreateDatabase.Settings IntoSettings()
        {
            Guard.IsNotNullOrEmpty(DatabaseName);

            return new(DatabaseName, DatabaseOwner);
        }
    }

    public sealed class CreateRoleSettings
    {
        public string? Name { get; set; }

        public string? Password { get; set; }

        internal NpgsqlCreateRole.Settings IntoSettings()
        {
            Guard.IsNotNullOrEmpty(Name);

            return new(Name, Password);
        }
    }
}
