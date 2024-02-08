﻿using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Models;
using Altinn.ResourceRegistry.Results;
using Altinn.ResourceRegistry.Tests.Utils;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Altinn.ResourceRegistry.Tests.Results;

public class  ConditionalResultTests
    : IClassFixture<ConditionalResultTests.Factory>
{
    private readonly Factory _factory;

    public ConditionalResultTests(Factory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PreconditionFailed()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/precondition-failed");

        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task NotModified()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/not-modified");

        response.StatusCode.Should().Be(HttpStatusCode.NotModified);
        Assert.NotNull(response.Headers.ETag);
        response.Headers.ETag.ToString().Should().Be(RequestCondition.SerializeETag("5"));

        Assert.NotNull(response.Content.Headers.LastModified);
        response.Content.Headers.LastModified.Should().Be(DateTimeOffset.UnixEpoch);
    }

    [Fact]
    public async Task NotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/not-found");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task NotFoundActionResult()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/not-found-action-result");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ok()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/ok");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Assert.NotNull(response.Headers.ETag);
        response.Headers.ETag.ToString().Should().Be(RequestCondition.SerializeETag("5"));

        Assert.NotNull(response.Content.Headers.LastModified);
        response.Content.Headers.LastModified.Should().Be(DateTimeOffset.UnixEpoch);

        var entity = await response.Content.ReadFromJsonAsync<TestEntity>();
        Assert.NotNull(entity);
        entity.Version.Should().Be("5");
        entity.ModifiedAt.Should().Be(DateTimeOffset.UnixEpoch);
    }

    [Fact]
    public async Task OkConditional()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/ok-conditional");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Assert.NotNull(response.Headers.ETag);
        response.Headers.ETag.ToString().Should().Be(RequestCondition.SerializeETag("5"));

        Assert.NotNull(response.Content.Headers.LastModified);
        response.Content.Headers.LastModified.Should().Be(DateTimeOffset.UnixEpoch);

        var entity = await response.Content.ReadFromJsonAsync<TestEntity>();
        Assert.NotNull(entity);
        entity.Version.Should().Be("5");
        entity.ModifiedAt.Should().Be(DateTimeOffset.UnixEpoch);
    }

    public class TestEntity
        : ITaggedEntity<string>
    {
        public required string Version { get; init; }

        public required DateTimeOffset ModifiedAt { get; init; }

        public void GetHeaderValues(out string version, out HttpDateTimeHeaderValue modifiedAt)
        {
            version = Version;
            modifiedAt = new(ModifiedAt);
        }
    }

    public class TestController
    {
        [HttpGet("/precondition-failed")]
        public ConditionalResult<TestEntity, string> PreconditionFailed()
        {
            return Conditional.ConditionFailed();
        }

        [HttpGet("/not-modified")]
        public ConditionalResult<TestEntity, string> NotModified()
        {
            return Conditional.Unmodified("5", DateTimeOffset.UnixEpoch);
        }

        [HttpGet("/not-found")]
        public ConditionalResult<TestEntity, string> NotFound()
        {
            return Conditional.NotFound();
        }

        [HttpGet("/not-found-action-result")]
        public ConditionalResult<TestEntity, string> NotFoundActionResult()
        {
            return new NotFoundResult();
        }

        [HttpGet("/ok")]
        public ConditionalResult<TestEntity, string> Ok()
        {
            return new TestEntity
            {
                Version = "5",
                ModifiedAt = DateTimeOffset.UnixEpoch,
            };
        }

        [HttpGet("/ok-conditional")]
        public ConditionalResult<TestEntity, string> OkConditional()
        {
            return Conditional.Succeeded(new TestEntity
            {
                Version = "5",
                ModifiedAt = DateTimeOffset.UnixEpoch,
            });
        }
    }

    public class  Factory : TestControllerApplicationFactory<TestController>
    {
    }
}
