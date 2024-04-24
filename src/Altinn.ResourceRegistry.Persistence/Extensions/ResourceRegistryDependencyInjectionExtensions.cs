using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Core.Enums;
using Altinn.ResourceRegistry.Persistence;
using Altinn.ResourceRegistry.Persistence.Configuration;
using Altinn.ServiceDefaults.Npgsql;
using Altinn.ServiceDefaults.Npgsql.DatabaseMigrator;
using Altinn.ServiceDefaults.Npgsql.DatabaseSeeder;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding resource registry services to the dependency injection container.
/// </summary>
public static class ResourceRegistryDependencyInjectionExtensions 
{
    /// <summary>
    /// Registers resource registry persistence services with the dependency injection container.
    /// </summary>
    /// <param name="builder">The <see cref="IServiceCollection"/>.</param>
    /// <returns><paramref name="builder"/> for further chaining.</returns>
    public static IHostApplicationBuilder AddResourceRegistryPersistence(
        this IHostApplicationBuilder builder)
    {
        builder.AddResourceRegistryRepository();
        builder.AddAccessListRepository();
        builder.AddResourceRegistryPolicyRepository();

        return builder;
    }

    /// <summary>
    /// Registers a <see cref="IResourceRegistryRepository"/> with the dependency injection container.
    /// </summary>
    /// <param name="builder">The <see cref="IServiceCollection"/>.</param>
    /// <returns><paramref name="builder"/> for further chaining.</returns>
    public static IHostApplicationBuilder AddResourceRegistryRepository(
        this IHostApplicationBuilder builder)
    {
        builder.AddDatabase();

        builder.Services.TryAddTransient<IResourceRegistryRepository, ResourceRegistryRepository>();

        return builder;
    }

    /// <summary>
    /// Registers a <see cref="IAccessListsRepository"/> with the dependency injection container.
    /// </summary>
    /// <param name="builder">The <see cref="IServiceCollection"/>.</param>
    /// <returns><paramref name="builder"/> for further chaining.</returns>
    public static IHostApplicationBuilder AddAccessListRepository(
        this IHostApplicationBuilder builder)
    {
        builder.AddDatabase();
        builder.Services.TryAddSingleton(TimeProvider.System);

        builder.Services.TryAddTransient<IAccessListsRepository, AccessListsRepository>();

        return builder;
    }

    /// <summary>
    /// Registers a <see cref="IPolicyRepository"/> with the dependency injection container.
    /// </summary>
    /// <param name="builder">The <see cref="IServiceCollection"/>.</param>
    /// <returns><paramref name="builder"/> for further chaining.</returns>
    public static IHostApplicationBuilder AddResourceRegistryPolicyRepository(
        this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<AzureStorageConfiguration>()
            .Validate(s => !string.IsNullOrEmpty(s.ResourceRegistryAccountKey), "account key cannot be null or empty")
            .Validate(s => !string.IsNullOrEmpty(s.ResourceRegistryAccountName), "account name cannot be null or empty")
            .Validate(s => !string.IsNullOrEmpty(s.ResourceRegistryContainer), "container cannot be null or empty")
            .Validate(s => !string.IsNullOrEmpty(s.ResourceRegistryBlobEndpoint), "blob endpoint cannot be null or empty");

        builder.Services.TryAddSingleton<IPolicyRepository, PolicyRepository>();

        return builder;
    }

    private static IHostApplicationBuilder AddDatabase(this IHostApplicationBuilder builder)
    {
        if (builder.Services.Any(s => s.ServiceType == typeof(Marker)))
        {
            return builder;
        }

        builder.Services.AddSingleton<Marker>();
        ////builder.Services.AddOptions<PostgreSQLSettings>()
        ////    .Validate(s => !string.IsNullOrEmpty(s.ConnectionString), "connection string cannot be null or empty")
        ////    .Validate(s => !string.IsNullOrEmpty(s.AuthorizationDbPwd), "connection string password be null or empty");

        builder.AddAltinnPostgresDataSource(serviceKey: "resource_registry")
            .MapEnum<ResourceType>("resourceregistry.resourcetype")
            .AddYuniqlMigrations((services, config) =>
            {
                config.Workspace = GetWorkspacePath(services);
            })
            .SeedTestData((cfg) =>
            {
                cfg.SeedFromDirectory(services =>
                {
                    var workspacePath = GetWorkspacePath(services);
                    return Path.Combine(workspacePath, "../TestData");
                });
            });

        return builder;

        static string GetWorkspacePath(IServiceProvider services)
        {
            var configuration = services.GetRequiredService<IConfiguration>();
            var isDevelopment = services.GetRequiredService<IHostEnvironment>().IsDevelopment();

            string workspacePath = Path.Combine(Environment.CurrentDirectory, configuration.GetValue<string>("PostgreSQLSettings:WorkspacePath")!);
            if (isDevelopment)
            {
                workspacePath = Path.Combine(Directory.GetParent(Environment.CurrentDirectory)!.FullName, configuration.GetValue<string>("PostgreSQLSettings:WorkspacePath")!);
            }

            return workspacePath;
        }
    }

    private class Marker
    {
    }
}
