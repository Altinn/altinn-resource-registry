#nullable enable

using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;
using Altinn.ResourceRegistry.Core.Models;
using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// Helper methods for <see cref="RequestConditions{T}"/>.
/// </summary>
public static class RequestConditions
{
    /// <inheritdoc cref="RequestConditions{T}.Empty"/>
    /// <typeparam name="T">The etag type</typeparam>
    public static RequestConditions<T> Empty<T>()
        where T : notnull
        => RequestConditions<T>.Empty;

    /// <summary>
    /// Creates a new <see cref="RequestConditions{T}"/> from a set of <see cref="RequestCondition{T}"/>.
    /// </summary>
    /// <typeparam name="T">The etag type</typeparam>
    /// <param name="conditions">The conditions</param>
    /// <returns>The new <see cref="RequestConditions{T}"/></returns>
    public static RequestConditions<T> Create<T>(IEnumerable<RequestCondition<T>> conditions)
        where T : notnull
    {
        if (conditions is ICollection collection && collection.Count == 0)
        {
            return RequestConditions<T>.Empty;
        }

        return Create(conditions.ToImmutableArray());
    }

    /// <summary>
    /// Creates a new <see cref="RequestConditions{T}"/> from a set of <see cref="RequestCondition{T}"/>.
    /// </summary>
    /// <typeparam name="T">The etag type</typeparam>
    /// <param name="conditions">The conditions</param>
    /// <returns>The new <see cref="RequestConditions{T}"/></returns>
    public static RequestConditions<T> Create<T>(ImmutableArray<RequestCondition<T>> conditions)
        where T : notnull
        => new(conditions);

    /// <summary>
    /// Returns the etag type for a <see cref="RequestConditions{T}"/> type.
    /// </summary>
    /// <param name="requestConditionsType">The <see cref="RequestConditions{T}"/> type.</param>
    /// <returns>
    /// The type argument of the <paramref name="requestConditionsType"/> parameter, 
    /// if the <paramref name="requestConditionsType"/> is a closed generic request 
    /// conditions type, otherwise <see langword="null"/>.
    /// </returns>
    public static Type? GetETagType(Type requestConditionsType)
    {
        Guard.IsNotNull(requestConditionsType);

        if (requestConditionsType.IsGenericType && !requestConditionsType.IsGenericTypeDefinition)
        {
            var genericType = requestConditionsType.GetGenericTypeDefinition();
            if (ReferenceEquals(genericType, typeof(RequestConditions<>)))
            {
                return requestConditionsType.GetGenericArguments()[0];
            }
        }

        return null;
    }

    /// <summary>
    /// <see cref="IModelBinderProvider"/> for <see cref="RequestConditions{T}"/>.
    /// </summary>
    internal class ModelBinderProvider : IModelBinderProvider
    {
        /// <summary>
        /// Gets a singleton instance of <see cref="ModelBinderProvider"/>.
        /// </summary>
        public static IModelBinderProvider Instance { get; } = new ModelBinderProvider();

        private ModelBinderProvider()
        { 
        }

        /// <inheritdoc/>
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            Guard.IsNotNull(context);

            if (GetETagType(context.Metadata.ModelType) is { } etagType)
            {
                return new BinderTypeModelBinder(typeof(ModelBinder<>).MakeGenericType(etagType));
            }

            return null;
        }
    }

    /// <summary>
    /// <see cref="IBindingSourceMetadata"/> for <see cref="RequestConditions{T}"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    internal class BindingSourceAttribute : Attribute, IBindingSourceMetadata
    {
        /// <inheritdoc/>
        public BindingSource? BindingSource => BindingSource.Header;
    }

    private class ModelBinder<T> : IModelBinder
        where T : notnull
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            Guard.IsNotNull(bindingContext);

            var modelName = bindingContext.ModelName;

            var headers = bindingContext.HttpContext.Request.Headers;
            if (headers == null)
            {
                bindingContext.Result = ModelBindingResult.Success(Empty<T>());
                return Task.CompletedTask;
            }

            var method = bindingContext.HttpContext.Request.Method;
            var isReadRequest = HttpMethods.IsGet(method) || HttpMethods.IsHead(method);
            var hasErrors = false;
            var builder = ImmutableArray.CreateBuilder<RequestCondition<T>>();
            
            foreach (var etag in ReadETags(modelName, bindingContext.ModelState, "If-Match", headers.IfMatch, ref hasErrors))
            {
                builder.Add(RequestCondition.IsMatches(etag));
            }

            foreach (var etag in ReadETags(modelName, bindingContext.ModelState, "If-None-Match", headers.IfNoneMatch, ref hasErrors))
            {
                builder.Add(RequestCondition.IsDifferent(etag, isReadRequest));
            }

            // only consult modified-since headers if we don't have any etag headers
            if (builder.Count == 0)
            {
                foreach (var date in ReadDates(modelName, bindingContext.ModelState, "If-Modified-Since", headers.IfModifiedSince, ref hasErrors))
                {
                    builder.Add(RequestCondition.IsModifiedSince<T>(date, isReadRequest));
                }

                foreach (var date in ReadDates(modelName, bindingContext.ModelState, "If-Unmodified-Since", headers.IfUnmodifiedSince, ref hasErrors))
                {
                    builder.Add(RequestCondition.IsUnmodifiedSince<T>(date));
                }
            }

            if (hasErrors)
            {
                return Task.CompletedTask;
            }

            var result = builder.Count == 0 ? Empty<T>() : Create(builder.ToImmutable());
            bindingContext.Result = ModelBindingResult.Success(result);
            return Task.CompletedTask;
        }

        private static ImmutableArray<T> ReadETags(string modelName, ModelStateDictionary modelState, string headerName, StringValues headerValues, ref bool hasErrors)
        {
            Debug.Assert(modelState != null);
            Debug.Assert(headerName != null);

            if (StringValues.IsNullOrEmpty(headerValues))
            {
                return [];
            }

            if (!EntityTagHeaderValue.TryParseList(headerValues, out var etags))
            {
                modelState.TryAddModelError(modelName, $"Invalid {headerName} header.");
                hasErrors = true;
                return [];
            }

            var builder = ImmutableArray.CreateBuilder<T>(etags.Count);
            foreach (var etag in etags)
            {
                if (!etag.IsWeak)
                {
                    modelState.TryAddModelError(modelName, $"Invalid {headerName} header. Only weak etags are supported.");
                    hasErrors = true;
                    return [];
                }

                if (!Opaque<T>.TryParse(etag.Tag.AsSpan()[1..^1], null, out var value))
                {
                    modelState.TryAddModelError(modelName, $"Invalid {headerName} header. Invalid etag.");
                    hasErrors = true;
                    return [];
                }

                builder.Add(value.Value);
            }

            return builder.MoveToImmutable();
        }

        private static ImmutableArray<HttpDateTimeHeaderValue> ReadDates(string modelName, ModelStateDictionary modelState, string headerName, StringValues headerValues, ref bool hasErrors)
        {
            Debug.Assert(modelState != null);
            Debug.Assert(headerName != null);

            if (StringValues.IsNullOrEmpty(headerValues))
            {
                return [];
            }

            var builder = ImmutableArray.CreateBuilder<HttpDateTimeHeaderValue>(headerValues.Count);
            foreach (var headerValue in headerValues)
            {
                if (!HeaderUtilities.TryParseDate(headerValue, out var date))
                {
                    modelState.TryAddModelError(modelName, $"Invalid {headerName} header.");
                    hasErrors = true;
                    return [];
                }

                builder.Add(new(date));
            }

            return builder.MoveToImmutable();
        }
    }
}

