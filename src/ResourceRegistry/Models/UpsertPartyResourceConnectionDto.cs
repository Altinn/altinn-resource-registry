namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// Model for creating or updateing a party registry resource-connection.
/// </summary>
/// <param name="Actions">The allowed actions.</param>
public record UpsertPartyResourceConnectionDto(
    IReadOnlyList<string> Actions);
