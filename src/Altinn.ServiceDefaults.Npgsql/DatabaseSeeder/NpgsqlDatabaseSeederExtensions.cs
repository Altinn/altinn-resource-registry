using Altinn.TestSeed;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Altinn.ServiceDefaults.Npgsql.DatabaseSeeder;

public static class NpgsqlDatabaseSeederExtensions 
{
    private readonly static ObjectFactory<TestSeedDatabaseSeeder> _seederFactory
        = ActivatorUtilities.CreateFactory<TestSeedDatabaseSeeder>([typeof(TestSeedDatabaseSeeder.Settings)]);

    public static INpgsqlDatabaseBuilder SeedTestData(this INpgsqlDatabaseBuilder builder, Action<ITestDataSeedServiceBuilder> configure)
    {
        if (builder.Services.Any(s => s.ServiceKey == builder.ServiceKey && s.ServiceType == typeof(SeedTestDataMarker)))
        {
            return builder;
        }

        var settings = new TestSeedDatabaseSeeder.Settings(builder.ServiceKey, builder.ConnectionName);

        builder.Services.AddKeyedSingleton<SeedTestDataMarker>(builder.ServiceKey);
        builder.Services.AddKeyedSingleton<INpgsqlDatabaseSeeder>(builder.ServiceKey, (services, _) =>
        {
            return _seederFactory(services, [settings]);
        });

        var testDataSeedBuilder = builder.Services.AddTestDataSeeding(builder.ServiceKey, builder.ConnectionName);
        configure(testDataSeedBuilder);

        return builder;
    }

    private class SeedTestDataMarker
    {
    }
}
