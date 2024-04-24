namespace Altinn.TestSeed;

/// <summary>
/// An exception that is thrown when data seeding fails.
/// </summary>
internal class DataSeedingFailedException : Exception
{
    /// <summary>
    /// Constructs a new <see cref="DataSeedingFailedException"/> instance.
    /// </summary>
    /// <param name="source">The <see cref="ITestDataSeederSource"/>.</param>
    /// <param name="innerException">The inner exception.</param>
    public DataSeedingFailedException(ITestDataSeederSource source, Exception innerException)
        : base($"Data seeding failed for {source.DisplayName}.", innerException)
    {
    }
}
