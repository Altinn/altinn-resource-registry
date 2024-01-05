namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// Model used for creating or updating a party registry.
/// </summary>
/// <param name="Name">The party registry name.</param>
/// <param name="Description">The (optional) party registry description.</param>
public record CreatePartyRegistryModel(
    string Name,
    string? Description);
