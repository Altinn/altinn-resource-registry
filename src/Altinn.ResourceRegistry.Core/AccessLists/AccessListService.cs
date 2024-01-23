﻿#nullable enable

using System.Data;
using System.Diagnostics;
using Altinn.ResourceRegistry.Core.Models;
using CommunityToolkit.Diagnostics;

namespace Altinn.ResourceRegistry.Core.AccessLists;

/// <summary>
/// Implementation of <see cref="IAccessListService"/>.
/// </summary>
internal class AccessListService
    : IAccessListService
{
    private const int LISTS_PAGE_SIZE = 20;

    private readonly IAccessListsRepository _repository;

    /// <summary>
    /// Constructs a new instance of <see cref="AccessListService"/>.
    /// </summary>
    /// <param name="repository">A <see cref="IAccessListsRepository"/></param>
    public AccessListService(IAccessListsRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Gets access lists by owner, limited by <see cref="LISTS_PAGE_SIZE"/> and optionally starting from <paramref name="request"/>.ContinuationToken.
    /// </summary>
    /// <param name="owner">The resource owner.</param>
    /// <param name="request">The page request metadata.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Page{TItem, TToken}"/> of <see cref="AccessListInfo"/></returns>
    public async Task<Page<AccessListInfo, string>> GetAccessListsByOwner(
        string owner,
        Page<string>.Request request,
        CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(owner);
        Guard.IsNotNull(request);

        // request 1 more than page size to determine if there are more pages
        var accessLists = await _repository.GetAccessListsByOwner(
            owner,
            continueFrom: request.ContinuationToken,
            count: LISTS_PAGE_SIZE + 1,
            cancellationToken);

        return Page.Create(accessLists, LISTS_PAGE_SIZE, static list => list.Identifier);
    }

    /// <inheritdoc/>
    public async Task<Conditional<AccessListInfo, ulong>> GetAccessList(string owner, string identifier, IVersionedEntityCondition<ulong>? condition = null, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(owner);
        Guard.IsNotNull(identifier);

        var accessList = await _repository.LookupInfo(owner, identifier, cancellationToken);
        if (accessList is null)
        {
            return Conditional.NotFound();
        }

        if (condition is not null)
        {
            var result = condition.Validate(accessList);
            if (result == VersionedEntityConditionResult.Failed)
            {
                return Conditional.Failed();
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
            return Conditional.NotFound();
        }

        if (condition is not null)
        {
            var info = aggregate.AsAccessListInfo();
            var result = condition.Validate(info);
            if (result == VersionedEntityConditionResult.Failed)
            {
                return Conditional.Failed();
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
        var createValidationResult = condition?.Validate(default(NullEntity)) ?? VersionedEntityConditionResult.Succeeded;
        Debug.Assert(createValidationResult != VersionedEntityConditionResult.Unmodified, "Unmodified should not be possible when creating");

        IAccessListAggregate aggregate;
        var allowCreate = createValidationResult == VersionedEntityConditionResult.Succeeded;
        if (!allowCreate)
        {
            // This is effectively just an update, not an upsert at this point.
            var existing = await _repository.LoadAccessList(owner, identifier, cancellationToken);
            if (existing is null)
            {
                Debug.Assert(createValidationResult == VersionedEntityConditionResult.Failed, "If the aggregate was not found, the condition should have failed");
                return Conditional.Failed();
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
                Debug.Assert(createValidationResult == VersionedEntityConditionResult.Succeeded, "If the aggregate was created, the condition should have succeeded");
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
                return Conditional.Failed();
            }

            Debug.Assert(result != VersionedEntityConditionResult.Unmodified, "Unmodified should not be possible when deleting");
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

    private readonly struct NullEntity
        : IVersionEquatable<ulong>
    {
        public bool ModifiedSince(HttpDateTimeHeaderValue other)
            => false;

        public bool VersionEquals(ulong other)
            => false;
    }
}
