using System.Diagnostics;
using System.Numerics;
using CommunityToolkit.Diagnostics;

namespace Altinn.ResourceRegistry.Core.Models;

/// <summary>
/// An object for which the version and last modification date can be compared.
/// </summary>
/// <typeparam name="T">The version type.</typeparam>
public interface IVersionEquatable<T>
    where T : notnull
{
    /// <summary>
    /// Indicates whether the current object's version is equal to the provided version value.
    /// </summary>
    /// <param name="other">The value to compare with this object's version.</param>
    /// <returns>
    /// <see langword="true"/> if the current object's version is equal to the <paramref name="other"/> parameter;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool VersionEquals(T other);

    /// <summary>
    /// Indicates whether the current object's time of modification is greater than the provided <see cref="DateTimeOffset"/> value.
    /// </summary>
    /// <param name="other">The value to compare with this object's modification time.</param>
    /// <returns>
    /// <see langword="true"/> if the current object's modification time is greater than the <paramref name="other"/> parameter;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>This comparison must use at most second precision.</remarks>
    bool ModifiedSince(HttpDateTimeHeaderValue other);
}

/// <summary>
/// A condition for a <see cref="IVersionEquatable{T}"/>.
/// </summary>
/// <typeparam name="T">The version type.</typeparam>
public interface IVersionedEntityCondition<T>
{
    /// <summary>
    /// Validate all request conditions against an entity,
    /// short-circuiting if any of the conditions return
    /// <see cref="VersionedEntityConditionResult.Failed"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="entity">The entity</param>
    /// <returns>A <see cref="VersionedEntityConditionResult"/></returns>
    VersionedEntityConditionResult Validate<TEntity>(TEntity entity)
        where TEntity : notnull, IVersionEquatable<T>;
}

/// <summary>
/// A <see cref="IVersionedEntityCondition{T}"/> result.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct VersionedEntityConditionResult
    : IEquatable<VersionedEntityConditionResult>
    , IComparable<VersionedEntityConditionResult>
    , IEqualityOperators<VersionedEntityConditionResult, VersionedEntityConditionResult, bool>
    , IComparisonOperators<VersionedEntityConditionResult, VersionedEntityConditionResult, bool>
{
    private readonly int _value;

    /// <summary>
    /// Request condition matched.
    /// </summary>
    public static VersionedEntityConditionResult Succeeded { get; } = new(0);

    /// <summary>
    /// Request condition did not match, but the condition was an If-Modified-Since.
    /// </summary>
    public static VersionedEntityConditionResult Unmodified { get; } = new(1);

    /// <summary>
    /// Request condition did not match.
    /// </summary>
    public static VersionedEntityConditionResult Failed { get; } = new(2);

    private VersionedEntityConditionResult(int value)
    {
        _value = value;
    }

    /// <inheritdoc/>
    public override string ToString()
        => _value switch
        {
            0 => nameof(Succeeded),
            1 => nameof(Unmodified),
            2 => nameof(Failed),
            _ => ThrowHelper.ThrowInvalidOperationException<string>("Invalid result value"),
        };

    /// <inheritdoc/>
    public int CompareTo(VersionedEntityConditionResult other)
    {
        return _value.CompareTo(other._value);
    }

    /// <summary>
    /// Returns the maximum of two <see cref="VersionedEntityConditionResult"/> values.
    /// </summary>
    /// <param name="left">The left value</param>
    /// <param name="right">The right value</param>
    /// <returns><paramref name="left"/> if <c><paramref name="left"/> &gt; <paramref name="right"/></c>, else <paramref name="right"/>.</returns>
    public static VersionedEntityConditionResult Max(VersionedEntityConditionResult left, VersionedEntityConditionResult right)
    {
        return left > right ? left : right;
    }

    public static bool operator >(VersionedEntityConditionResult left, VersionedEntityConditionResult right)
        => left.CompareTo(right) > 0;

    public static bool operator <(VersionedEntityConditionResult left, VersionedEntityConditionResult right)
        => left.CompareTo(right) < 0;

    public static bool operator <=(VersionedEntityConditionResult left, VersionedEntityConditionResult right)
        => left.CompareTo(right) <= 0;

    public static bool operator >=(VersionedEntityConditionResult left, VersionedEntityConditionResult right)
        => left.CompareTo(right) >= 0;
}

/// <summary>
/// Extension methods for <see cref="IVersionedEntityCondition{T}"/>.
/// </summary>
public static class VersionedEntityConditionExtensions
{
    /// <summary>
    /// Create a new <see cref="IVersionedEntityCondition{T}"/> over the selected property.
    /// </summary>
    /// <typeparam name="TOuter">The outer type</typeparam>
    /// <typeparam name="TInner">The inner type</typeparam>
    /// <param name="self">The original entity condition</param>
    /// <param name="converter">The converter from <typeparamref name="TOuter"/> to <typeparamref name="TInner"/></param>
    /// <returns>A new <see cref="IVersionedEntityCondition{T}"/></returns>
    public static IVersionedEntityCondition<TInner> Select<TOuter, TInner>(this IVersionedEntityCondition<TOuter> self, Func<TOuter, TInner> converter)
        => new MappedVersionEntityCondition<TInner, TOuter>(self, converter);

    private class MappedVersionEntityCondition<TInner, TOuter>
        : IVersionedEntityCondition<TInner>
    {
        private readonly IVersionedEntityCondition<TOuter> _inner;
        private readonly Func<TOuter, TInner> _converter;

        public MappedVersionEntityCondition(IVersionedEntityCondition<TOuter> inner, Func<TOuter, TInner> converte)
        {
            Guard.IsNotNull(inner);
            Guard.IsNotNull(converte);

            _inner = inner;
            _converter = converte;
        }

        public VersionedEntityConditionResult Validate<TEntity>(TEntity entity) 
            where TEntity : IVersionEquatable<TInner>
        {
            return _inner.Validate(new MappedEntity<TEntity>(entity, _converter));
        }

        private readonly struct MappedEntity<TEntity>
            : IVersionEquatable<TOuter>
            where TEntity : IVersionEquatable<TInner>
        {
            private readonly TEntity _entity;
            private readonly Func<TOuter, TInner> _converter;

            public MappedEntity(TEntity entity, Func<TOuter, TInner> mapper)
            {
                _entity = entity;
                _converter = mapper;
            }

            public bool ModifiedSince(HttpDateTimeHeaderValue other)
            {
                return _entity.ModifiedSince(other);
            }

            public bool VersionEquals(TOuter other)
            {
                return _entity.VersionEquals(_converter(other));
            }
        }
    }
}