/// <summary>
/// A set of <see cref="RequestCondition{T}"/>.
/// </summary>
/// <typeparam name="T">The etag type</typeparam>
[RequestConditions.BindingSource]
public sealed class RequestConditions<T>
    : IReadOnlyList<RequestCondition<T>>
    , IVersionedEntityCondition<T>
    where T : notnull
{
    private readonly ImmutableArray<RequestCondition<T>> _conditions;

    /// <summary>
    /// Gets an empty <see cref="RequestConditions{T}"/>.
    /// </summary>
    public static RequestConditions<T> Empty { get; } = new(ImmutableArray<RequestCondition<T>>.Empty);

    /// <summary>
    /// Constructs a new instance of <see cref="RequestConditions{T}"/>.
    /// </summary>
    /// <param name="conditions">The conditions</param>
    public RequestConditions(ImmutableArray<RequestCondition<T>> conditions)
    {
        _conditions = conditions;
    }

    /// <summary>
    /// Constructs a new instance of <see cref="RequestConditions{T}"/>.
    /// </summary>
    /// <param name="conditions">The conditions</param>
    public RequestConditions(IEnumerable<RequestCondition<T>> conditions)
        : this(conditions.ToImmutableArray())
    {
    }

    /// <inheritdoc/>
    public RequestCondition<T> this[int index] => _conditions[index];

    /// <inheritdoc/>
    public int Count => _conditions.Length;

    /// <inheritdoc/>
    public VersionedEntityConditionResult Validate<TEntity>(TEntity entity)
        where TEntity : IVersionEquatable<T>
    {
        var result = VersionedEntityConditionResult.Succeeded;
        foreach (var condition in _conditions)
        {
            result = VersionedEntityConditionResult.Max(result, condition.Validate(entity));
            if (result == VersionedEntityConditionResult.Failed)
            {
                break;
            }
        }

        return result;
    }

    /// <inheritdoc cref="ImmutableArray{T}.GetEnumerator()"/>
    public ImmutableArray<RequestCondition<T>>.Enumerator GetEnumerator()
        => _conditions.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator<RequestCondition<T>> IEnumerable<RequestCondition<T>>.GetEnumerator()
        => ((IEnumerable<RequestCondition<T>>)_conditions).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => ((IEnumerable)_conditions).GetEnumerator();
}

/// <summary>
/// Helpers for <see cref="RequestCondition{T}"/>.
/// </summary>
public static class RequestCondition
{
    /// <inheritdoc cref="RequestCondition{T}.IsMatch(T)"/>
    public static RequestCondition<T> IsMatches<T>(T value)
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
            RequestCondition<T>.Mode.IsMatch => Check(entity.VersionEquals(_etag!), VersionedEntityConditionResult.Failed),
            RequestCondition<T>.Mode.IsDifferentRead => Check(!entity.VersionEquals(_etag!), VersionedEntityConditionResult.Unmodified),
            RequestCondition<T>.Mode.IsDifferent => Check(!entity.VersionEquals(_etag!), VersionedEntityConditionResult.Failed),
            RequestCondition<T>.Mode.IsModifiedSinceRead => Check(entity.ModifiedSince(_date!.Value), VersionedEntityConditionResult.Unmodified),
            RequestCondition<T>.Mode.IsModifiedSince => Check(entity.ModifiedSince(_date!.Value), VersionedEntityConditionResult.Failed),
            RequestCondition<T>.Mode.IsUnmodifiedSince => Check(!entity.ModifiedSince(_date!.Value), VersionedEntityConditionResult.Failed),
            _ => ThrowHelper.ThrowInvalidOperationException<VersionedEntityConditionResult>("Invalid mode"),
        };

        static VersionedEntityConditionResult Check(bool check, VersionedEntityConditionResult ifError)
            => check ? VersionedEntityConditionResult.Succeeded : ifError;
    }

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
