using System.Diagnostics.CodeAnalysis;
using Altinn.ResourceRegistry.Core.PartyRegistry;

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
{
    private bool _isDeleted;
    private string? _registryOwner;
    private string? _identifier;
    private string? _name;
    private string? _description;

    /// <inheritdoc/>
    [SuppressMessage(
        "StyleCop.CSharp.DocumentationRules", 
        "SA1648:inheritdoc should be used with inheriting class", 
        Justification = "https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3717")]
    public static PartyRegistryAggregate New(Guid id)
        => new(id);

    private PartyRegistryAggregate(Guid id)
        : base(id)
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
    /// <param name="eventTime">The event time</param>
    /// <param name="registryOwner">The registry owner</param>
    /// <param name="identifier">The registry identifier</param>
    /// <param name="name">The registry (display) name</param>
    /// <param name="description">The registry (optional) description</param>
    public void Initialize(DateTimeOffset eventTime, string registryOwner, string identifier, string name, string? description)
        => Apply(new PartyRegistryCreatedEvent(Id, registryOwner, identifier, name, description ?? string.Empty, eventTime));

    /// <summary>
    /// Update the party registry.
    /// </summary>
    /// <param name="eventTime">The event time</param>
    /// <param name="identifier">The new identifier, or <see langword="null"/> to keep the old value</param>
    /// <param name="name">The new <see cref="Name"/>, or <see langword="null"/> to keep the old value</param>
    /// <param name="description">The new <see cref="Description"/>, or <see langword="null"/> to keep the old value</param>
    public void Update(
        DateTimeOffset eventTime,
        string? identifier = null,
        string? name = null,
        string? description = null)
        => Apply(new PartyRegistryUpdatedEvent(Id, identifier, name, description, eventTime));

    /// <summary>
    /// Delete the party registry.
    /// </summary>
    /// <param name="eventTime">The event time</param>
    public void Delete(DateTimeOffset eventTime)
        => Apply(new PartyRegistryDeletedEvent(Id, eventTime));

    /// <inheritdoc />
    void IAggregateEventHandler<PartyRegistryCreatedEvent>.Apply(PartyRegistryCreatedEvent @event)
    {
        if (IsInitialized)
        {
            throw new InvalidOperationException("Aggregate already initialized");
        }

        _registryOwner = @event.RegistryOwner;
        _identifier = @event.Identifier;
        _name = @event.Name;
        _description = @event.Description;
    }

    /// <inheritdoc />
    void IAggregateEventHandler<PartyRegistryUpdatedEvent>.Apply(PartyRegistryUpdatedEvent @event)
    {
        AssertInitialized();
        
        if (@event.Identifier is null
            && @event.Name is null
            && @event.Description is null)
        {
            throw new ArgumentException("At least one of the parameters must be specified", nameof(@event));
        }

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
    void IAggregateEventHandler<PartyRegistryDeletedEvent>.Apply(PartyRegistryDeletedEvent @event)
    {
        AssertInitialized();

        _isDeleted = true;
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
        string[]? Actions,
        Guid[]? PartyIds);
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
        => ((IAggregateEventHandler<PartyRegistryCreatedEvent>)aggregate).Apply(this);

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
            Actions: null,
            PartyIds: null);
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
        => ((IAggregateEventHandler<PartyRegistryUpdatedEvent>)aggregate).Apply(this);

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
            Actions: null,
            PartyIds: null);
}

internal record PartyRegistryDeletedEvent(
    Guid RegistryId,
    DateTimeOffset EventTime)
    : PartyRegistryEvent(RegistryId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(PartyRegistryAggregate aggregate)
        => ((IAggregateEventHandler<PartyRegistryDeletedEvent>)aggregate).Apply(this);

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
            Actions: null,
            PartyIds: null);
}