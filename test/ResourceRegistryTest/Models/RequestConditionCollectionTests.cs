﻿#nullable enable

using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Versioned;
using Altinn.ResourceRegistry.Models;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using Xunit;

namespace Altinn.ResourceRegistry.Tests.Models;

public class RequestConditionCollectionTests
{
    public static TheoryData<VersionedEntityConditionResult> AllResults => new()
    {
        VersionedEntityConditionResult.Succeeded,
        VersionedEntityConditionResult.Unmodified,
        VersionedEntityConditionResult.Failed,
    };

    [Fact]
    public void Empty_Collection_Matches_Any_Entity()
    {
        var collection = RequestConditionCollection.Empty<Nil>();

        collection.Validate(default(Nil))
            .Should().Be(VersionedEntityConditionResult.Succeeded);
    }

    [Theory]
    [MemberData(nameof(AllResults))]
    public void Collection_With_Single_Condition_Matches_Entity_If_Condition_Matches(VersionedEntityConditionResult expected)
    {
        var collection = RequestConditionCollection.Create([
            ConstCondition.For(expected)
        ]);

        collection.Validate(default(Nil))
            .Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(AllResults))]
    public void Collection_With_Multiple_Identical_Conditions_Matches_Entity_If_Condition_Matches(VersionedEntityConditionResult expected)
    {
        var collection = RequestConditionCollection.Create([
            ConstCondition.For(expected),
            ConstCondition.For(expected),
            ConstCondition.For(expected),
        ]);

        collection.Validate(default(Nil))
            .Should().Be(expected);
    }

    [Fact]
    public void Collection_Returns_Most_Severe_Result()
    {
        RequestConditionCollection.Create([
            ConstCondition.Succeeded,
            ConstCondition.Unmodified,
        ]).Validate(default(Nil))
            .Should().Be(VersionedEntityConditionResult.Unmodified);

        RequestConditionCollection.Create([
            ConstCondition.Unmodified,
            ConstCondition.Succeeded,
        ]).Validate(default(Nil))
            .Should().Be(VersionedEntityConditionResult.Unmodified);

        RequestConditionCollection.Create([
            ConstCondition.Succeeded,
            ConstCondition.Unmodified,
            ConstCondition.Succeeded,
            ConstCondition.Succeeded,
        ]).Validate(default(Nil))
            .Should().Be(VersionedEntityConditionResult.Unmodified);

        RequestConditionCollection.Create([
            ConstCondition.Succeeded,
            ConstCondition.Unmodified,
            ConstCondition.Succeeded,
            ConstCondition.Succeeded,
            ConstCondition.Succeeded,
            ConstCondition.Unmodified,
            ConstCondition.Failed,
            ConstCondition.Succeeded,
        ]).Validate(default(Nil))
            .Should().Be(VersionedEntityConditionResult.Failed);
    }

    private class ConstCondition(VersionedEntityConditionResult result)
        : IVersionedEntityCondition<Nil>
    {
        public static IVersionedEntityCondition<Nil> Succeeded { get; } = new ConstCondition(VersionedEntityConditionResult.Succeeded);
        public static IVersionedEntityCondition<Nil> Unmodified { get; } = new ConstCondition(VersionedEntityConditionResult.Unmodified);
        public static IVersionedEntityCondition<Nil> Failed { get; } = new ConstCondition(VersionedEntityConditionResult.Failed);

        public static IVersionedEntityCondition<Nil> For(VersionedEntityConditionResult result)
        {
            if (result == VersionedEntityConditionResult.Succeeded)
            {
                return Succeeded;
            }
            else if (result == VersionedEntityConditionResult.Unmodified)
            {
                return Unmodified;
            }
            else if (result == VersionedEntityConditionResult.Failed)
            {
                return Failed;
            }

            return ThrowHelper.ThrowArgumentOutOfRangeException<IVersionedEntityCondition<Nil>>(nameof(result));
        }

        public VersionedEntityConditionResult Validate<TEntity>(TEntity entity) 
            where TEntity : notnull, IVersionEquatable<Nil>
            => result;
    }

    private readonly struct Nil
        : IVersionEquatable<Nil>
    {
        public bool ModifiedSince(HttpDateTimeHeaderValue other)
        {
            return ThrowHelper.ThrowNotSupportedException<bool>();
        }

        public bool VersionEquals(Nil other)
        {
            return ThrowHelper.ThrowNotSupportedException<bool>();
        }
    }
}
