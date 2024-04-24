using Microsoft.Extensions.DependencyInjection;

namespace Altinn.TestSeed;

/// <summary>
/// A builder to configure test-data seeding in the service collection.
/// </summary>
public interface ITestDataSeedServiceBuilder
{
    /// <summary>
    /// Gets the service collection.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// The service key, used if multiple instances of the same service are registered.
    /// </summary>
    object? ServiceKey { get; }

    /// <summary>
    /// The connection name. Used for <see cref="Microsoft.Extensions.Options.IOptionsMonitor{TOptions}.Get(string?)"/>.
    /// </summary>
    string ConnectionName { get; }
}

/// <summary>
/// A <see cref="ITestDataSeedServiceBuilder"/>.
/// </summary>
internal class TestDataSeedServiceBuilder
    : ITestDataSeedServiceBuilder
{
    /// <summary>
    /// Creates a new instance of <see cref="TestDataSeedServiceBuilder"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceKey">The service key.</param>
    /// <param name="connectionName">The connection name.</param>
    public TestDataSeedServiceBuilder(
        IServiceCollection services,
        object? serviceKey,
        string connectionName)
    {
        Services = services;
        ServiceKey = serviceKey;
        ConnectionName = connectionName;
    }

    /// <inheritdoc />
    public IServiceCollection Services { get; }

    /// <inheritdoc />
    public object? ServiceKey { get; }

    /// <inheritdoc />
    public string ConnectionName { get; }
}
