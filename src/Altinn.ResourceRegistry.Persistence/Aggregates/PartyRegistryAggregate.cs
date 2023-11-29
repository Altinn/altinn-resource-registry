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
{
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
    public override bool IsDeleted => false;

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
    /// <param name="eventTime">The event time</param>
    public void Initialize(string registryOwner, string identifier, string name, string? description, DateTimeOffset eventTime)
        => Apply(new PartyRegistryCreatedEvent(Id, registryOwner, identifier, name, description, eventTime));

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
    string? Description,
    DateTimeOffset EventTime)
    : PartyRegistryEvent(RegistryId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(PartyRegistryAggregate aggregate)
        => ((IAggregateEventHandler<PartyRegistryCreatedEvent>)aggregate).Apply(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            "registry_created",
            EventTime,
            RegistryId,
            Identifier,
            Name,
            Description,
            RegistryOwner,
            null,
            null);
}