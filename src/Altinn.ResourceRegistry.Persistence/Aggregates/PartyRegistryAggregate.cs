using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Altinn.ResourceRegistry.Core.PartyRegistry;
using Microsoft.Extensions.Logging;

namespace Altinn.ResourceRegistry.Persistence.Aggregates;

/// <summary>
/// Represents a party registry aggregate.
/// </summary>
internal class PartyRegistryAggregate
    : Aggregate<PartyRegistryAggregate, PartyRegistryEvent>
    , IAggregateFactory<PartyRegistryAggregate, PartyRegistryEvent>
    , IAggregateEventHandler<PartyRegistryCreatedEvent>
    , IAggregateEventHandler<PartyRegistryUpdatedEvent>
    , IAggregateEventHandler<PartyRegistryDeletedEvent>
    , IAggregateEventHandler<PartyRegistryResourceConnectionSetEvent>
    , IAggregateEventHandler<PartyRegistryMembersAddedEvent>
    , IAggregateEventHandler<PartyRegistryMembersRemovedEvent>
{
    private bool _isDeleted;
    private string? _registryOwner;
    private string? _identifier;
    private string? _name;
    private string? _description;
    private Dictionary<string, ImmutableArray<string>> _resourceConnections = new();
    private HashSet<Guid> _members = new();

    /// <inheritdoc/>
    [SuppressMessage(
        "StyleCop.CSharp.DocumentationRules", 
        "SA1648:inheritdoc should be used with inheriting class", 
        Justification = "https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3717")]
    public static PartyRegistryAggregate New(TimeProvider timeProvider, Guid id)
        => new(timeProvider, id);

    private PartyRegistryAggregate(TimeProvider timeProvider, Guid id)
        : base(timeProvider, id)
    {
    }

    /// <inheritdoc />
    public override bool IsInitialized => _registryOwner is not null;

    /// <inheritdoc />
    public override bool IsDeleted => _isDeleted;

    /// <summary>
    /// Gets the registry owner.
    /// </summary>
    public string RegistryOwner => InitializedThis._registryOwner!;

    /// <summary>
    /// Gets the registry identifier.
    /// </summary>
    public string Identifier => InitializedThis._identifier!;

    /// <summary>
    /// Gets the registry (display) name.
    /// </summary>
    public string Name => InitializedThis._name!;

    /// <summary>
    /// Gets the registry (optional) description.
    /// </summary>
    public string? Description => InitializedThis._description;

    /// <summary>
    /// Create a new party registry.
    /// </summary>
    /// <param name="registryOwner">The registry owner</param>
    /// <param name="identifier">The registry identifier</param>
    /// <param name="name">The registry (display) name</param>
    /// <param name="description">The registry (optional) description</param>
    public void Initialize(string registryOwner, string identifier, string name, string? description)
    {
        if (IsInitialized)
        {
            throw new InvalidOperationException("Aggregate already initialized");
        }

        AddEvent(new PartyRegistryCreatedEvent(Id, registryOwner, identifier, name, description ?? string.Empty, GetUtcNow()));
    }

    /// <summary>
    /// Update the party registry.
    /// </summary>
    /// <param name="identifier">The new identifier, or <see langword="null"/> to keep the old value</param>
    /// <param name="name">The new <see cref="Name"/>, or <see langword="null"/> to keep the old value</param>
    /// <param name="description">The new <see cref="Description"/>, or <see langword="null"/> to keep the old value</param>
    public void Update(
        string? identifier = null,
        string? name = null,
        string? description = null)
    {
        AssertInitialized();

        if (identifier is null
            && name is null
            && description is null)
        {
            throw new ArgumentException("At least one of the parameters must be specified");
        }

        AddEvent(new PartyRegistryUpdatedEvent(Id, identifier, name, description, GetUtcNow()));
    }

    /// <summary>
    /// Delete the party registry.
    /// </summary>
    public void Delete()
    {
        AssertInitialized();

        AddEvent(new PartyRegistryDeletedEvent(Id, GetUtcNow()));
    }

    /// <summary>
    /// Add a resource connection to the party registry.
    /// </summary>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="actions">The actions allow-list</param>
    public void AddResourceConnection(string resourceIdentifier, IEnumerable<string> actions)
    {
        AssertInitialized();

        if (_resourceConnections.ContainsKey(resourceIdentifier))
        {
            throw new ArgumentException($"Resource connection for resource '{resourceIdentifier}' already exists");
        }

        var actionsImmutable = actions.ToImmutableArray();
        if (actionsImmutable.IsDefault)
        {
            throw new ArgumentException("Actions must be specified");
        }

        AddEvent(new PartyRegistryResourceConnectionSetEvent(Id, resourceIdentifier, actionsImmutable, GetUtcNow()));
    }

    /// <summary>
    /// Update a resource connection in the party registry.
    /// </summary>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="actions">The actions allow-list</param>
    public void UpdateResourceConnection(string resourceIdentifier, IEnumerable<string> actions)
    {
        AssertInitialized();

        if (!_resourceConnections.ContainsKey(resourceIdentifier))
        {
            throw new ArgumentException($"Resource connection for resource '{resourceIdentifier}' does not exist");
        }

        var actionsImmutable = actions.ToImmutableArray();
        if (actionsImmutable.IsDefault)
        {
            throw new ArgumentException("Actions must be specified");
        }

        AddEvent(new PartyRegistryResourceConnectionSetEvent(Id, resourceIdentifier, actionsImmutable, GetUtcNow()));
    }

    /// <summary>
    /// Remove a resource connection from the party registry.
    /// </summary>
    /// <param name="resourceIdentifier">The resource identifier</param>
    public void RemoveResourceConnection(string resourceIdentifier)
    {
        AssertInitialized();

        if (!_resourceConnections.ContainsKey(resourceIdentifier))
        {
            throw new ArgumentException($"Resource connection for resource '{resourceIdentifier}' does not exist");
        }

        AddEvent(new PartyRegistryResourceConnectionSetEvent(Id, resourceIdentifier, Actions: default, GetUtcNow()));
    }

    /// <summary>
    /// Add members to the party registry.
    /// </summary>
    /// <param name="partyIds">The members</param>
    public void AddMembers(IEnumerable<Guid> partyIds)
    {
        AssertInitialized();

        var partyIdsImmutable = partyIds.ToImmutableArray();
        if (partyIdsImmutable.IsDefault)
        {
            throw new ArgumentException("Party IDs must be specified", nameof(partyIds));
        }

        if (partyIdsImmutable.Any(_members.Contains))
        {
            throw new ArgumentException("One or more party IDs already exist in the registry", nameof(partyIds));
        }

        AddEvent(new PartyRegistryMembersAddedEvent(Id, partyIdsImmutable, GetUtcNow()));
    }

    /// <summary>
    /// Remove members from the party registry.
    /// </summary>
    /// <param name="partyIds">The members</param>
    public void RemoveMembers(IEnumerable<Guid> partyIds)
    {
        AssertInitialized();

        var partyIdsImmutable = partyIds.ToImmutableArray();
        if (partyIdsImmutable.IsDefault)
        {
            throw new ArgumentException("Party IDs must be specified", nameof(partyIds));
        }

        if (!partyIdsImmutable.All(_members.Contains))
        {
            throw new ArgumentException("One or more party IDs do not exist in the registry", nameof(partyIds));
        }

        AddEvent(new PartyRegistryMembersRemovedEvent(Id, partyIdsImmutable, GetUtcNow()));
    }

    /// <inheritdoc />
    void IAggregateEventHandler<PartyRegistryCreatedEvent>.ApplyEvent(PartyRegistryCreatedEvent @event)
    {
        _registryOwner = @event.RegistryOwner;
        _identifier = @event.Identifier;
        _name = @event.Name;
        _description = @event.Description;
    }

    /// <inheritdoc />
    void IAggregateEventHandler<PartyRegistryUpdatedEvent>.ApplyEvent(PartyRegistryUpdatedEvent @event)
    {
        if (@event.Identifier is { } identifier)
        {
            _identifier = identifier;
        }

        if (@event.Name is { } name)
        {
            _name = name;
        }

        if (@event.Description is { } description)
        {
            _description = description;
        }
    }

    /// <inheritdoc />
    void IAggregateEventHandler<PartyRegistryDeletedEvent>.ApplyEvent(PartyRegistryDeletedEvent @event)
    {
        _isDeleted = true;
    }

    /// <inheritdoc />
    void IAggregateEventHandler<PartyRegistryResourceConnectionSetEvent>.ApplyEvent(PartyRegistryResourceConnectionSetEvent @event)
    {
        if (@event.Actions.IsDefault)
        {
            _resourceConnections.Remove(@event.ResourceIdentifier);
        }
        else
        {
            _resourceConnections[@event.ResourceIdentifier] = @event.Actions;
        }
    }

    /// <inheritdoc />
    void IAggregateEventHandler<PartyRegistryMembersAddedEvent>.ApplyEvent(PartyRegistryMembersAddedEvent @event)
    {
        _members.UnionWith(@event.PartyIds);
    }

    /// <inheritdoc />
    void IAggregateEventHandler<PartyRegistryMembersRemovedEvent>.ApplyEvent(PartyRegistryMembersRemovedEvent @event)
    {
        _members.ExceptWith(@event.PartyIds);
    }

    /// <summary>
    /// Gets the aggregate as a <see cref="PartyRegistryInfo"/>.
    /// </summary>
    /// <returns><see cref="PartyRegistryInfo"/></returns>
    public PartyRegistryInfo AsRegistryInfo()
        => new PartyRegistryInfo(Id, RegistryOwner, Identifier, Name, Description, CreatedAt, UpdatedAt);
}

