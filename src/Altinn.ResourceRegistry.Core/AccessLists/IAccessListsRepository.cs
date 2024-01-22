#nullable enable

namespace Altinn.ResourceRegistry.Core.AccessLists;

/// <summary>
/// Repository for managing access lists.
/// </summary>
public interface IAccessListsRepository
{
    /// <summary>
    /// Gets access lists by owner, limited by <paramref name="count"/> and optionally starting from <paramref name="continueFrom"/>.
    /// </summary>
    /// <param name="resourceOwner">The resource owner</param>
    /// <param name="continueFrom">An optional value to continue iterating from. This value is an <see cref="AccessListInfo.Identifier"/> to start from, using greater than or equals comparison.</param>
    /// <param name="count">The total number of entries to return.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A list of <see cref="AccessListInfo"/>, sorted by <see cref="AccessListInfo.Identifier"/> and limited by <paramref name="count"/>.</returns>
    Task<IReadOnlyList<AccessListInfo>> GetAccessListsByOwner(string resourceOwner, string? continueFrom, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lookup access list info by id.
    /// </summary>
    /// <param name="id">The id</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="AccessListInfo"/>, if found, else <see langword="null"/></returns>
    Task<AccessListInfo?> LookupInfo(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lookup access list info by resource owner and identifier.
    /// </summary>
    /// <param name="resourceOwner">The resource owner</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="AccessListInfo"/>, if found, else <see langword="null"/></returns>
    Task<AccessListInfo?> LookupInfo(string resourceOwner, string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lookup resource connections for an access list by it's id.
    /// </summary>
    /// <param name="id">The id</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A list of <see cref="AccessListResourceConnection"/> for the given resource.</returns>
    Task<IReadOnlyList<AccessListResourceConnection>?> GetAccessListResourceConnections(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lookup resource connections for an access list by it's resource owner and identifier.
    /// </summary>
    /// <param name="resourceOwner">The resource owner</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A list of <see cref="AccessListResourceConnection"/> for the given resource.</returns>
    Task<IReadOnlyList<AccessListResourceConnection>?> GetAccessListResourceConnections(
        string resourceOwner,
        string identifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get members of an access list.
    /// </summary>
    /// <param name="id">The access list id</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A list of <see cref="AccessListMembership"/></returns>
    Task<IReadOnlyList<AccessListMembership>?> GetAccessListMemberships(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get members of an access list.
    /// </summary>
    /// <param name="resourceOwner">The resource owner</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A list of <see cref="AccessListMembership"/></returns>
    Task<IReadOnlyList<AccessListMembership>?> GetAccessListMemberships(
        string resourceOwner,
        string identifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new access list.
    /// </summary>
    /// <param name="resourceOwner">The resource owner (org.nr.).</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="name">The registry name.</param>
    /// <param name="description">The registry description.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The newly created registry in the form of a <see cref="AccessListInfo"/>.</returns>
    Task<IAccessListAggregate> CreateAccessList(
        string resourceOwner,
        string identifier,
        string name,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Load an access list aggregate.
    /// </summary>
    /// <param name="id">The access list id</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="IAccessListAggregate"/>, if found, else <see langword="null"/></returns>
    Task<IAccessListAggregate?> LoadAccessList(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Load an access list aggregate.
    /// </summary>
    /// <param name="resourceOwner">The resource owner</param>
    /// <param name="identifier">The access list identifier (unique per owner).</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="IAccessListAggregate"/>, if found, else <see langword="null"/></returns>
    Task<IAccessListAggregate?> LoadAccessList(string resourceOwner, string identifier, CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about an access list.
/// </summary>
/// <param name="Id">The database id for the access list.</param>
/// <param name="RegistryOwner">The resource owner (a org.nr.).</param>
/// <param name="Identifier">The resource owner-unique identifier. Limited to 'a'-'z' and '-' characters.</param>
/// <param name="Name">The registry name. Does not have to be unique, and can contain any characters.</param>
/// <param name="Description">A registry description.</param>
/// <param name="CreatedAt">When this registry was created.</param>
/// <param name="UpdatedAt">When this registry was last updated.</param>
public record AccessListInfo(
    Guid Id,
    string RegistryOwner,
    string Identifier,
    string Name,
    string Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Information about an access list resource connection.
/// </summary>
/// <param name="ResourceIdentifier">The resource identifier.</param>
/// <param name="Actions">The allowed actions.</param>
/// <param name="Created">When the connection was created.</param>
/// <param name="Modified">When the connection was last modified.</param>
public record AccessListResourceConnection(
    string ResourceIdentifier,
    ImmutableHashSet<string> Actions,
    DateTimeOffset Created,
    DateTimeOffset Modified);

/// <summary>
/// Information about an access list membership.
/// </summary>
/// <param name="PartyId">The party id.</param>
/// <param name="Since">When the party was added to the registry.</param>
public record AccessListMembership(
    Guid PartyId,
    DateTimeOffset Since);
