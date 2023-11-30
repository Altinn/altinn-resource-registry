using System.Diagnostics.CodeAnalysis;

namespace Altinn.ResourceRegistry.Persistence.Aggregates;

/// <summary>
/// A base class for aggregates.
/// </summary>
/// <typeparam name="TAggregate">The concrete aggregate type</typeparam>
/// <typeparam name="TEvent">The concrete event type</typeparam>
internal abstract class Aggregate<TAggregate, TEvent>
    : IAggregateEventHandler<TAggregate, TEvent>
    where TAggregate : Aggregate<TAggregate, TEvent>, IAggregateEventHandler<TAggregate, TEvent>, IAggregateFactory<TAggregate, TEvent>
    where TEvent : IAggregateEvent<TAggregate, TEvent>
{
    [SuppressMessage(
        "StyleCop.CSharp.SpacingRules", 
        "SA1010:Opening square brackets should be spaced correctly", 
        Justification = "https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3687")]
    private readonly List<TEvent> _events = [];
    private int _committed = 0;

    /// <summary>
    /// Gets a value indicating wheather the aggregate is initialized.
    /// </summary>
    public abstract bool IsInitialized { get; }

    /// <summary>
    /// Gets a value indicating wheather the aggregate is deleted.
    /// </summary>
    public abstract bool IsDeleted { get; }

    /// <summary>
    /// Gets when this aggregate was created.
    /// </summary>
    public DateTimeOffset CreatedAt => InitializedThis._events[0].EventTime;

    /// <summary>
    /// Gets when this aggregate was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt => InitializedThis._events[^1].EventTime;

    /// <summary>
    /// Asserts that the aggregate is initialized.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the aggregate is not initialized</exception>
    protected void AssertInitialized()
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException("Aggregate not initialized");
        }

        if (IsDeleted)
        {
            throw new InvalidOperationException("Aggregate is deleted");
        }
    }

    /// <summary>
    /// Gets a self-reference that throws an exception if the aggregate is not initialized.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the aggregate is not initialized</exception>
    protected TAggregate InitializedThis
    {
        get
        {
            AssertInitialized();

            return (TAggregate)this;
        }
    }

    /// <summary>
    /// Gets the aggregate id.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Constructs a new <see cref="Aggregate{TAggregate,TEvent}"/> with the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The aggregate id</param>
    protected Aggregate(Guid id)
    {
        Id = id;
    }

    /// <summary>
    /// Mark all events as committed.
    /// </summary>
    public void Commit()
    {
        _committed = _events.Count;
    }

    /// <summary>
    /// Gets the uncommitted events.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{T}"/> of <typeparamref name="TEvent"/>s containing all uncommitted events</returns>
    internal IEnumerable<TEvent> GetUncommittedEvents()
        => _events.Skip(_committed);

    /// <summary>
    /// Apply the <paramref name="event"/> to the aggregate.
    /// </summary>
    /// <param name="event">The event</param>
    protected void Apply(TEvent @event)
    {
        @event.ApplyTo((TAggregate)this);
        _events.Add(@event);
    }

    /// <inheritdoc />
    void IAggregateEventHandler<TEvent>.Apply(TEvent @event)
        => Apply(@event);
}