internal abstract record PartyRegistryEvent(Guid RegistryId, DateTimeOffset EventTime)
    : IAggregateEvent<PartyRegistryAggregate, PartyRegistryEvent>
{
    /// <summary>
    /// Apply the event to the specified <paramref name="aggregate"/>.
    /// </summary>
    /// <param name="aggregate">The aggregate</param>
    /// <remarks>This implements the visitor pattern.</remarks>
    protected abstract void ApplyTo(PartyRegistryAggregate aggregate);

    /// <inheritdoc />
    void IAggregateEvent<PartyRegistryAggregate, PartyRegistryEvent>.ApplyTo(PartyRegistryAggregate aggregate)
        => ApplyTo(aggregate);

    /// <summary>
    /// Gets the event as a set of values used for persistence.
    /// </summary>
    /// <returns><see cref="Values"/></returns>
    internal abstract Values AsValues();

    internal readonly record struct Values(
        string Kind,
        DateTimeOffset EventTime,
        Guid AggregateId,
        string? Identifier,
        string? Name,
        string? Description,
        string? RegistryOwner,
        ImmutableArray<string> Actions,
        ImmutableArray<Guid> PartyIds);
}

internal record PartyRegistryCreatedEvent(
    Guid RegistryId,
    string RegistryOwner,
    string Identifier,
    string Name,
    string Description,
    DateTimeOffset EventTime)
    : PartyRegistryEvent(RegistryId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(PartyRegistryAggregate aggregate)
        => ((IAggregateEventHandler<PartyRegistryCreatedEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "registry_created",
            EventTime: EventTime,
            AggregateId: RegistryId,
            Identifier: Identifier,
            Name: Name,
            Description: Description,
            RegistryOwner: RegistryOwner,
            Actions: default,
            PartyIds: default);
}

internal record PartyRegistryUpdatedEvent(
    Guid RegistryId,
    string? Identifier,
    string? Name,
    string? Description,
    DateTimeOffset EventTime)
    : PartyRegistryEvent(RegistryId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(PartyRegistryAggregate aggregate)
        => ((IAggregateEventHandler<PartyRegistryUpdatedEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "registry_updated",
            EventTime: EventTime,
            AggregateId: RegistryId,
            Identifier: Identifier,
            Name: Name,
            Description: Description,
            RegistryOwner: null,
            Actions: default,
            PartyIds: default);
}

internal record PartyRegistryDeletedEvent(
    Guid RegistryId,
    DateTimeOffset EventTime)
    : PartyRegistryEvent(RegistryId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(PartyRegistryAggregate aggregate)
        => ((IAggregateEventHandler<PartyRegistryDeletedEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "registry_deleted",
            EventTime: EventTime,
            AggregateId: RegistryId,
            Identifier: null,
            Name: null,
            Description: null,
            RegistryOwner: null,
            Actions: default,
            PartyIds: default);
}

internal record PartyRegistryResourceConnectionSetEvent(
    Guid RegistryId,
    string ResourceIdentifier,
    ImmutableArray<string> Actions,
    DateTimeOffset EventTime)
    : PartyRegistryEvent(RegistryId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(PartyRegistryAggregate aggregate)
        => ((IAggregateEventHandler<PartyRegistryResourceConnectionSetEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "resource_connection_set",
            EventTime: EventTime,
            AggregateId: RegistryId,
            Identifier: null,
            Name: null,
            Description: null,
            RegistryOwner: null,
            Actions: Actions,
            PartyIds: default);
}

internal record PartyRegistryMembersAddedEvent(
    Guid RegistryId,
    ImmutableArray<Guid> PartyIds,
    DateTimeOffset EventTime)
    : PartyRegistryEvent(RegistryId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(PartyRegistryAggregate aggregate)
        => ((IAggregateEventHandler<PartyRegistryMembersAddedEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "members_added",
            EventTime: EventTime,
            AggregateId: RegistryId,
            Identifier: null,
            Name: null,
            Description: null,
            RegistryOwner: null,
            Actions: default,
            PartyIds: PartyIds);
}

internal record PartyRegistryMembersRemovedEvent(
    Guid RegistryId,
    ImmutableArray<Guid> PartyIds,
    DateTimeOffset EventTime)
    : PartyRegistryEvent(RegistryId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(PartyRegistryAggregate aggregate)
        => ((IAggregateEventHandler<PartyRegistryMembersRemovedEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "members_removed",
            EventTime: EventTime,
            AggregateId: RegistryId,
            Identifier: null,
            Name: null,
            Description: null,
            RegistryOwner: null,
            Actions: default,
            PartyIds: PartyIds);
}
