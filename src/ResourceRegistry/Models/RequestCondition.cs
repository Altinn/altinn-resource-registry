#nullable enable

using System.ComponentModel;
using System.Diagnostics;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Versioned;
using CommunityToolkit.Diagnostics;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// Helpers for <see cref="RequestCondition{T}"/>.
/// </summary>
public static class RequestCondition
{
    /// <inheritdoc cref="RequestCondition{T}.IsMatch(T)"/>
    public static RequestCondition<T> IsMatch<T>(T value)
        where T : notnull
        => RequestCondition<T>.IsMatch(value);

    /// <inheritdoc cref="RequestCondition{T}.IsDifferent(T, bool)"/>
    public static RequestCondition<T> IsDifferent<T>(T value, bool isRead)
        where T : notnull
        => RequestCondition<T>.IsDifferent(value, isRead);

    /// <inheritdoc cref="RequestCondition{T}.IsModifiedSince(HttpDateTimeHeaderValue, bool)"/>
    public static RequestCondition<T> IsModifiedSince<T>(HttpDateTimeHeaderValue date, bool isRead)
        where T : notnull
        => RequestCondition<T>.IsModifiedSince(date, isRead);

    /// <inheritdoc cref="RequestCondition{T}.IsUnmodifiedSince(HttpDateTimeHeaderValue)"/>
    public static RequestCondition<T> IsUnmodifiedSince<T>(HttpDateTimeHeaderValue date)
        where T : notnull
        => RequestCondition<T>.IsUnmodifiedSince(date);
}

/// <summary>
/// A condition for a request.
/// </summary>
/// <typeparam name="T">The etag type</typeparam>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class RequestCondition<T>
    : IVersionedEntityCondition<T>
    where T : notnull
{
    private readonly HttpDateTimeHeaderValue? _date;
    private readonly T? _etag;
    private readonly Mode _mode;

    /// <summary>
    /// Create a <see cref="RequestCondition{T}"/> for an <c>If-Match</c> header.
    /// </summary>
    /// <param name="value">The etag to match against.</param>
    /// <returns>The <see cref="RequestCondition{T}"/></returns>
    public static RequestCondition<T> IsMatch(T value)
        => new(value, null, Mode.IsMatch);

    /// <summary>
    /// Create a <see cref="RequestCondition{T}"/> for an <c>If-None-Match</c> header.
    /// </summary>
    /// <param name="value">The etag to match against.</param>
    /// <param name="isRead">Wheather or not this is a read request.</param>
    /// <returns>The <see cref="RequestCondition{T}"/></returns>
    public static RequestCondition<T> IsDifferent(T value, bool isRead)
        => new(value, null, isRead ? Mode.IsDifferentRead : Mode.IsDifferent);

    /// <summary>
    /// Create a <see cref="RequestCondition{T}"/> for an <c>If-Modified-Since</c> header.
    /// </summary>
    /// <param name="date">The date to match against</param>
    /// <param name="isRead">Wheather or not this is a read request.</param>
    /// <returns>The <see cref="RequestCondition{T}"/></returns>
    public static RequestCondition<T> IsModifiedSince(HttpDateTimeHeaderValue date, bool isRead)
        => new(default, date, isRead ? Mode.IsModifiedSinceRead : Mode.IsModifiedSince);

    /// <summary>
    /// Create a <see cref="RequestCondition{T}"/> for an <c>If-Unmodified-Since</c> header.
    /// </summary>
    /// <param name="date">The date to match against</param>
    /// <returns>The <see cref="RequestCondition{T}"/></returns>
    public static RequestCondition<T> IsUnmodifiedSince(HttpDateTimeHeaderValue date)
        => new(default, date, Mode.IsUnmodifiedSince);

    private RequestCondition(T? etag, HttpDateTimeHeaderValue? date, Mode mode)
    {
        _etag = etag;
        _date = date;
        _mode = mode;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The rules for what result to return can be found on
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/304"/> and
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/412"/>.
    /// </remarks>
    public VersionedEntityConditionResult Validate<TEntity>(TEntity entity)
        where TEntity : notnull, IVersionEquatable<T>
    {
        return _mode switch
        {
            Mode.IsMatch => Check(entity.VersionEquals(_etag!), VersionedEntityConditionResult.Failed),
            Mode.IsDifferentRead => Check(!entity.VersionEquals(_etag!), VersionedEntityConditionResult.Unmodified),
            Mode.IsDifferent => Check(!entity.VersionEquals(_etag!), VersionedEntityConditionResult.Failed),
            Mode.IsModifiedSinceRead => Check(entity.ModifiedSince(_date!.Value), VersionedEntityConditionResult.Unmodified),
            Mode.IsModifiedSince => Check(entity.ModifiedSince(_date!.Value), VersionedEntityConditionResult.Failed),
            Mode.IsUnmodifiedSince => Check(!entity.ModifiedSince(_date!.Value), VersionedEntityConditionResult.Failed),
            _ => ThrowHelper.ThrowInvalidOperationException<VersionedEntityConditionResult>("Invalid mode"),
        };

        static VersionedEntityConditionResult Check(bool check, VersionedEntityConditionResult ifError)
            => check ? VersionedEntityConditionResult.Succeeded : ifError;
    }

    /// <summary>
    /// Debugger display string.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal string DebuggerDisplay
        => _mode switch
        {
            Mode.IsMatch => $"If-Match: {_etag}",
            Mode.IsDifferent or Mode.IsDifferentRead => $"If-None-Match: {_etag}",
            Mode.IsModifiedSince or Mode.IsModifiedSinceRead => $"If-Modified-Since: {_date}",
            Mode.IsUnmodifiedSince => $"If-Unmodified-Since: {_date}",
            _ => "Invalid",
        };

    private enum Mode
    {
        IsMatch,
        IsDifferentRead,
        IsDifferent,
        IsModifiedSinceRead,
        IsModifiedSince,
        IsUnmodifiedSince,
    }
}
