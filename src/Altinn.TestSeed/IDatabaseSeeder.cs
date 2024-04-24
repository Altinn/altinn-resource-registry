using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Altinn.TestSeed;

/// <summary>
/// A database seeder.
/// </summary>
public interface IDatabaseSeeder
{
    /// <summary>
    /// Seeds the database.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="scopedServices">A scoped service provider.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SeedDatabase(NpgsqlConnection connection, IServiceProvider scopedServices, CancellationToken cancellationToken);
}

/// <summary>
/// Implementation of <see cref="IDatabaseSeeder"/>.
/// </summary>
internal partial class DatabaseSeeder
    : IDatabaseSeeder
{
    private readonly Settings _settings;
    private readonly ILogger<DatabaseSeeder> _logger;

    /// <summary>
    /// Constructs a new <see cref="TestDataSeederHostedService"/> instance.
    /// </summary>
    /// <param name="serviceScopeFactory">The service scope factory.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The options monitor.</param>
    public DatabaseSeeder(
        Settings settings,
        ILogger<DatabaseSeeder> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task SeedDatabase(NpgsqlConnection connection, IServiceProvider scopedServices, CancellationToken cancellationToken)
    {
        Log.StartingDataSeeding(_logger);

        try
        {
            await SeedDataAsync(scopedServices, connection, cancellationToken);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            Log.DataSeedingCancelled(_logger);
            throw;
        }
        catch (Exception ex)
        {
            Log.DataSeedingFailed(_logger, ex);
            throw;
        }

        Log.DataSeedingCompleted(_logger);
    }

    private async Task SeedDataAsync(IServiceProvider services, NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        const string COLLECT_SAVEPOINT = "collect";

        var providers = services.GetKeyedServices<ITestDataSeederProvider>(_settings.ServiceKey).ToList();

        if (providers.Count == 0)
        {
            Log.NoProvidersConfigured(_logger);
            return;
        }

        await using var tx = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        await tx.SaveAsync(COLLECT_SAVEPOINT, cancellationToken);
        var seeders = new List<ITestDataSeeder>();

        foreach (var provider in providers)
        {
            {
                using var scope = provider.BeginLoggerScope(_logger);
                try
                {
                    await foreach (var seeder in provider.GetSeeders(connection, cancellationToken))
                    {
                        Log.GotSeeder(_logger, seeder.DisplayName, seeder.Order);
                        seeders.Add(seeder);
                    }
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new DataSeedingFailedException(provider, ex);
                }
            }

            await tx.RollbackAsync(COLLECT_SAVEPOINT, cancellationToken);
        }

        if (seeders.Count == 0)
        {
            Log.NoSeeders(_logger);
            return;
        }

        seeders.Sort((a, b) => a.Order.CompareTo(b.Order));

        await using var batch = connection.CreateBatch();
        batch.Transaction = tx;

        var builder = new BatchBuilder(batch);
        foreach (var seeder in seeders)
        {
            using var scope = seeder.BeginLoggerScope(_logger);
            try
            {
                await seeder.SeedData(builder, cancellationToken);
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DataSeedingFailedException(seeder, ex);
            }
        }

        await batch.ExecuteNonQueryAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    private static partial class Log
    {
        [LoggerMessage(0, LogLevel.Information, "Starting data seeding.")]
        public static partial void StartingDataSeeding(ILogger logger);

        [LoggerMessage(1, LogLevel.Information, "Data seeding completed.")]
        public static partial void DataSeedingCompleted(ILogger logger);

        [LoggerMessage(2, LogLevel.Error, "Data seeding failed.")]
        public static partial void DataSeedingFailed(ILogger logger, Exception exception);

        [LoggerMessage(3, LogLevel.Information, "Data seeding cancelled.")]
        public static partial void DataSeedingCancelled(ILogger logger);

        [LoggerMessage(4, LogLevel.Information, "No providers configured.")]
        public static partial void NoProvidersConfigured(ILogger logger);

        [LoggerMessage(5, LogLevel.Debug, "Got seeder {SeederName} with order {Order}.")]
        public static partial void GotSeeder(ILogger logger, string seederName, uint order);

        [LoggerMessage(6, LogLevel.Information, "No seeders provided, nothing to do.")]
        public static partial void NoSeeders(ILogger logger);
    }
    
    /// <summary>
    /// Settings for <see cref="DatabaseSeeder"/>.
    /// </summary>
    /// <param name="ServiceKey">The service key.</param>
    internal sealed record Settings(
        object? ServiceKey);
}
