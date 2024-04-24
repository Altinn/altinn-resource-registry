namespace Altinn.TestSeed;

/// <summary>
/// A test-data seeder source.
/// </summary>
public interface ITestDataSeederSource
{
    /// <summary>
    /// Gets the display name of the seeder source.
    /// </summary>
    string DisplayName { get; }
}
