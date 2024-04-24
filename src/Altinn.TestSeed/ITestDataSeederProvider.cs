using Npgsql;

namespace Altinn.TestSeed;

/// <summary>
/// A test-data seeder provider.
/// </summary>
public interface ITestDataSeederProvider
    : ITestDataSeederSource
{
    /// <summary>
    /// Gets the seeders.
    /// </summary>
    /// <param name="db">The database connection.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>An async enumerable of <see cref="ITestDataSeeder"/>.</returns>
    /// <remarks>
    /// The database is rolled back to before this method is called, such that any changes made to the database
    /// during the execution of this method are discarded.
    /// </remarks>
    IAsyncEnumerable<ITestDataSeeder> GetSeeders(NpgsqlConnection db, CancellationToken cancellationToken);
}
