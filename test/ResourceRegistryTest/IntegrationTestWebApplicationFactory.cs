using Altinn.Common.Authentication.Configuration;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Tests.Mocks;
using Altinn.ResourceRegistry.Tests.Utils;
using Altinn.ResourceRegistry.TestUtils;
using AltinnCore.Authentication.JwtCookie;
using Azure.Storage.Blobs.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;

namespace Altinn.ResourceRegistry.Tests;

public class IntegrationTestWebApplicationFactory
    : WebApplicationFactory<Program>
    , IAsyncLifetime
{
    DbFixture _dbServer = new();
    DbFixture.OwnedDb? _db;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        //builder.ConfigureAppConfiguration(config => config.AddJsonFile("appsettings.test.json"));
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddConfiguration(new ConfigurationBuilder()
                    .AddJsonFile("appsettings.test.json")
                    .Build());
        });
        builder.ConfigureServices(services =>
        {
            _db!.ConfigureServices(services);
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
            services.AddSingleton<IPostConfigureOptions<OidcProviderSettings>, OidcProviderPostConfigureSettingsStub>();
        });

        base.ConfigureWebHost(builder);
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await ((IAsyncLifetime)_dbServer).InitializeAsync();
        _db = await _dbServer.CreateDbAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await ((IAsyncLifetime)_dbServer).DisposeAsync();
    }
}

public class AccessListControllerTests 
    : IClassFixture<IntegrationTestWebApplicationFactory>
{
    private readonly IntegrationTestWebApplicationFactory _factory;

    public AccessListControllerTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Unauthenticated_Returns_Unauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/resourceregistry/api/v1/access-lists/974761076");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MissingScope_Returns_Forbidden()
    {
        using var client = _factory.CreateClient();

        var token = PrincipalUtil.GetOrgToken("skd", "974761076", "some.scope");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/resourceregistry/api/v1/access-lists/974761076");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task WrongOwner_Returns_Forbidden()
    {
        using var client = _factory.CreateClient();

        var token = PrincipalUtil.GetOrgToken("skd", "974761076", AuthzConstants.SCOPE_ACCESS_LIST_READ);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/resourceregistry/api/v1/access-lists/1234");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CorrectScopeAndOwner_Returns_NotImplemented()
    {
        using var client = _factory.CreateClient();

        var token = PrincipalUtil.GetOrgToken("skd", "974761076", AuthzConstants.SCOPE_ACCESS_LIST_READ);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/resourceregistry/api/v1/access-lists/974761076");
        response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
    }

    [Fact]
    public async Task CorrectScopeAndAdmin_Returns_NotImplemented()
    {
        using var client = _factory.CreateClient();

        var token = PrincipalUtil.GetOrgToken("skd", "974761076", $"{AuthzConstants.SCOPE_RESOURCE_ADMIN}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/resourceregistry/api/v1/access-lists/1234");
        response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
    }
}