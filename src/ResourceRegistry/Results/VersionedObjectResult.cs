﻿#nullable enable

using Altinn.ResourceRegistry.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Altinn.ResourceRegistry.Results;

/// <summary>
/// A versioned object result.
/// </summary>
public class VersionedObjectResult : ObjectResult
{
    /// <summary>
    /// Gets the version tag.
    /// </summary>
    public string? VersionTag { get; }

    /// <summary>
    /// Gets when this version was last modified.
    /// </summary>
    public HttpDateTimeHeaderValue? LastModified { get; }

    /// <summary>
    /// Creates a new instance of <see cref="VersionedObjectResult"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="versionTag">The version tag.</param>
    /// <param name="lastModified">The modified at timestamp.</param>
    public VersionedObjectResult(object? value, string? versionTag, HttpDateTimeHeaderValue? lastModified)
        : base(value)
    {
        VersionTag = versionTag;
        LastModified = lastModified;
    }

    /// <inheritdoc/>
    public override Task ExecuteResultAsync(ActionContext context)
    {
        var headers = context.HttpContext.Response.Headers;
        if (VersionTag is { } tag)
        {
            headers.ETag = tag.ToString();
        }

        if (LastModified is { } modifiedAt)
        {
            headers.LastModified = HeaderUtilities.FormatDate(modifiedAt.Value);
        }

        return base.ExecuteResultAsync(context);
    }
}
