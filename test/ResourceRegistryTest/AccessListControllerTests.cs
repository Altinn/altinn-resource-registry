﻿using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Models;
using Altinn.ResourceRegistry.Tests.Utils;
using Altinn.ResourceRegistry.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Altinn.ResourceRegistry.Tests;

public class AccessListControllerTests(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : WebApplicationTests(dbFixture, webApplicationFixture)
{
    private const string ORG_NR = "974761076";

    protected IAccessListsRepository Repository => Services.GetRequiredService<IAccessListsRepository>();
    protected AdvanceableTimeProvider TimeProvider => Services.GetRequiredService<AdvanceableTimeProvider>();


    private HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();

        var token = PrincipalUtil.GetOrgToken("skd", "974761076", AuthzConstants.SCOPE_ACCESS_LIST_WRITE);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
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
            await aggregate.SaveChanged();

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
}
