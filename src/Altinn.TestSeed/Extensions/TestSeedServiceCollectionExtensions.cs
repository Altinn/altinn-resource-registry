using Altinn.TestSeed;
using Altinn.TestSeed.FileSystem;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring test-data seeding in the service collection.
/// </summary>
public static class TestSeedServiceCollectionExtensions 
{
    private static readonly ObjectFactory<SeedDataDirectoryTestDataSeederProvider> _dirProviderFactory
        = ActivatorUtilities.CreateFactory<SeedDataDirectoryTestDataSeederProvider>([typeof(SeedDataDirectorySettings)]);

    private static readonly ObjectFactory<DatabaseSeeder> _rootSeederFactory
        = ActivatorUtilities.CreateFactory<DatabaseSeeder>([typeof(DatabaseSeeder.Settings)]);

    /// <summary>
    /// Add a hosted service for seeding test data.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>A <see cref="ITestDataSeedServiceBuilder"/>.</returns>
    public static ITestDataSeedServiceBuilder AddTestDataSeeding(
        this IServiceCollection services,
        object? serviceKey,
        string connectionName)
    {
        var settings = new DatabaseSeeder.Settings(serviceKey);

        services.TryAddKeyedSingleton<IDatabaseSeeder>(serviceKey, (services, _) =>
        {
            return _rootSeederFactory(services, [settings]);
        });

        return new TestDataSeedServiceBuilder(services, serviceKey, connectionName);
    }

    /// <summary>
    /// Add a test data seeder provider.
    /// </summary>
    /// <typeparam name="T">The provider type.</typeparam>
    /// <param name="builder">The <see cref="ITestDataSeedServiceBuilder"/>.</param>
    /// <returns><paramref name="builder"/>.</returns>
    public static ITestDataSeedServiceBuilder TryAddProvider<T>(this ITestDataSeedServiceBuilder builder)
        where T : class, ITestDataSeederProvider
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.KeyedTransient<ITestDataSeederProvider, T>(builder.ServiceKey));
        
        return builder;
    }

    /// <summary>
    /// Add a test data seeder provider.
    /// </summary>
    /// <param name="builder">The <see cref="ITestDataSeedServiceBuilder"/>.</param>
    /// <param name="type">The provider type.</param>
    /// <returns><paramref name="builder"/>.</returns>
    public static ITestDataSeedServiceBuilder TryAddProvider(this ITestDataSeedServiceBuilder builder, Type type)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.KeyedTransient(typeof(ITestDataSeederProvider), builder.ServiceKey, type));

        return builder;
    }

    /// <summary>
    /// Add a test data seeder provider.
    /// </summary>
    /// <param name="builder">The <see cref="ITestDataSeedServiceBuilder"/>.</param>
    /// <param name="factory">A factory function that creates the provider.</param>
    /// <returns><paramref name="builder"/>.</returns>
    public static ITestDataSeedServiceBuilder AddProvider(this ITestDataSeedServiceBuilder builder, Func<IServiceProvider, object?, ITestDataSeederProvider> factory)
    {
        builder.Services.Add(ServiceDescriptor.KeyedTransient(builder.ServiceKey, factory));

        return builder;
    }

    /// <summary>
    /// Add a test data seeder provider that seeds data from a directory.
    /// </summary>
    /// <param name="builder">The <see cref="ITestDataSeedServiceBuilder"/>.</param>
    /// <param name="fileProvider">The file provider, rooted in the directory which contains the seed data.</param>
    /// <returns><paramref name="builder"/>.</returns>
    public static ITestDataSeedServiceBuilder SeedFromDirectory(
        this ITestDataSeedServiceBuilder builder,
        Func<IServiceProvider, IFileProvider> fileProvider)
    {
        return builder.AddProvider((services, _) =>
        {
            var options = new SeedDataDirectorySettings
            {
                FileProvider = fileProvider(services),
            };

            return _dirProviderFactory(services, [options]);
        });
    }

    /// <summary>
    /// Add a test data seeder provider that seeds data from a directory.
    /// </summary>
    /// <param name="builder">The <see cref="ITestDataSeedServiceBuilder"/>.</param>
    /// <param name="fileProvider">The file provider, rooted in the directory which contains the seed data.</param>
    /// <returns><paramref name="builder"/>.</returns>
    public static ITestDataSeedServiceBuilder SeedFromDirectory(
        this ITestDataSeedServiceBuilder builder,
        IFileProvider fileProvider)
    {
        return builder.SeedFromDirectory((_) => fileProvider);
    }

    /// <summary>
    /// Add a test data seeder provider that seeds data from a directory.
    /// </summary>
    /// <param name="builder">The <see cref="ITestDataSeedServiceBuilder"/>.</param>
    /// <param name="directoryPath">The path to the directory which contains the seed data.</param>
    /// <returns><paramref name="builder"/>.</returns>
    public static ITestDataSeedServiceBuilder SeedFromDirectory(
        this ITestDataSeedServiceBuilder builder,
        Func<IServiceProvider, string> directoryPath)
    {
        return builder.SeedFromDirectory(services =>
        {
            var fileProvider = new PhysicalFileProvider(directoryPath(services));

            return fileProvider;
        });
    }

    /// <summary>
    /// Add a test data seeder provider that seeds data from a directory.
    /// </summary>
    /// <param name="builder">The <see cref="ITestDataSeedServiceBuilder"/>.</param>
    /// <param name="directoryPath">The path to the directory which contains the seed data.</param>
    /// <returns><paramref name="builder"/>.</returns>
    public static ITestDataSeedServiceBuilder SeedFromDirectory(
        this ITestDataSeedServiceBuilder builder,
        string directoryPath)
    {
        var fileProvider = new PhysicalFileProvider(directoryPath);

        return builder.SeedFromDirectory(fileProvider);
    }
}
