#nullable enable

using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Versioned;
using Altinn.ResourceRegistry.Models;
using Altinn.ResourceRegistry.Tests.Utils;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using System;
using Xunit;

namespace Altinn.ResourceRegistry.Tests.Models;

public class RequestConditionTests
{
    public static TheoryData<string, string, VersionedEntityConditionResult> IsMatchData => new()
    {
        { "foo", "foo", VersionedEntityConditionResult.Succeeded },
        { "foo", "bar", VersionedEntityConditionResult.Failed },
    };

    [Theory]
    [MemberData(nameof(IsMatchData))]
    public void IsMatch(string ifMatches, string etag, VersionedEntityConditionResult result)
    {
        var condition = RequestCondition.IsMatch(ifMatches);
        condition.Validate(Entity.Matchable(etag)).Should().Be(result);
    }

    public static TheoryData<string, string, bool, VersionedEntityConditionResult> IsDifferentData => new()
    {
        { "foo", "foo", false, VersionedEntityConditionResult.Failed },
        { "foo", "bar", false, VersionedEntityConditionResult.Succeeded },
        { "foo", "foo", true, VersionedEntityConditionResult.Unmodified },
        { "foo", "bar", true, VersionedEntityConditionResult.Succeeded },
    };

    [Theory]
    [MemberData(nameof(IsDifferentData))]
    public void IsDifferent(string ifNoneMatches, string etag, bool isRead, VersionedEntityConditionResult result)
    {
        var condition = RequestCondition.IsDifferent(ifNoneMatches, isRead);
        condition.Validate(Entity.Matchable(etag)).Should().Be(result);
    }

    public static TheoryData<DateTimeOffset, DateTimeOffset, VersionedEntityConditionResult> IsUnmodifiedSinceData => new()
    {
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), VersionedEntityConditionResult.Succeeded },
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2022, 01, 01, 01, 01, 01, TimeSpan.Zero), VersionedEntityConditionResult.Succeeded },
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2023, 03, 03, 03, 03, 03, TimeSpan.Zero), VersionedEntityConditionResult.Failed },
    };

    [Theory]
    [MemberData(nameof(IsUnmodifiedSinceData))]
    public void IsUnmodifiedSince(DateTimeOffset ifUnmodifiedSince, DateTimeOffset lastModifiedAt, VersionedEntityConditionResult result)
    {
        var condition = RequestCondition.IsUnmodifiedSince<string>(new(ifUnmodifiedSince));
        condition.Validate(Entity.LastModified<string>(lastModifiedAt)).Should().Be(result);
    }

    public static TheoryData<DateTimeOffset, DateTimeOffset, bool, VersionedEntityConditionResult> IsModifiedSinceData => new()
    {
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), false, VersionedEntityConditionResult.Failed },
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2022, 01, 01, 01, 01, 01, TimeSpan.Zero), false, VersionedEntityConditionResult.Failed },
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2023, 03, 03, 03, 03, 03, TimeSpan.Zero), false, VersionedEntityConditionResult.Succeeded },
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), true, VersionedEntityConditionResult.Unmodified },
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2022, 01, 01, 01, 01, 01, TimeSpan.Zero), true, VersionedEntityConditionResult.Unmodified },
        { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2023, 03, 03, 03, 03, 03, TimeSpan.Zero), true, VersionedEntityConditionResult.Succeeded },
    };

    [Theory]
    [MemberData(nameof(IsModifiedSinceData))]
    public void IsModifiedSince(DateTimeOffset ifModifiedSince, DateTimeOffset lastModifiedAt, bool isRead, VersionedEntityConditionResult result)
    {
        var condition = RequestCondition.IsModifiedSince<string>(new(ifModifiedSince), isRead);
        condition.Validate(Entity.LastModified<string>(lastModifiedAt)).Should().Be(result);
    }

    private static class Entity
    {
        public static MatchableEntity<T> Matchable<T>(T value)
            where T : notnull, IEquatable<T>
            => new(value);

        public static LastModifiedEntity<T> LastModified<T>(DateTimeOffset lastModified)
            where T : notnull, IEquatable<T>
            => new(lastModified);
    }

    private readonly record struct MatchableEntity<T>(T Value) : IVersionEquatable<T>
        where T : notnull, IEquatable<T>
    {
        public bool ModifiedSince(HttpDateTimeHeaderValue other)
            => ThrowHelper.ThrowNotSupportedException<bool>();

        public bool VersionEquals(T other)
            => Value.Equals(other);
    }

    private readonly record struct LastModifiedEntity<T>(DateTimeOffset LastModified) : IVersionEquatable<T>
        where T : notnull, IEquatable<T>
    {
        public bool ModifiedSince(HttpDateTimeHeaderValue other)
            => LastModified > other;

        public bool VersionEquals(T other)
            => ThrowHelper.ThrowNotSupportedException<bool>();
    }
}
