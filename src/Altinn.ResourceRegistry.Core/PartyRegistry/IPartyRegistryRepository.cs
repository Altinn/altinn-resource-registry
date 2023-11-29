namespace Altinn.ResourceRegistry.Core.PartyRegistry;

/// <summary>
/// Repository for managing party registries.
/// </summary>
public interface IPartyRegistryRepository
{
    /// <summary>
    /// Create a new party registry.
    /// </summary>
    /// <param name="registryOwner">The registry owner (org.nr.).</param>
    /// <param name="identifier">The registry identifier (unique per owner).</param>
    /// <param name="name">The registry name.</param>
    /// <param name="description">The registry description.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The newly created registry in the form of a <see cref="PartyRegistryInfo"/>.</returns>
    public Task<PartyRegistryInfo> CreatePartyRegistry(
        string registryOwner,
        string identifier,
        string name,
        string description,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about a party registry.
/// </summary>
/// <param name="Id">The database id for the party registry.</param>
/// <param name="RegistryOwner">The registry owner (a org.nr.).</param>
/// <param name="Identifier">The registry owner-unique identifier. Limited to 'a'-'z' and '-' characters.</param>
/// <param name="Name">The registry name. Does not have to be unique, and can contain any characters.</param>
/// <param name="Description">A registry description.</param>
/// <param name="CreatedAt">When this registry was created.</param>
/// <param name="UpdatedAt">When this registry was last updated.</param>
public record PartyRegistryInfo(
    Guid Id,
    string RegistryOwner,
    string Identifier,
    string Name,
    string Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);