﻿using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Register;
using Altinn.ResourceRegistry.Models;
using Altinn.ResourceRegistry.Tests.Utils;
using Altinn.ResourceRegistry.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace Altinn.ResourceRegistry.Tests;

public class AccessListControllerTests(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : WebApplicationTests(dbFixture, webApplicationFixture)
{
    private const string ORG_NR = "974761076";

    protected IAccessListsRepository Repository => Services.GetRequiredService<IAccessListsRepository>();
    protected AdvanceableTimeProvider TimeProvider => Services.GetRequiredService<AdvanceableTimeProvider>();
    protected NpgsqlDataSource DataSource => Services.GetRequiredService<NpgsqlDataSource>();

    private int _nextUserId = 1;


    private HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();

        var token = PrincipalUtil.GetOrgToken("skd", "974761076", AuthzConstants.SCOPE_ACCESS_LIST_WRITE);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    protected override void ConfigureServices(IServiceCollection services)
    {

        services.AddSingleton<IRegisterClient, MockRegisterClient>();

        base.ConfigureServices(services);
    }

    #region GetAccessListsByOwner
    public class GetAccessListsByOwner(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : AccessListControllerTests(dbFixture, webApplicationFixture)
    {
        [Fact]
        public async Task Returns_EmptyList()
        {
            using var client = CreateAuthenticatedClient();

            var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<Paginated<AccessListInfoDto>>();
            Assert.NotNull(content);

            content.Items.Should().BeEmpty();
            content.Links.Next.Should().BeNull();
        }

        [Fact]
        public async Task Returns_ItemsInDatabase_OrderedByIdentifier()
        {
            var def1 = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");
            var def2 = await Repository.CreateAccessList(ORG_NR, "test2", "Test 2", "test 2 description");

            using var client = CreateAuthenticatedClient();

            var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<Paginated<AccessListInfoDto>>();
            Assert.NotNull(content);

            content.Items.Should().HaveCount(2);
            content.Items.Should().Contain(al => al.Identifier == "test1")
                .Which.Name.Should().Be("Test 1");
            content.Items.Should().Contain(al => al.Identifier == "test2")
                .Which.Name.Should().Be("Test 2");
            content.Links.Next.Should().BeNull();

            var identifiers = content.Items.Select(al => al.Identifier).ToList();
            identifiers.Should().BeInAscendingOrder();
        }

        [Fact]
        public async Task Can_Include_Additional_Data()
        {
            const string ACTION_READ = "read";

            const string RESOURCE1_NAME = "test1";
            const string RESOURCE2_NAME = "test2";

            // Insert a fake resource (we have a foreign constraint on the party registry table)
            await using var resourceCmd = DataSource.CreateCommand(/*strpsql*/"INSERT INTO resourceregistry.resources (identifier, created, serviceresourcejson) VALUES (@name, NOW(), @json);");
            var nameParam = resourceCmd.Parameters.Add("name", NpgsqlTypes.NpgsqlDbType.Text);
            var jsonParam = resourceCmd.Parameters.Add("json", NpgsqlTypes.NpgsqlDbType.Jsonb);
            jsonParam.Value = "{}";

            nameParam.Value = RESOURCE1_NAME;
            await resourceCmd.ExecuteNonQueryAsync();
            nameParam.Value = RESOURCE2_NAME;
            await resourceCmd.ExecuteNonQueryAsync();

            var def1 = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");
            def1.AddResourceConnection(RESOURCE1_NAME, []);
            def1.AddResourceConnection(RESOURCE2_NAME, [ACTION_READ]);
            await def1.SaveChanges();

            using var client = CreateAuthenticatedClient();

            {
                using var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}");

                var content = await response.Content.ReadFromJsonAsync<Paginated<AccessListInfoDto>>();
                Assert.NotNull(content);

                content.Items.Should().HaveCount(1);
                content.Items.Should().Contain(al => al.Identifier == "test1")
                    .Which.ResourceConnections.Should().BeNull();
            }

            {
                using var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}?include=resources&resource=test1");

                var content = await response.Content.ReadFromJsonAsync<Paginated<AccessListInfoDto>>();
                Assert.NotNull(content);

                content.Items.Should().HaveCount(1);
                content.Items.Should().Contain(al => al.Identifier == "test1")
                    .Which.ResourceConnections.Should().HaveCount(1)
                    .And.AllSatisfy(rc => rc.Actions.Should().BeNull())
                    .And.Contain(rc => rc.ResourceIdentifier == RESOURCE1_NAME);
            }

            {
                using var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}?include=resources&resource=test2");

                var content = await response.Content.ReadFromJsonAsync<Paginated<AccessListInfoDto>>();
                Assert.NotNull(content);

                content.Items.Should().HaveCount(1);
                content.Items.Should().Contain(al => al.Identifier == "test1")
                    .Which.ResourceConnections.Should().HaveCount(1)
                    .And.AllSatisfy(rc => rc.Actions.Should().BeNull())
                    .And.Contain(rc => rc.ResourceIdentifier == RESOURCE2_NAME);
            }

            {
                using var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}?include=resource-actions&resource=test1");

                var content = await response.Content.ReadFromJsonAsync<Paginated<AccessListInfoDto>>();
                Assert.NotNull(content);

                content.Items.Should().HaveCount(1);
                content.Items.Should().Contain(al => al.Identifier == "test1")
                    .Which.ResourceConnections.Should().HaveCount(1)
                    .And.AllSatisfy(rc => rc.Actions.Should().NotBeNull())
                    .And.Contain(rc => rc.ResourceIdentifier == RESOURCE1_NAME)
                    .Which.Actions.Should().BeEmpty();
            }

            {
                using var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}?include=resource-actions&resource=test2");

                var content = await response.Content.ReadFromJsonAsync<Paginated<AccessListInfoDto>>();
                Assert.NotNull(content);

                content.Items.Should().HaveCount(1);
                content.Items.Should().Contain(al => al.Identifier == "test1")
                    .Which.ResourceConnections.Should().HaveCount(1)
                    .And.AllSatisfy(rc => rc.Actions.Should().NotBeNull())
                    .And.Contain(rc => rc.ResourceIdentifier == RESOURCE2_NAME)
                    .Which.Actions.Should().BeEquivalentTo([ACTION_READ]);
            }

            {
                using var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}?include=resources");
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            }

            {
                using var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}?include=resource-actions");
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            }
        }

        [Fact]
        public async Task Returns_Paginated()
        {
            // create enough access lists to fill two pages and then some
            for (var i = 41; i > 0; i--)
            {
                await Repository.CreateAccessList(ORG_NR, $"test{i:00}", $"Test {i:00}", $"test {i:00} description");
            }

            using var client = CreateAuthenticatedClient();

            // page 1
            var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<Paginated<AccessListInfoDto>>();
            Assert.NotNull(content);

            content.Items.Should().HaveCount(20);
            content.Links.Next.Should().NotBeNull();

            var identifiers = content.Items.Select(al => al.Identifier).ToList();
            identifiers.Should().BeInAscendingOrder()
                .And.StartWith("test01")
                .And.EndWith("test20");

            response = await client.GetAsync(content.Links.Next);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // page 2
            content = await response.Content.ReadFromJsonAsync<Paginated<AccessListInfoDto>>();
            Assert.NotNull(content);

            content.Items.Should().HaveCount(20);
            content.Links.Next.Should().NotBeNull();

            identifiers = content.Items.Select(al => al.Identifier).ToList();
            identifiers.Should().BeInAscendingOrder()
                .And.StartWith("test21")
                .And.EndWith("test40");

            // page 3
            response = await client.GetAsync(content.Links.Next);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            content = await response.Content.ReadFromJsonAsync<Paginated<AccessListInfoDto>>();
            Assert.NotNull(content);

            content.Items.Should().HaveCount(1);
            content.Links.Next.Should().BeNull();

            identifiers = content.Items.Select(al => al.Identifier).ToList();
            identifiers.Should().BeInAscendingOrder()
                .And.StartWith("test41");
        }
    }
    #endregion

    #region GetAccessList
    public class GetAccessList(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : AccessListControllerTests(dbFixture, webApplicationFixture)
    {
        [Fact]
        public async Task Returns_404_ForMissingList()
        {
            using var client = CreateAuthenticatedClient();

            var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/test");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Returns_Existing_List()
        {
            var def = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");

            using var client = CreateAuthenticatedClient();

            var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/{def.Identifier}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<AccessListInfoDto>();
            Assert.NotNull(content);

            content.Identifier.Should().Be(def.Identifier);
            content.Name.Should().Be(def.Name);
            content.Description.Should().Be(def.Description);

            response.Headers.ETag.Should().NotBeNull();
            response.Content.Headers.LastModified.Should().NotBeNull();
        }

        public class ETagHeaders(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
            : EtagHeadersTests(dbFixture, webApplicationFixture)
        {
            protected override async Task<AccessListInfo> Setup()
            {
                var aggregate = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");

                return aggregate.AsAccessListInfo();
            }

            protected override HttpRequestMessage CreateRequest(AccessListInfo info)
                => new(HttpMethod.Get, $"/resourceregistry/api/v1/access-lists/{info.ResourceOwner}/{info.Identifier}");

            protected override async Task ValidateResponse(HttpResponseMessage response, AccessListInfo info)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var content = await response.Content.ReadFromJsonAsync<AccessListInfoDto>();
                Assert.NotNull(content);

                content.Identifier.Should().Be(info.Identifier);
                content.Name.Should().Be(info.Name);
                content.Description.Should().Be(info.Description);
            }
        }
    }
    #endregion

    #region DeleteAccessList
    public class DeleteAccessList(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : AccessListControllerTests(dbFixture, webApplicationFixture)
    {
        [Fact]
        public async Task Returns_NoContent_ForMissingList()
        {
            using var client = CreateAuthenticatedClient();

            var response = await client.DeleteAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/test");
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Deletes_Existing_List()
        {
            var def = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");

            using var client = CreateAuthenticatedClient();

            var response = await client.DeleteAsync($"/resourceregistry/api/v1/access-lists/{def.ResourceOwner}/{def.Identifier}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var info = await Repository.LookupInfo(ORG_NR, def.Identifier);
            Assert.Null(info);
        }

        public class ETagHeaders(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
            : EtagHeadersTests(dbFixture, webApplicationFixture)
        {
            protected override bool ReturnsCacheHeaders => false;

            protected override async Task<AccessListInfo> Setup()
            {
                var aggregate = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");

                return aggregate.AsAccessListInfo();
            }

            protected override HttpRequestMessage CreateRequest(AccessListInfo info)
                => new(HttpMethod.Delete, $"/resourceregistry/api/v1/access-lists/{info.ResourceOwner}/{info.Identifier}");

            protected override async Task ValidateResponse(HttpResponseMessage response, AccessListInfo info)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var content = await response.Content.ReadFromJsonAsync<AccessListInfoDto>();
                Assert.NotNull(content);

                content.Identifier.Should().Be(info.Identifier);
                content.Name.Should().Be(info.Name);
                content.Description.Should().Be(info.Description);
            }
        }
    }
    #endregion

    #region UpsertAccessList
    public class UpsertAccessList(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : AccessListControllerTests(dbFixture, webApplicationFixture)
    {
        [Fact]
        public async Task Can_Create_List()
        {
            using var client = CreateAuthenticatedClient();

            var identifier = "test1";
            var dto = new CreateAccessListModel(Name: "Test 1", Description: "Test 1 description");
            var response = await client.PutAsJsonAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/{identifier}", dto);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<AccessListInfoDto>();
            Assert.NotNull(body);

            body.Identifier.Should().Be(identifier);
            body.Name.Should().Be(dto.Name);
            body.Description.Should().Be(dto.Description);

            response.Headers.ETag.Should().NotBeNull();
            response.Content.Headers.LastModified.Should().NotBeNull();

            var info = await Repository.LookupInfo(ORG_NR, identifier);
            Assert.NotNull(info);
            info!.Identifier.Should().Be(identifier);
            info.Name.Should().Be(dto.Name);
            info.Description.Should().Be(dto.Description);
        }

        [Fact]
        public async Task Can_Create_List_Without_Description()
        {
            using var client = CreateAuthenticatedClient();

            var identifier = "test1";
            var dto = new CreateAccessListModel(Name: "Test 1", Description: null);
            var response = await client.PutAsJsonAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/{identifier}", dto);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<AccessListInfoDto>();
            Assert.NotNull(body);

            body.Identifier.Should().Be(identifier);
            body.Name.Should().Be(dto.Name);
            body.Description.Should().BeEmpty();

            response.Headers.ETag.Should().NotBeNull();
            response.Content.Headers.LastModified.Should().NotBeNull();

            var info = await Repository.LookupInfo(ORG_NR, identifier);
            Assert.NotNull(info);
            info!.Identifier.Should().Be(identifier);
            info.Name.Should().Be(dto.Name);
            info.Description.Should().BeEmpty();
        }

        [Fact]
        public async Task CanNot_Create_List_Without_Name()
        {
            using var client = CreateAuthenticatedClient();

            var identifier = "test1";
            var dto = new CreateAccessListModel(Name: null!, Description: "Test 1 description");
            var response = await client.PutAsJsonAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/{identifier}", dto);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Can_Update_List()
        {
            var def = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");

            using var client = CreateAuthenticatedClient();

            var dto = new CreateAccessListModel(Name: "Test 1 updated", Description: "Test 1 description updated");
            var response = await client.PutAsJsonAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/{def.Identifier}", dto);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<AccessListInfoDto>();
            Assert.NotNull(body);

            body.Identifier.Should().Be(def.Identifier);
            body.Name.Should().Be(dto.Name);
            body.Description.Should().Be(dto.Description);

            response.Headers.ETag.Should().NotBeNull();
            response.Content.Headers.LastModified.Should().NotBeNull();

            var info = await Repository.LookupInfo(ORG_NR, def.Identifier);
            Assert.NotNull(info);
            info!.Identifier.Should().Be(def.Identifier);
            info.Name.Should().Be(dto.Name);
            info.Description.Should().Be(dto.Description);
        }

        [Fact]
        public async Task Can_Unset_Description()
        {
            var def = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");

            using var client = CreateAuthenticatedClient();

            var dto = new CreateAccessListModel(Name: "Test 1 updated", Description: null);
            var response = await client.PutAsJsonAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/{def.Identifier}", dto);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<AccessListInfoDto>();
            Assert.NotNull(body);

            body.Identifier.Should().Be(def.Identifier);
            body.Name.Should().Be(dto.Name);
            body.Description.Should().BeEmpty();

            response.Headers.ETag.Should().NotBeNull();
            response.Content.Headers.LastModified.Should().NotBeNull();

            var info = await Repository.LookupInfo(ORG_NR, def.Identifier);
            Assert.NotNull(info);
            info!.Identifier.Should().Be(def.Identifier);
            info.Name.Should().Be(dto.Name);
            info.Description.Should().BeEmpty();
        }

        [Fact]
        public async Task Cannot_Unset_Name()
        {
            var def = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");

            using var client = CreateAuthenticatedClient();

            var dto = new CreateAccessListModel(Name: null!, Description: "Test 1 description updated");
            var response = await client.PutAsJsonAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/{def.Identifier}", dto);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Handles_Update_To_Same()
        {
            var def = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");

            using var client = CreateAuthenticatedClient();

            var dto = new CreateAccessListModel(Name: def.Name, Description: def.Description);
            var response = await client.PutAsJsonAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/{def.Identifier}", dto);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<AccessListInfoDto>();
            Assert.NotNull(body);

            body.Identifier.Should().Be(def.Identifier);
            body.Name.Should().Be(def.Name);
            body.Description.Should().Be(def.Description);

            response.Headers.ETag.Should().NotBeNull();
            response.Content.Headers.LastModified.Should().NotBeNull();

            var info = await Repository.LookupInfo(ORG_NR, def.Identifier);
            Assert.NotNull(info);
            info!.Identifier.Should().Be(def.Identifier);
            info.Name.Should().Be(def.Name);
            info.Description.Should().Be(def.Description);
            info.Version.Should().Be(def.CommittedVersion.UnsafeValue);
        }

        [Fact]
        public async Task Can_Require_Existing_List()
        {
            var def = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");

            using var client = CreateAuthenticatedClient();

            var dto = new CreateAccessListModel(Name: "Test 1 external", Description: "Test 1 description external");
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/resourceregistry/api/v1/access-lists/{ORG_NR}/{def.Identifier}")
            {
                Content = JsonContent.Create(dto)
            };
            request.Headers.IfMatch.ParseAdd("*");

            var response = await client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Can_Require_New_List()
        {
            var def = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");

            using var client = CreateAuthenticatedClient();

            var dto = new CreateAccessListModel(Name: "Test 1 external", Description: "Test 1 description external");
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/resourceregistry/api/v1/access-lists/{ORG_NR}/{def.Identifier}")
            {
                Content = JsonContent.Create(dto)
            };
            request.Headers.IfNoneMatch.ParseAdd("*");

            var response = await client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        }

        public class ETagHeaders(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
            : EtagHeadersTests(dbFixture, webApplicationFixture)
        {
            protected override async Task<AccessListInfo> Setup()
            {
                var aggregate = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");

                return aggregate.AsAccessListInfo();
            }

            protected override HttpRequestMessage CreateRequest(AccessListInfo info)
            {
                var dto = new CreateAccessListModel(Name: $"new {info.Name}", Description: $"new {info.Description}");
                return new(HttpMethod.Put, $"/resourceregistry/api/v1/access-lists/{info.ResourceOwner}/{info.Identifier}")
                {
                    Content = JsonContent.Create(dto)
                };
            }

            protected override async Task ValidateResponse(HttpResponseMessage response, AccessListInfo info)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var content = await response.Content.ReadFromJsonAsync<AccessListInfoDto>();
                Assert.NotNull(content);

                content.Identifier.Should().Be(info.Identifier);
                content.Name.Should().Be($"new {info.Name}");
                content.Description.Should().Be($"new {info.Description}");
            }
        }
    }
    #endregion

    #region GetAccessListResourceConnections
    public class GetAccessListResourceConnections(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : AccessListControllerTests(dbFixture, webApplicationFixture)
    {
        [Fact]
        public async Task Returns_NextPage_Link_When_Enough_Items()
        {
            var resources = Enumerable.Range(0, 222).Select(i => new
            {
                Identifier = $"resource-{i:D4}",
                Index = i,
                Actions = (i % 4) switch
                {
                    0 => ImmutableArray<string>.Empty,
                    1 => ["read"],
                    2 => ["write"],
                    3 => ["read", "write"],
                    _ => throw new UnreachableException()
                },
            }).ToDictionary(r => r.Identifier);

            foreach (var resource in resources.Values)
            {
                await AddResource(resource.Identifier);
            }

            var aggregate = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");
            foreach (var resource in resources.Values)
            {
                aggregate.AddResourceConnection(resource.Identifier, resource.Actions);
            }
            await aggregate.SaveChanges();

            using var client = CreateAuthenticatedClient();
            using var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/test1/resource-connections");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<Paginated<AccessListResourceConnectionDto>>();
            Assert.NotNull(content);

            content.Items.Should().HaveCount(100);
            content.Links.Next.Should().NotBeNull();

            foreach (var item in content.Items)
            {
                var resource = resources[item.ResourceIdentifier];
                item.Actions.Should().BeEquivalentTo(resource.Actions);
            }

            using var nextPageResponse = await client.GetAsync(content.Links.Next);
            nextPageResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var nextPageContent = await nextPageResponse.Content.ReadFromJsonAsync<Paginated<AccessListResourceConnectionDto>>();
            Assert.NotNull(nextPageContent);

            nextPageContent.Items.Should().HaveCount(100);
            nextPageContent.Links.Next.Should().NotBeNull();

            foreach (var item in nextPageContent.Items)
            {
                var resource = resources[item.ResourceIdentifier];
                item.Actions.Should().BeEquivalentTo(resource.Actions);
            }

            using var lastPageResponse = await client.GetAsync(nextPageContent.Links.Next);
            lastPageResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var lastPageContent = await lastPageResponse.Content.ReadFromJsonAsync<Paginated<AccessListResourceConnectionDto>>();
            Assert.NotNull(lastPageContent);

            lastPageContent.Items.Should().HaveCount(22);
            lastPageContent.Links.Next.Should().BeNull();

            foreach (var item in lastPageContent.Items)
            {
                var resource = resources[item.ResourceIdentifier];
                item.Actions.Should().BeEquivalentTo(resource.Actions);
            }
        }

        [Fact]
        public async Task Returns_PreconditionFailed_If_AccessList_Is_Modified_While_Iterating()
        {
            var resources = Enumerable.Range(0, 222).Select(i => new
            {
                Identifier = $"resource-{i:D4}",
                Index = i,
                Actions = (i % 4) switch
                {
                    0 => ImmutableArray<string>.Empty,
                    1 => ["read"],
                    2 => ["write"],
                    3 => ["read", "write"],
                    _ => throw new UnreachableException()
                },
            }).ToDictionary(r => r.Identifier);

            foreach (var resource in resources.Values)
            {
                await AddResource(resource.Identifier);
            }

            var aggregate = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");
            foreach (var resource in resources.Values)
            {
                aggregate.AddResourceConnection(resource.Identifier, resource.Actions);
            }
            await aggregate.SaveChanges();

            using var client = CreateAuthenticatedClient();
            using var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/test1/resource-connections");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<Paginated<AccessListResourceConnectionDto>>();
            Assert.NotNull(content);

            content.Items.Should().HaveCount(100);
            content.Links.Next.Should().NotBeNull();

            // Update access list
            aggregate.Update(name: "Test 1 updated");
            await aggregate.SaveChanges();

            using var nextPageResponse = await client.GetAsync(content.Links.Next);
            nextPageResponse.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        }

        public class ETagHeaders(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
            : EtagHeadersTests(dbFixture, webApplicationFixture)
        {
            protected override async Task<AccessListInfo> Setup()
            {
                await AddResource("empty");
                await AddResource("read");
                await AddResource("write");
                await AddResource("readwrite");

                var aggregate = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");
                aggregate.AddResourceConnection("empty", []);
                aggregate.AddResourceConnection("read", ["read"]);
                aggregate.AddResourceConnection("write", ["write"]);
                aggregate.AddResourceConnection("readwrite", ["read", "write"]);
                await aggregate.SaveChanges();

                return aggregate.AsAccessListInfo();
            }

            protected override HttpRequestMessage CreateRequest(AccessListInfo info)
            {
                return new(HttpMethod.Get, $"/resourceregistry/api/v1/access-lists/{info.ResourceOwner}/{info.Identifier}/resource-connections");
            }

            protected override async Task ValidateResponse(HttpResponseMessage response, AccessListInfo info)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var content = await response.Content.ReadFromJsonAsync<Paginated<AccessListResourceConnectionDto>>();
                Assert.NotNull(content);

                content.Links.Next.Should().BeNull();
                content.Items.Should().HaveCount(4);

                content.Items.Should().Contain(rc => rc.ResourceIdentifier == "empty")
                    .Which.Actions.Should().BeEmpty();

                content.Items.Should().Contain(rc => rc.ResourceIdentifier == "read")
                    .Which.Actions.Should().BeEquivalentTo(["read"]);

                content.Items.Should().Contain(rc => rc.ResourceIdentifier == "write")
                    .Which.Actions.Should().BeEquivalentTo(["write"]);

                content.Items.Should().Contain(rc => rc.ResourceIdentifier == "readwrite")
                    .Which.Actions.Should().BeEquivalentTo(["read", "write"]);
            }
        }
    }
    #endregion

    #region UpsertAccessListResourceConnection
    public class UpsertAccessListResourceConnection(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : AccessListControllerTests(dbFixture, webApplicationFixture)
    {
        [Fact]
        public async Task Can_Create_Connection()
        {
            await AddResource("test1");

            var def = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");

            using var client = CreateAuthenticatedClient();

            var dto = new UpsertAccessListResourceConnectionDto(Actions: ["read", "write"]);
            var response = await client.PutAsJsonAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/{def.Identifier}/resource-connections/test1", dto);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<AccessListResourceConnectionDto>();
            Assert.NotNull(body);

            body.ResourceIdentifier.Should().Be("test1");
            body.Actions.Should().BeEquivalentTo(["read", "write"]);

            response.Headers.ETag.Should().NotBeNull();
            response.Content.Headers.LastModified.Should().NotBeNull();

            var info = await Repository.LoadAccessList(ORG_NR, def.Identifier);
            Assert.NotNull(info);
            info.TryGetResourceConnections("test1", out var conn).Should().BeTrue();
            conn!.Actions.Should().BeEquivalentTo(["read", "write"]);
        }

        [Fact]
        public async Task Can_Update_Connection()
        {
            await AddResource("test1");

            var def = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");
            def.AddResourceConnection("test1", ["read"]);
            await def.SaveChanges();

            using var client = CreateAuthenticatedClient();

            var dto = new UpsertAccessListResourceConnectionDto(Actions: ["write"]);
            var response = await client.PutAsJsonAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/{def.Identifier}/resource-connections/test1", dto);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<AccessListResourceConnectionDto>();
            Assert.NotNull(body);

            body.ResourceIdentifier.Should().Be("test1");
            body.Actions.Should().BeEquivalentTo(["write"]);

            response.Headers.ETag.Should().NotBeNull();
            response.Content.Headers.LastModified.Should().NotBeNull();

            var info = await Repository.LoadAccessList(ORG_NR, def.Identifier);
            Assert.NotNull(info);
            info.TryGetResourceConnections("test1", out var conn).Should().BeTrue();
            conn!.Actions.Should().BeEquivalentTo(["write"]);
        }

        [Fact]
        public async Task Does_Not_Create_New_Versio_If_Identical()
        {
            await AddResource("test1");

            var def = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");
            def.AddResourceConnection("test1", ["read", "write"]);
            await def.SaveChanges();
            var version = def.CommittedVersion;

            using var client = CreateAuthenticatedClient();

            var dto = new UpsertAccessListResourceConnectionDto(Actions: ["read", "write"]);
            var response = await client.PutAsJsonAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/{def.Identifier}/resource-connections/test1", dto);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<AccessListResourceConnectionDto>();
            Assert.NotNull(body);

            body.ResourceIdentifier.Should().Be("test1");
            body.Actions.Should().BeEquivalentTo(["read", "write"]);

            response.Headers.ETag.Should().NotBeNull();
            response.Content.Headers.LastModified.Should().NotBeNull();

            var updated = await Repository.LoadAccessList(ORG_NR, def.Identifier);
            Assert.NotNull(updated);
            updated.TryGetResourceConnections("test1", out var conn).Should().BeTrue();
            conn!.Actions.Should().BeEquivalentTo(["read", "write"]);
            updated.CommittedVersion.Should().Be(version);
        }

        public class ETagHeaders(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
            : EtagHeadersTests(dbFixture, webApplicationFixture)
        {
               protected override async Task<AccessListInfo> Setup()
            {
                await AddResource("test1");

                var aggregate = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");
                aggregate.AddResourceConnection("test1", ["read"]);
                await aggregate.SaveChanges();

                return aggregate.AsAccessListInfo();
            }

            protected override HttpRequestMessage CreateRequest(AccessListInfo info)
            {
                var dto = new UpsertAccessListResourceConnectionDto(Actions: ["write"]);
                return new(HttpMethod.Put, $"/resourceregistry/api/v1/access-lists/{info.ResourceOwner}/{info.Identifier}/resource-connections/test1")
                {
                    Content = JsonContent.Create(dto)
                };
            }

            protected override async Task ValidateResponse(HttpResponseMessage response, AccessListInfo info)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var content = await response.Content.ReadFromJsonAsync<AccessListResourceConnectionDto>();
                Assert.NotNull(content);

                content.ResourceIdentifier.Should().Be("test1");
                content.Actions.Should().BeEquivalentTo(["write"]);
            }
        }
    }
    #endregion

    #region DeleteAccessListResourceConnection
    public class DeleteAccessListResourceConnection(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : AccessListControllerTests(dbFixture, webApplicationFixture)
    {
        [Fact]
        public async Task Returns_NotFound_ForMissingAccessList()
        {
            using var client = CreateAuthenticatedClient();

            var response = await client.DeleteAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/test1/resource-connections/test1");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Returns_NoContent_ForMissingConnection()
        {
            var def = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");

            using var client = CreateAuthenticatedClient();

            var response = await client.DeleteAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/{def.Identifier}/resource-connections/test1");
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Deletes_Existing_Connection()
        {
            await AddResource("test1");

            var def = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");
            def.AddResourceConnection("test1", ["read", "write"]);
            await def.SaveChanges();

            using var client = CreateAuthenticatedClient();

            var response = await client.DeleteAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/{def.Identifier}/resource-connections/test1");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var data = await response.Content.ReadFromJsonAsync<AccessListResourceConnectionDto>();
            Assert.NotNull(data);
            data.ResourceIdentifier.Should().Be("test1");
            data.Actions.Should().BeEquivalentTo(["read", "write"]);

            var aggregate = await Repository.LoadAccessList(ORG_NR, def.Identifier);
            Assert.NotNull(aggregate);
            aggregate.TryGetResourceConnections("test1", out _).Should().BeFalse();
        }

        public class ETagHeaders(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
            : EtagHeadersTests(dbFixture, webApplicationFixture)
        {
            protected override async Task<AccessListInfo> Setup()
            {
                await AddResource("test1");

                var aggregate = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");
                aggregate.AddResourceConnection("test1", ["read", "write"]);
                await aggregate.SaveChanges();

                return aggregate.AsAccessListInfo();
            }

            protected override HttpRequestMessage CreateRequest(AccessListInfo info)
                => new(HttpMethod.Delete, $"/resourceregistry/api/v1/access-lists/{info.ResourceOwner}/{info.Identifier}/resource-connections/test1");

            protected override async Task ValidateResponse(HttpResponseMessage response, AccessListInfo info)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var data = await response.Content.ReadFromJsonAsync<AccessListResourceConnectionDto>();
                Assert.NotNull(data);
                data.ResourceIdentifier.Should().Be("test1");
                data.Actions.Should().BeEquivalentTo(["read", "write"]);

                var aggregate = await Repository.LoadAccessList(info.Id);
                Assert.NotNull(aggregate);
                aggregate.TryGetResourceConnections("test1", out _).Should().BeFalse();
            }
        }
    }
    #endregion

    #region GetAccessListMembers
    public class GetAccessListMembers(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : AccessListControllerTests(dbFixture, webApplicationFixture)
    {
        [Fact]
        public async Task Returns_NotFound_ForMissingAccessList()
        {
            using var client = CreateAuthenticatedClient();

            var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/test1/members");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Returns_MembersInList()
        {
            var user1 = GenerateUserId();
            var user2 = GenerateUserId();

            var def = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");
            def.AddMembers([user1, user2]);
            await def.SaveChanges();

            using var client = CreateAuthenticatedClient();

            var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/test1/members");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<Paginated<AccessListMembershipDto>>();
            Assert.NotNull(content);

            content.Items.Should().HaveCount(2);
            content.Links.Next.Should().BeNull();

            content.Items.Should().Contain(m => m.Id.Value == user1);
            content.Items.Should().Contain(m => m.Id.Value == user2);
        }

        [Fact]
        public async Task Paginates()
        {
            var users = new List<Guid>(250);
            var received = new List<Guid>(250);
            for (var i = 0; i < 250; i++)
            {
                var user = GenerateUserId();
                users.Add(user);
            }

            var def = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");
            def.AddMembers(users);
            await def.SaveChanges();

            using var client = CreateAuthenticatedClient();

            var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/test1/members");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<Paginated<AccessListMembershipDto>>();
            Assert.NotNull(content);

            content.Items.Should().HaveCount(100);
            content.Links.Next.Should().NotBeNull();
            received.AddRange(content.Items.Select(m => m.Id.Value));

            response = await client.GetAsync(content.Links.Next);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            content = await response.Content.ReadFromJsonAsync<Paginated<AccessListMembershipDto>>();
            Assert.NotNull(content);

            content.Items.Should().HaveCount(100);
            content.Links.Next.Should().NotBeNull();
            received.AddRange(content.Items.Select(m => m.Id.Value));

            response = await client.GetAsync(content.Links.Next);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            content = await response.Content.ReadFromJsonAsync<Paginated<AccessListMembershipDto>>();
            Assert.NotNull(content);

            content.Items.Should().HaveCount(50);
            content.Links.Next.Should().BeNull();
            received.AddRange(content.Items.Select(m => m.Id.Value));

            foreach (var user in users)
            {
                received.Should().Contain(user);
            }
        }

        public class ETagHeaders
            : EtagHeadersTests
        {
            private Guid _user1;
            private Guid _user2;

            public ETagHeaders(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
                : base(dbFixture, webApplicationFixture)
            {
                _user1 = GenerateUserId();
                _user2 = GenerateUserId();
            }

            protected override async Task<AccessListInfo> Setup()
            {
                var aggregate = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");
                aggregate.AddMembers([_user1, _user2]);
                await aggregate.SaveChanges();

                return aggregate.AsAccessListInfo();
            }

            protected override HttpRequestMessage CreateRequest(AccessListInfo info)
                => new(HttpMethod.Get, $"/resourceregistry/api/v1/access-lists/{ORG_NR}/test1/members");

            protected override async Task ValidateResponse(HttpResponseMessage response, AccessListInfo info)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var content = await response.Content.ReadFromJsonAsync<Paginated<AccessListMembershipDto>>();
                Assert.NotNull(content);

                content.Items.Should().HaveCount(2);
                content.Links.Next.Should().BeNull();

                content.Items.Should().Contain(m => m.Id.Value == _user1);
                content.Items.Should().Contain(m => m.Id.Value == _user2);
            }
        }
    }
    #endregion

    #region ReplaceAccessListMembers
    public class ReplaceAccessListMembers(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : AccessListControllerTests(dbFixture, webApplicationFixture)
    {
        [Fact]
        public async Task Returns_NotFound_ForMissingAccessList()
        {
            using var client = CreateAuthenticatedClient();

            using var body = JsonContent.Create(new UpsertAccessListPartyMembersListDto([
                PartyReference.PartyUuid.Create(GenerateUserId()),
                PartyReference.PartyUuid.Create(GenerateUserId()),
            ]));
            var response = await client.PutAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/test1/members", body);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Replaces_MembersInList()
        {
            var user1 = GenerateUserId();
            var user2 = GenerateUserId();
            var user3 = GenerateUserId();
            var user4 = GenerateUserId();

            var def = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");
            def.AddMembers([user1, user2]);
            await def.SaveChanges();

            using var client = CreateAuthenticatedClient();

            using var body = JsonContent.Create(new UpsertAccessListPartyMembersListDto([
                PartyReference.PartyUuid.Create(user3),
                PartyReference.PartyUuid.Create(user4),
            ]));
            var response = await client.PutAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/test1/members", body);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<Paginated<AccessListMembershipDto>>();
            Assert.NotNull(content);

            content.Items.Should().HaveCount(2);
            content.Links.Next.Should().BeNull();

            content.Items.Should().Contain(m => m.Id.Value == user3);
            content.Items.Should().Contain(m => m.Id.Value == user4);
        }

        [Fact]
        public async Task IsIdempotent()
        {
            var user1 = GenerateUserId();
            var user2 = GenerateUserId();
            var user3 = GenerateUserId();
            var user4 = GenerateUserId();

            var def = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");
            def.AddMembers([user1, user2]);
            await def.SaveChanges();

            using var client = CreateAuthenticatedClient();

            using var body = JsonContent.Create(new UpsertAccessListPartyMembersListDto([
                PartyReference.PartyUuid.Create(user3),
                PartyReference.PartyUuid.Create(user4),
            ]));
            var response = await client.PutAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/test1/members", body);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<Paginated<AccessListMembershipDto>>();
            var etag = response.Headers.ETag;
            Assert.NotNull(content);
            Assert.NotNull(etag);

            content.Items.Should().HaveCount(2);
            content.Links.Next.Should().BeNull();

            content.Items.Should().Contain(m => m.Id.Value == user3);
            content.Items.Should().Contain(m => m.Id.Value == user4);

            response = await client.PutAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/test1/members", body);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            content = await response.Content.ReadFromJsonAsync<Paginated<AccessListMembershipDto>>();
            Assert.NotNull(content);

            response.Headers.ETag.Should().Be(etag);
            content.Items.Should().HaveCount(2);
            content.Links.Next.Should().BeNull();

            content.Items.Should().Contain(m => m.Id.Value == user3);
            content.Items.Should().Contain(m => m.Id.Value == user4);
        }

        public class ETagHeaders
            : EtagHeadersTests
        {
            private Guid _user1;
            private Guid _user2;
            private Guid _user3;
            private Guid _user4;

            public ETagHeaders(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
                : base(dbFixture, webApplicationFixture)
            {
                _user1 = GenerateUserId();
                _user2 = GenerateUserId();
                _user3 = GenerateUserId();
                _user4 = GenerateUserId();
            }

            protected override async Task<AccessListInfo> Setup()
            {
                var aggregate = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");
                aggregate.AddMembers([_user1, _user2]);
                await aggregate.SaveChanges();

                return aggregate.AsAccessListInfo();
            }

            protected override HttpRequestMessage CreateRequest(AccessListInfo info)
            {
                var body = JsonContent.Create(new UpsertAccessListPartyMembersListDto([
                    PartyReference.PartyUuid.Create(_user3),
                    PartyReference.PartyUuid.Create(_user4),
                ]));

                return new(HttpMethod.Put, $"/resourceregistry/api/v1/access-lists/{ORG_NR}/test1/members")
                {
                    Content = body,
                };
            }

            protected override async Task ValidateResponse(HttpResponseMessage response, AccessListInfo info)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var content = await response.Content.ReadFromJsonAsync<Paginated<AccessListMembershipDto>>();
                Assert.NotNull(content);

                content.Items.Should().HaveCount(2);
                content.Links.Next.Should().BeNull();

                content.Items.Should().Contain(m => m.Id.Value == _user3);
                content.Items.Should().Contain(m => m.Id.Value == _user4);
            }
        }
    }
    #endregion

    #region AddAccessListMembers
    public class AddAccessListMembers(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : AccessListControllerTests(dbFixture, webApplicationFixture)
    {
        [Fact]
        public async Task Returns_NotFound_ForMissingAccessList()
        {
            using var client = CreateAuthenticatedClient();

            using var body = JsonContent.Create(new UpsertAccessListPartyMembersListDto([
                PartyReference.PartyUuid.Create(GenerateUserId()),
                PartyReference.PartyUuid.Create(GenerateUserId()),
            ]));
            var response = await client.PostAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/test1/members", body);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Adds_MembersInList()
        {
            var user1 = GenerateUserId();
            var user2 = GenerateUserId();
            var user3 = GenerateUserId();
            var user4 = GenerateUserId();

            var def = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");
            def.AddMembers([user1, user2]);
            await def.SaveChanges();

            using var client = CreateAuthenticatedClient();

            using var body = JsonContent.Create(new UpsertAccessListPartyMembersListDto([
                PartyReference.PartyUuid.Create(user3),
                PartyReference.PartyUuid.Create(user4),
            ]));
            var response = await client.PostAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/test1/members", body);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<Paginated<AccessListMembershipDto>>();
            Assert.NotNull(content);

            content.Items.Should().HaveCount(4);
            content.Links.Next.Should().BeNull();

            content.Items.Should().Contain(m => m.Id.Value == user1);
            content.Items.Should().Contain(m => m.Id.Value == user2);
            content.Items.Should().Contain(m => m.Id.Value == user3);
            content.Items.Should().Contain(m => m.Id.Value == user4);
        }

        [Fact]
        public async Task IsIdempotent()
        {
            var user1 = GenerateUserId();
            var user2 = GenerateUserId();
            var user3 = GenerateUserId();
            var user4 = GenerateUserId();

            var def = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");
            def.AddMembers([user1, user2]);
            await def.SaveChanges();

            using var client = CreateAuthenticatedClient();

            using var body = JsonContent.Create(new UpsertAccessListPartyMembersListDto([
                PartyReference.PartyUuid.Create(user3),
                PartyReference.PartyUuid.Create(user4),
            ]));
            var response = await client.PostAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/test1/members", body);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<Paginated<AccessListMembershipDto>>();
            var etag = response.Headers.ETag;
            Assert.NotNull(content);
            Assert.NotNull(etag);

            content.Items.Should().HaveCount(4);
            content.Links.Next.Should().BeNull();

            content.Items.Should().Contain(m => m.Id.Value == user1);
            content.Items.Should().Contain(m => m.Id.Value == user2);
            content.Items.Should().Contain(m => m.Id.Value == user3);
            content.Items.Should().Contain(m => m.Id.Value == user4);

            response = await client.PostAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}/test1/members", body);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            content = await response.Content.ReadFromJsonAsync<Paginated<AccessListMembershipDto>>();
            Assert.NotNull(content);

            response.Headers.ETag.Should().Be(etag);
            content.Items.Should().HaveCount(4);
            content.Links.Next.Should().BeNull();

            content.Items.Should().Contain(m => m.Id.Value == user1);
            content.Items.Should().Contain(m => m.Id.Value == user2);
            content.Items.Should().Contain(m => m.Id.Value == user3);
            content.Items.Should().Contain(m => m.Id.Value == user4);
        }

        public class ETagHeaders
            : EtagHeadersTests
        {
            private Guid _user1;
            private Guid _user2;
            private Guid _user3;
            private Guid _user4;

            public ETagHeaders(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
                : base(dbFixture, webApplicationFixture)
            {
                _user1 = GenerateUserId();
                _user2 = GenerateUserId();
                _user3 = GenerateUserId();
                _user4 = GenerateUserId();
            }

            protected override async Task<AccessListInfo> Setup()
            {
                var aggregate = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");
                aggregate.AddMembers([_user1, _user2]);
                await aggregate.SaveChanges();

                return aggregate.AsAccessListInfo();
            }

            protected override HttpRequestMessage CreateRequest(AccessListInfo info)
            {
                var body = JsonContent.Create(new UpsertAccessListPartyMembersListDto([
                    PartyReference.PartyUuid.Create(_user3),
                    PartyReference.PartyUuid.Create(_user4),
                ]));

                return new(HttpMethod.Post, $"/resourceregistry/api/v1/access-lists/{ORG_NR}/test1/members")
                {
                    Content = body,
                };
            }

            protected override async Task ValidateResponse(HttpResponseMessage response, AccessListInfo info)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var content = await response.Content.ReadFromJsonAsync<Paginated<AccessListMembershipDto>>();
                Assert.NotNull(content);

                content.Items.Should().HaveCount(4);
                content.Links.Next.Should().BeNull();

                content.Items.Should().Contain(m => m.Id.Value == _user1);
                content.Items.Should().Contain(m => m.Id.Value == _user2);
                content.Items.Should().Contain(m => m.Id.Value == _user3);
                content.Items.Should().Contain(m => m.Id.Value == _user4);
            }
        }
    }
    #endregion

    #region Authorization
    public class Authorization(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : AccessListControllerTests(dbFixture, webApplicationFixture)
    {
        [Fact]
        public async Task Unauthenticated_Returns_Unauthorized()
        {
            using var client = CreateClient();

            var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task MissingScope_Returns_Forbidden()
        {
            using var client = CreateClient();

            var token = PrincipalUtil.GetOrgToken("skd", "974761076", "some.scope");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}");
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task WrongOwner_Returns_Forbidden()
        {
            using var client = CreateClient();

            var token = PrincipalUtil.GetOrgToken("skd", "974761076", AuthzConstants.SCOPE_ACCESS_LIST_READ);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/resourceregistry/api/v1/access-lists/1234");
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CorrectScopeAndOwner_Returns_NotImplemented()
        {
            using var client = CreateClient();

            var token = PrincipalUtil.GetOrgToken("skd", "974761076", AuthzConstants.SCOPE_ACCESS_LIST_READ);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CorrectScopeAndAdmin_Returns_NotImplemented()
        {
            using var client = CreateClient();

            var token = PrincipalUtil.GetOrgToken("skd", "974761076", $"{AuthzConstants.SCOPE_RESOURCE_ADMIN}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/resourceregistry/api/v1/access-lists/1234");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
    #endregion

    #region ETag Headers Tests Helpers
    public abstract class EtagHeadersTests(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : AccessListControllerTests(dbFixture, webApplicationFixture)
    {
        protected virtual bool ReturnsCacheHeaders => true;

        protected abstract Task<AccessListInfo> Setup();
        protected abstract HttpRequestMessage CreateRequest(AccessListInfo info);
        protected abstract Task ValidateResponse(HttpResponseMessage response, AccessListInfo info);

        private async Task<(EntityTagHeaderValue ETag, DateTimeOffset LastModified)> GetAccessListCacheHeaders(HttpClient client, AccessListInfo info)
        {
            var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{info.ResourceOwner}/{info.Identifier}");
            var etag = response.Headers.ETag;
            var lastModified = response.Content.Headers.LastModified;

            Assert.NotNull(etag);
            Assert.NotNull(lastModified);

            return (etag, lastModified.Value);
        }

        private async Task<EntityTagHeaderValue> GetETag(HttpClient client, AccessListInfo info)
            => (await GetAccessListCacheHeaders(client, info)).ETag;

        private async Task<DateTimeOffset> GetLastModified(HttpClient client, AccessListInfo info)
            => (await GetAccessListCacheHeaders(client, info)).LastModified;

        private async Task<AccessListInfo> Modify(AccessListInfo info)
        {
            var aggregate = await Repository.LoadAccessList(info.ResourceOwner, info.Identifier);
            Assert.NotNull(aggregate);

            aggregate.Update(name: $"{info.Name} updated", description: $"{info.Description} updated");
            await aggregate.SaveChanges();

            return aggregate.AsAccessListInfo();
        }

        [Fact]
        public async Task Works_Without_Cache_Headers()
        {
            var info = await Setup();

            using var client = CreateAuthenticatedClient();
            var request = CreateRequest(info);

            var response = await client.SendAsync(request);
            if (ReturnsCacheHeaders)
            {
                response.Headers.ETag.Should().NotBeNull();
                response.Content.Headers.LastModified.Should().NotBeNull();
            }

            await ValidateResponse(response, info);
        }

        [Fact]
        public async Task Works_With_Matching_ETag()
        {
            var info = await Setup();

            using var client = CreateAuthenticatedClient();
            var etag = await GetETag(client, info);

            var request = CreateRequest(info);
            request.Headers.IfMatch.Add(etag);

            var response = await client.SendAsync(request);
            if (ReturnsCacheHeaders)
            {
                response.Headers.ETag.Should().NotBeNull();
                response.Content.Headers.LastModified.Should().NotBeNull();
            }

            await ValidateResponse(response, info);
        }

        [Fact]
        public async Task Works_With_UnmodifiedSince()
        {
            var info = await Setup();

            using var client = CreateAuthenticatedClient();
            var lastModified = await GetLastModified(client, info);

            var request = CreateRequest(info);
            request.Headers.IfUnmodifiedSince = lastModified;

            var response = await client.SendAsync(request);
            if (ReturnsCacheHeaders)
            {
                response.Headers.ETag.Should().NotBeNull();
                response.Content.Headers.LastModified.Should().NotBeNull();
            }

            await ValidateResponse(response, info);
        }

        [Fact]
        public async Task Works_With_ETag_And_UnmodifiedSince()
        {
            var info = await Setup();

            using var client = CreateAuthenticatedClient();
            var (etag, lastModified) = await GetAccessListCacheHeaders(client, info);

            var request = CreateRequest(info);
            request.Headers.IfMatch.Add(etag);
            request.Headers.IfUnmodifiedSince = lastModified;

            var response = await client.SendAsync(request);
            if (ReturnsCacheHeaders)
            {
                response.Headers.ETag.Should().NotBeNull();
                response.Content.Headers.LastModified.Should().NotBeNull();
            }

            await ValidateResponse(response, info);
        }

        [Fact]
        public async Task Returns_Mismatch_When_ETag_Is_Up_To_Date()
        {
            var info = await Setup();

            using var client = CreateAuthenticatedClient();
            var etag = await GetETag(client, info);

            var request = CreateRequest(info);
            request.Headers.IfNoneMatch.Add(etag);
            var isReadRequest = request.Method == HttpMethod.Get || request.Method == HttpMethod.Head;

            var response = await client.SendAsync(request);

            if (isReadRequest)
            {
                response.StatusCode.Should().Be(HttpStatusCode.NotModified);
            }
            else
            {
                response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);

                // update should not have gone through
                var latest = await Repository.LookupInfo(info.ResourceOwner, info.Identifier);
                Assert.NotNull(latest);
                latest.Version.Should().Be(info.Version);
            }
        }

        [Fact]
        public async Task Returns_Mismatch_When_ModifiedSince_Is_Up_To_Date()
        {
            var info = await Setup();

            using var client = CreateAuthenticatedClient();
            var lastModified = await GetLastModified(client, info);

            var request = CreateRequest(info);
            request.Headers.IfModifiedSince = lastModified;
            var isReadRequest = request.Method == HttpMethod.Get || request.Method == HttpMethod.Head;

            var response = await client.SendAsync(request);

            if (isReadRequest)
            {
                response.StatusCode.Should().Be(HttpStatusCode.NotModified);
            }
            else
            {
                response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);

                // update should not have gone through
                var latest = await Repository.LookupInfo(info.ResourceOwner, info.Identifier);
                Assert.NotNull(latest);
                latest.Version.Should().Be(info.Version);
            }
        }

        [Fact]
        public async Task Returns_PreconditionFailed_When_Etag_Is_Old()
        {
            var info = await Setup();

            using var client = CreateAuthenticatedClient();
            var etag = await GetETag(client, info);

            TimeProvider.Advance(TimeSpan.FromSeconds(10));
            info = await Modify(info);

            var request = CreateRequest(info);
            request.Headers.IfMatch.Add(etag);

            var response = await client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);

            // update should not have gone through
            var latest = await Repository.LookupInfo(info.ResourceOwner, info.Identifier);
            Assert.NotNull(latest);
            latest.Version.Should().Be(info.Version);
        }

        [Fact]
        public async Task Returns_PreconditionFailed_When_ModifiedSince_Is_Old()
        {
            var info = await Setup();

            using var client = CreateAuthenticatedClient();
            var lastModified = await GetLastModified(client, info);

            TimeProvider.Advance(TimeSpan.FromDays(1));
            info = await Modify(info);

            var request = CreateRequest(info);
            request.Headers.IfUnmodifiedSince = lastModified;

            var response = await client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);

            // update should not have gone through
            var latest = await Repository.LookupInfo(info.ResourceOwner, info.Identifier);
            Assert.NotNull(latest);
            latest.Version.Should().Be(info.Version);
        }
    }
    #endregion

    #region Utils
    private async Task AddResource(string name)
    {
        await using var resourceCmd = DataSource.CreateCommand(/*strpsql*/"INSERT INTO resourceregistry.resources (identifier, created, serviceresourcejson) VALUES (@name, NOW(), @json);");
        var nameParam = resourceCmd.Parameters.Add("name", NpgsqlTypes.NpgsqlDbType.Text);
        var jsonParam = resourceCmd.Parameters.Add("json", NpgsqlTypes.NpgsqlDbType.Jsonb);
        jsonParam.Value = "{}";

        nameParam.Value = name;
        await resourceCmd.ExecuteNonQueryAsync();
    }

    protected Guid GenerateUserId()
    {
        var id = Interlocked.Increment(ref _nextUserId) - 1;
        var lastGuidPart = id.ToString("D12");
        var guidString = $"00000000-0000-0000-0000-{lastGuidPart}";

        return Guid.Parse(guidString);
    }

    private class MockRegisterClient
        : IRegisterClient
    {
        public IAsyncEnumerable<PartyIdentifiers> GetPartyIdentifiers(IEnumerable<PartyReference> parties, CancellationToken cancellationToken = default)
        {
            List<int>? partyIds = null;
            List<Guid>? partyUuids = null;
            List<string>? orgNos = null;

            foreach (var party in parties)
            {
                switch (party)
                {
                    case PartyReference.PartyId partyId:
                        partyIds ??= new List<int>();
                        partyIds.Add(partyId.Value);
                        break;

                    case PartyReference.PartyUuid partyUuid:
                        partyUuids ??= new List<Guid>();
                        partyUuids.Add(partyUuid.Value);
                        break;

                    case PartyReference.OrganizationIdentifier orgNo:
                        orgNos ??= new List<string>();
                        orgNos.Add(orgNo.Value.ToString());
                        break;
                }
            }

            return GetPartyIdentifiers(partyIds, partyUuids, orgNos, cancellationToken);
        }

        public IAsyncEnumerable<PartyIdentifiers> GetPartyIdentifiers(IEnumerable<Guid> partyUuids, CancellationToken cancellationToken = default)
        {
            return GetPartyIdentifiers(partyIds: null, partyUuids, orgNos: null, cancellationToken);
        }

        private static async IAsyncEnumerable<PartyIdentifiers> GetPartyIdentifiers(
            IEnumerable<int>? partyIds,
            IEnumerable<Guid>? partyUuids,
            IEnumerable<string>? orgNos,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (partyIds is { } ids)
            {
                foreach (var id in ids)
                {
                    yield return MakePartyIdentifiers(id);
                }
            }

            if (partyUuids is { } uuids)
            {
                foreach (var uuid in uuids)
                {
                    yield return MakePartyIdentifiers(uuid);
                }
            }

            if (orgNos is { } orgs)
            {
                foreach (var org in orgs)
                {
                    yield return MakePartyIdentifiers(org);
                }
            }
        }

        private static PartyIdentifiers MakePartyIdentifiers(int id)
        {
            var orgNo = id.ToString("D9");
            var lastGuidPart = id.ToString("D12");
            var guidString = $"00000000-0000-0000-0000-{lastGuidPart}";
            var guid = Guid.Parse(guidString);

            return new PartyIdentifiers
            {
                PartyId = id,
                PartyUuid = guid,
                OrgNumber = orgNo,
            };
        }

        private static PartyIdentifiers MakePartyIdentifiers(Guid guid)
        {
            var lastGuidPart = guid.ToString().AsSpan()[^12..];
            var id = int.Parse(lastGuidPart);
            var orgNo = id.ToString("D9");

            return new PartyIdentifiers
            {
                PartyId = id,
                PartyUuid = guid,
                OrgNumber = orgNo,
            };
        }

        private static PartyIdentifiers MakePartyIdentifiers(string orgNo)
        {
            var id = int.Parse(orgNo);

            return MakePartyIdentifiers(id);
        }
    }
    #endregion
}
