using Microsoft.Extensions.FileProviders;

namespace Altinn.TestSeed.FileSystem;

/// <summary>
/// Options for <see cref="SeedDataDirectoryTestDataSeederProvider"/>.
/// </summary>
public class SeedDataDirectorySettings
{
    /// <summary>
    /// Gets or sets the file provider.
    /// </summary>
    public IFileProvider? FileProvider { get; set; }
}
