﻿namespace Altinn.ResourceRegistry.Persistence.Aggregates;

/// <summary>
/// Used as a generic <see langword="new" /> constraint on <see cref="IAggregateEventHandler{TAggregate,TEvent}" />.
/// </summary>
/// <typeparam name="TAggregate">The concrete aggregate type</typeparam>
/// <typeparam name="TEvent">The concrete event type</typeparam>
internal interface IAggregateFactory<TAggregate, TEvent>
    where TAggregate : Aggregate<TAggregate, TEvent>, IAggregateFactory<TAggregate, TEvent>, IAggregateEventHandler<TAggregate, TEvent>
    where TEvent : IAggregateEvent<TAggregate, TEvent>
{
    /// <summary>
    /// Creates a new aggregate with the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="timeProvider">The time provider</param>
    /// <param name="id">The aggregate id</param>
    /// <param name="repository">A <see cref="IAggregateRepository{TAggregate, TEvent}"/> for saving changes to the aggregate</param>
    /// <returns><typeparamref name="TAggregate"/></returns>
    static abstract TAggregate New(TimeProvider timeProvider, Guid id, IAggregateRepository<TAggregate, TEvent> repository);

    /// <summary>
    /// Creates a new aggregate with the specified <paramref name="id"/> from the specified <paramref name="events"/>.
    /// </summary>
    /// <param name="timeProvider">The time provider</param>
    /// <param name="id">The aggregate id</param>
    /// <param name="repository">A <see cref="IAggregateRepository{TAggregate, TEvent}"/> for saving changes to the aggregate</param>
    /// <param name="events">The events that are loaded into the aggregate and marked as committed</param>
    /// <returns><typeparamref name="TAggregate"/></returns>
    static TAggregate LoadFrom(TimeProvider timeProvider, Guid id, IAggregateRepository<TAggregate, TEvent> repository, IEnumerable<TEvent> events)
    {
        var aggregate = TAggregate.New(timeProvider, id, repository);
        
        foreach (var e in events)
        {
            aggregate.ApplyEvent(e);
        }

        aggregate.Commit();
        return aggregate;
    }

    /// <summary>
    /// Creates a new aggregate with the specified <paramref name="id"/> from the specified <paramref name="events"/>.
    /// </summary>
    /// <param name="timeProvider">The time provider</param>
    /// <param name="id">The aggregate id</param>
    /// <param name="repository">A <see cref="IAggregateRepository{TAggregate, TEvent}"/> for saving changes to the aggregate</param>
    /// <param name="events">The events that are loaded into the aggregate and marked as committed</param>
    /// <returns><typeparamref name="TAggregate"/></returns>
    static async Task<TAggregate> LoadFrom(TimeProvider timeProvider, Guid id, IAggregateRepository<TAggregate, TEvent> repository, IAsyncEnumerable<TEvent> events)
    {
        var aggregate = TAggregate.New(timeProvider, id, repository);
        
        await foreach (var e in events)
        {
            aggregate.ApplyEvent(e);
        }
            
        aggregate.Commit();
        return aggregate;
    }
}
