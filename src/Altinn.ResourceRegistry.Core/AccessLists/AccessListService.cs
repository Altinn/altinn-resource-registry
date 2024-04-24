﻿#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Versioned;
using Altinn.ResourceRegistry.Core.Register;
using CommunityToolkit.Diagnostics;

namespace Altinn.ResourceRegistry.Core.AccessLists;

/// <summary>
/// Implementation of <see cref="IAccessListService"/>.
/// </summary>
internal class AccessListService
    : IAccessListService
{
    private const int SMALL_PAGE_SIZE = 20;
    private const int LARGE_PAGE_SIZE = 100;

    private readonly IAccessListsRepository _repository;
    private readonly IRegisterClient _register;

    /// <summary>
    /// Constructs a new instance of <see cref="AccessListService"/>.
    /// </summary>
    public AccessListService(IAccessListsRepository repository, IRegisterClient register)
    {
        _repository = repository;
        _register = register;
    }

    /// <inheritdoc/>
    public async Task<Page<AccessListInfo, string>> GetAccessListsByOwner(
        string owner,
        Page<string>.Request request,
        AccessListIncludes includes = default,
        string? resourceIdentifier = null,
        CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(owner);
        Guard.IsNotNull(request);

        // request 1 more than page size to determine if there are more pages
        var accessLists = await _repository.GetAccessListsByOwner(
            owner,
            continueFrom: request.ContinuationToken,
            count: SMALL_PAGE_SIZE + 1,
            includes,
            resourceIdentifier,
            cancellationToken);

        return Page.Create(accessLists, SMALL_PAGE_SIZE, static list => list.Identifier);
    }

    /// <inheritdoc/>
    public async Task<Conditional<AccessListInfo, ulong>> GetAccessList(
        string owner,
        string identifier,
        AccessListIncludes includes = default,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(owner);
        Guard.IsNotNull(identifier);

        var accessList = await _repository.LookupInfo(owner, identifier, includes, cancellationToken);
        if (accessList is null)
        {
            return Conditional.NotFound(nameof(AccessListInfo));
        }

        if (condition is not null)
        {
            var result = condition.Validate(accessList);
            if (result == VersionedEntityConditionResult.Failed)
            {
                return Conditional.ConditionFailed();
            }

            if (result == VersionedEntityConditionResult.Unmodified)
            {
                return Conditional.Unmodified(accessList.Version, accessList.UpdatedAt);
            }
        }

        return accessList;
    }

    /// <inheritdoc/>
    public async Task<Conditional<AccessListInfo, ulong>> DeleteAccessList(
        string owner,
        string identifier,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(owner);
        Guard.IsNotNull(identifier);

        var aggregate = await _repository.LoadAccessList(owner, identifier, cancellationToken);
        if (aggregate is null)
        {
            return Conditional.NotFound(nameof(AccessListInfo));
        }

        if (condition is not null)
        {
            var info = aggregate.AsAccessListInfo();
            var result = condition.Validate(info);
            if (result == VersionedEntityConditionResult.Failed)
            {
                return Conditional.ConditionFailed();
            }

            Debug.Assert(result != VersionedEntityConditionResult.Unmodified, "Unmodified should not be possible when deleting");
        }

        aggregate.Delete();
        await aggregate.SaveChanged(cancellationToken);

        return aggregate.AsAccessListInfo();
    }

    /// <inheritdoc/>
    public async Task<Conditional<AccessListInfo, ulong>> CreateOrUpdateAccessList(
        string owner,
        string identifier,
        string name,
        string description,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(owner);
        Guard.IsNotNull(identifier);
        Guard.IsNotNull(name);
        Guard.IsNotNull(description);

        // First check if we're allowed to create a new list. This has to happen first,
        // because the list needs to be created in the same transaction that did not find
        // it to avoid race conditions.
        var allowCreate = condition?.AllowsCreatingNewEntity() ?? true;

        IAccessListAggregate aggregate;
        if (!allowCreate)
        {
            // This is effectively just an update, not an upsert at this point.
            var existing = await _repository.LoadAccessList(owner, identifier, cancellationToken);
            if (existing is null)
            {
                // If the aggregate was not found, we're not allowed to create it by a condition.
                return Conditional.ConditionFailed();
            }

            aggregate = existing;
        }
        else
        {
            var result = await _repository.LoadOrCreateAccessList(owner, identifier, name, description, cancellationToken);
            aggregate = result.Aggregate;

            // If the list was created, we can simply return it.
            if (result.IsNew)
            {
                Debug.Assert(aggregate.Name == name, "The name should have been set on the aggregate");
                Debug.Assert(aggregate.Description == description, "The description should have been set on the aggregate");

                // The aggregate was just created (and saved), and we already checked
                // that the condition was met, so we can return it as is.
                return aggregate!.AsAccessListInfo();
            }
        }
        
        // If we had an existing list - we need to validate it against the condition.
        if (condition is not null)
        {
            var info = aggregate.AsAccessListInfo();
            var result = condition.Validate(info);
            if (result == VersionedEntityConditionResult.Failed)
            {
                return Conditional.ConditionFailed();
            }

            Debug.Assert(result != VersionedEntityConditionResult.Unmodified, "Unmodified should not be possible when creating");
        }

        // If the name and/or description is different in the list from what was requested, we need to update it.
        var newName = aggregate.Name == name ? null : name;
        var newDescription = aggregate.Description == description ? null : description;

        if (newName is not null || newDescription is not null)
        {
            aggregate.Update(name: newName, description: newDescription);
            await aggregate.SaveChanged(cancellationToken);
        }

        // Return the maybe updated list.
        return aggregate.AsAccessListInfo();
    }

    /// <inheritdoc/>
    public async Task<Conditional<VersionedPage<AccessListResourceConnection, string, ulong>, ulong>> GetAccessListResourceConnections(
        string owner,
        string identifier,
        Page<string>.Request request,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(owner);
        Guard.IsNotNull(identifier);
        Guard.IsNotNull(request);

        var data = await _repository.GetAccessListResourceConnections(
            owner,
            identifier,
            continueFrom: request.ContinuationToken,
            count: LARGE_PAGE_SIZE + 1,
            includeActions: true,
            cancellationToken);

        if (data is null)
        {
            return Conditional.NotFound(nameof(AccessListInfo));
        }

        if (condition is not null)
        {
            var result = condition.Validate(data);

            if (result == VersionedEntityConditionResult.Failed)
            {
                return Conditional.ConditionFailed();
            }

            if (result == VersionedEntityConditionResult.Unmodified)
            {
                return Conditional.Unmodified(data.Version, data.UpdatedAt);
            }
        }

        return Page.Create(data.Value, LARGE_PAGE_SIZE, static resource => resource.ResourceIdentifier)
            .WithVersion(data.UpdatedAt, data.Version);
    }

    /// <inheritdoc/>
    public async Task<Conditional<AccessListData<AccessListResourceConnection>, ulong>> UpsertAccessListResourceConnection(
        string owner,
        string identifier,
        string resourceIdentifier,
        IReadOnlyList<string> actions,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(owner);
        Guard.IsNotNull(identifier);
        Guard.IsNotNull(resourceIdentifier);
        Guard.IsNotNull(actions);

        var aggregate = await _repository.LoadAccessList(owner, identifier, cancellationToken);

        if (aggregate is null)
        {
            return Conditional.NotFound(nameof(AccessListInfo));
        }

        if (condition is not null)
        {
            var result = condition.Validate(aggregate.AsAccessListInfo());

            Debug.Assert(result != VersionedEntityConditionResult.Unmodified, "Unmodified should not be possible when upserting");

            if (result == VersionedEntityConditionResult.Failed)
            {
                return Conditional.ConditionFailed();
            }
        }

        switch (aggregate.TryGetResourceConnections(resourceIdentifier, out var connection))
        {
            case true when !connection.Actions!.SetEquals(actions):
                var toRemove = connection.Actions.Except(actions).ToImmutableArray();
                var toAdd = actions.Except(connection.Actions).ToImmutableArray();
            
                if (toRemove.Length > 0)
                {
                    aggregate.RemoveResourceConnectionActions(resourceIdentifier, toRemove);
                }
            
                if (toAdd.Length > 0)
                {
                    aggregate.AddResourceConnectionActions(resourceIdentifier, toAdd);
                }

                break;

            case false:
                aggregate.AddResourceConnection(resourceIdentifier, actions);
                break;
        }

        await aggregate.SaveChanged(cancellationToken);
        
        if (!aggregate.TryGetResourceConnections(resourceIdentifier, out connection))
        {
            throw new UnreachableException("The resource connection should exist at this point");
        }

        return AccessListData.Create(aggregate.AsAccessListInfo(), connection);
    }

    /// <inheritdoc/>
    public async Task<Conditional<AccessListData<AccessListResourceConnection>, ulong>> DeleteAccessListResourceConnection(
        string owner,
        string identifier,
        string resourceIdentifier,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(owner);
        Guard.IsNotNull(identifier);
        Guard.IsNotNull(resourceIdentifier);

        var aggregate = await _repository.LoadAccessList(owner, identifier, cancellationToken);

        if (aggregate is null)
        {
            return Conditional.NotFound(nameof(AccessListInfo));
        }

        if (condition is not null)
        {
            var result = condition.Validate(aggregate.AsAccessListInfo());

            Debug.Assert(result != VersionedEntityConditionResult.Unmodified, "Unmodified should not be possible when upserting");

            if (result == VersionedEntityConditionResult.Failed)
            {
                return Conditional.ConditionFailed();
            }
        }

        if (!aggregate.TryGetResourceConnections(resourceIdentifier, out var connection))
        {
            return Conditional.NotFound(nameof(AccessListResourceConnection));
        }

        aggregate.RemoveResourceConnection(resourceIdentifier);
        await aggregate.SaveChanged(cancellationToken);

        return AccessListData.Create(aggregate.AsAccessListInfo(), connection);
    }

    /// <inheritdoc/>
    public async Task<Conditional<VersionedPage<EnrichedAccessListMembership, Guid, ulong>, ulong>> GetAccessListMembers(
        string owner,
        string identifier,
        Page<Guid?>.Request request,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(owner);
        Guard.IsNotNull(identifier);

        var data = await _repository.GetAccessListMemberships(
            owner,
            identifier,
            continueFrom: request.ContinuationToken,
            count: LARGE_PAGE_SIZE + 1,
            cancellationToken);

        if (data is null)
        {
            return Conditional.NotFound(nameof(AccessListInfo));
        }

        if (condition is not null)
        {
            var result = condition.Validate(data);

            if (result == VersionedEntityConditionResult.Failed)
            {
                return Conditional.ConditionFailed();
            }

            if (result == VersionedEntityConditionResult.Unmodified)
            {
                return Conditional.Unmodified(data.Version, data.UpdatedAt);
            }
        }

        var identifiers = await _register
            .GetPartyIdentifiers(data.Value.Select(m => m.PartyUuid), cancellationToken)
            .ToDictionaryAsync(m => m.PartyUuid, cancellationToken);

        var enrichedBuilder = ImmutableArray.CreateBuilder<EnrichedAccessListMembership>(data.Value.Count);
        enrichedBuilder.AddRange(data.Value.Select(m => new EnrichedAccessListMembership(m, identifiers[m.PartyUuid])));
        var enriched = enrichedBuilder.MoveToImmutable();

        return Page.Create(enriched, LARGE_PAGE_SIZE, static membership => membership.PartyUuid)
            .WithVersion(data.UpdatedAt, data.Version);
    }

    /// <inheritdoc/>
    public async Task<Conditional<VersionedPage<EnrichedAccessListMembership, Guid, ulong>, ulong>> ReplaceAccessListMembers(
        string owner,
        string identifier,
        IReadOnlyList<PartyReference> parties,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(owner);
        Guard.IsNotNull(identifier);
        Guard.IsNotNull(parties);

        var aggregate = await _repository.LoadAccessList(owner, identifier, cancellationToken);

        if (aggregate is null)
        {
            return Conditional.NotFound(nameof(AccessListInfo));
        }

        if (condition is not null)
        {
            var result = condition.Validate(aggregate.AsAccessListInfo());

            Debug.Assert(result != VersionedEntityConditionResult.Unmodified, "Unmodified should not be possible when updating");

            if (result == VersionedEntityConditionResult.Failed)
            {
                return Conditional.ConditionFailed();
            }
        }

        var identifiers = await _register.GetPartyIdentifiers(parties, cancellationToken).ToListAsync(cancellationToken);
        var membersBuilder = ImmutableHashSet.CreateBuilder<Guid>();

        foreach (var partyRef in parties)
        {
            var match = partyRef switch
            {
                PartyReference.PartyId partyId => identifiers.Find(v => v.PartyId == partyId.Value),
                PartyReference.PartyUuid partyUuid => identifiers.Find(v => v.PartyUuid == partyUuid.Value),
                PartyReference.OrganizationIdentifier orgNo => identifiers.Find(v => v.OrgNumber == orgNo.Value.ToString()),
                _ => null
            };

            if (match is null)
            {
                return Conditional.NotFound(nameof(PartyReference));
            }

            membersBuilder.Add(match.PartyUuid);
        }

        var newMembers = membersBuilder.ToImmutable();
        var toRemove = aggregate.Members.Except(newMembers);
        var toAdd = newMembers.Except(aggregate.Members);

        if (toRemove.Count > 0)
        {
            aggregate.RemoveMembers(toRemove);
        }

        if (toAdd.Count > 0)
        {
            aggregate.AddMembers(toAdd);
        }

        await aggregate.SaveChanged(cancellationToken);

        return await GetAccessListMembers(owner, identifier, Page.DefaultRequest(), null, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Conditional<VersionedPage<EnrichedAccessListMembership, Guid, ulong>, ulong>> AddAccessListMembers(
        string owner,
        string identifier,
        IReadOnlyList<PartyReference> parties,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(owner);
        Guard.IsNotNull(identifier);
        Guard.IsNotNull(parties);

        var aggregate = await _repository.LoadAccessList(owner, identifier, cancellationToken);

        if (aggregate is null)
        {
            return Conditional.NotFound(nameof(AccessListInfo));
        }

        if (condition is not null)
        {
            var result = condition.Validate(aggregate.AsAccessListInfo());

            Debug.Assert(result != VersionedEntityConditionResult.Unmodified, "Unmodified should not be possible when updating");

            if (result == VersionedEntityConditionResult.Failed)
            {
                return Conditional.ConditionFailed();
            }
        }

        var identifiers = await _register.GetPartyIdentifiers(parties, cancellationToken).ToListAsync(cancellationToken);
        var toRemove = new HashSet<Guid>();

        foreach (var partyRef in parties)
        {
            var match = partyRef switch
            {
                PartyReference.PartyId partyId => identifiers.Find(v => v.PartyId == partyId.Value),
                PartyReference.PartyUuid partyUuid => identifiers.Find(v => v.PartyUuid == partyUuid.Value),
                PartyReference.OrganizationIdentifier orgNo => identifiers.Find(v => v.OrgNumber == orgNo.Value.ToString()),
                _ => null
            };

            if (match is null)
            {
                return Conditional.NotFound(nameof(PartyReference));
            }

            if (aggregate.Members.Contains(match.PartyUuid))
            {
                toRemove.Add(match.PartyUuid);
            }
        }

        if (toRemove.Count > 0)
        {
            aggregate.RemoveMembers(toRemove);
        }

        await aggregate.SaveChanged(cancellationToken);

        return await GetAccessListMembers(owner, identifier, Page.DefaultRequest(), null, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Conditional<VersionedPage<EnrichedAccessListMembership, Guid, ulong>, ulong>> RemoveAccessListMembers(
        string owner,
        string identifier,
        IReadOnlyList<PartyReference> parties,
        IVersionedEntityCondition<ulong>? condition = null,
        CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(owner);
        Guard.IsNotNull(identifier);
        Guard.IsNotNull(parties);

        var aggregate = await _repository.LoadAccessList(owner, identifier, cancellationToken);

        if (aggregate is null)
        {
            return Conditional.NotFound(nameof(AccessListInfo));
        }

        if (condition is not null)
        {
            var result = condition.Validate(aggregate.AsAccessListInfo());

            Debug.Assert(result != VersionedEntityConditionResult.Unmodified, "Unmodified should not be possible when updating");

            if (result == VersionedEntityConditionResult.Failed)
            {
                return Conditional.ConditionFailed();
            }
        }

        var identifiers = await _register.GetPartyIdentifiers(parties, cancellationToken).ToListAsync(cancellationToken);
        var toAdd = new HashSet<Guid>();

        foreach (var partyRef in parties)
        {
            var match = partyRef switch
            {
                PartyReference.PartyId partyId => identifiers.Find(v => v.PartyId == partyId.Value),
                PartyReference.PartyUuid partyUuid => identifiers.Find(v => v.PartyUuid == partyUuid.Value),
                PartyReference.OrganizationIdentifier orgNo => identifiers.Find(v => v.OrgNumber == orgNo.Value.ToString()),
                _ => null
            };

            if (match is null)
            {
                return Conditional.NotFound(nameof(PartyReference));
            }

            if (!aggregate.Members.Contains(match.PartyUuid))
            {
                toAdd.Add(match.PartyUuid);
            }
        }

        if (toAdd.Count > 0)
        {
            aggregate.AddMembers(toAdd);
        }

        await aggregate.SaveChanged(cancellationToken);

        return await GetAccessListMembers(owner, identifier, Page.DefaultRequest(), null, cancellationToken);
    }
}
