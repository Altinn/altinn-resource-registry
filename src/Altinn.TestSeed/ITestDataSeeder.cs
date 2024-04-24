namespace Altinn.TestSeed;

/// <summary>
/// A test-data seeder.
/// </summary>
public interface ITestDataSeeder
    : ITestDataSeederSource
{
    /// <summary>
    /// Gets the order in which the seeder should be executed.
    /// </summary>
    uint Order { get; }

    /// <summary>
    /// Seeds data into the database.
    /// </summary>
    /// <param name="batch">The database batch.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    Task SeedData(BatchBuilder batch, CancellationToken cancellationToken);
}
