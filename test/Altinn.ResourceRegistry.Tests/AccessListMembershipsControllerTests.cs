using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Register;
using Altinn.ResourceRegistry.Models;
using Altinn.ResourceRegistry.Tests.Utils;
using Altinn.ResourceRegistry.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Altinn.ResourceRegistry.Tests;

public class AccessListMembershipsControllerTests(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
    : WebApplicationTests(dbFixture, webApplicationFixture)
{
    private const string ORG_NR = "974761076";

    protected IAccessListsRepository Repository => Services.GetRequiredService<IAccessListsRepository>();
    protected AdvanceableTimeProvider TimeProvider => Services.GetRequiredService<AdvanceableTimeProvider>();

    private HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();

        var token = PrincipalUtil.GetOrgToken("skd", "974761076", $"{AuthzConstants.SCOPE_RESOURCE_ADMIN}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    [Fact]
    public async Task By_Party_Returns_Multiple_Memberships_To_Same_Resource()
    {
        const string RESOURCE1 = "resource1";

        await AddResource(RESOURCE1);
        var resource = ResourceUrn.ResourceId.Create(ResourceIdentifier.CreateUnchecked(RESOURCE1));
        var user1 = PartyUrn.PartyUuid.Create(GenerateUserId());
        var user2 = PartyUrn.PartyUuid.Create(GenerateUserId());

        var list1 = await Repository.CreateAccessList(
            resourceOwner: ORG_NR, 
            identifier: "access-list1", 
            name: "Access List 1",
            description: "description1");

        list1.AddResourceConnection(RESOURCE1, []);
        list1.AddMembers([user1.Value, user2.Value]);
        await list1.SaveChanges();

        var list2 = await Repository.CreateAccessList(
            resourceOwner: ORG_NR,
            identifier: "access-list2",
            name: "Access List 2",
            description: "description2");

        list2.AddResourceConnection(RESOURCE1, []);
        list2.AddMembers([user1.Value]);
        await list2.SaveChanges();

        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/memberships?party={user1}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var memberships = await response.Content.ReadFromJsonAsync<ListObject<AccessListResourceMembershipDto>>();
        Assert.NotNull(memberships);

        memberships.Items.Should().HaveCount(2);
        memberships.Items.Should().AllSatisfy(m =>
        {
            m.Party.Should().Be(user1);
            m.Resource.Should().Be(resource);
        });
    }

    [Fact]
    public async Task By_Party_Returns_Multiple_Resources()
    {
        const string RESOURCE1 = "resource1";
        const string RESOURCE2 = "resource2";

        await AddResource(RESOURCE1);
        await AddResource(RESOURCE2);

        var resource1 = ResourceUrn.ResourceId.Create(ResourceIdentifier.CreateUnchecked(RESOURCE1));
        var resource2 = ResourceUrn.ResourceId.Create(ResourceIdentifier.CreateUnchecked(RESOURCE2));
        var user1 = PartyUrn.PartyUuid.Create(GenerateUserId());
        var user2 = PartyUrn.PartyUuid.Create(GenerateUserId());

        var list1 = await Repository.CreateAccessList(
            resourceOwner: ORG_NR,
            identifier: "access-list1",
            name: "Access List 1",
            description: "description1");

        list1.AddResourceConnection(RESOURCE1, []);
        list1.AddResourceConnection(RESOURCE2, []);
        list1.AddMembers([user1.Value, user2.Value]);
        await list1.SaveChanges();

        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/memberships?party={user1}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var memberships = await response.Content.ReadFromJsonAsync<ListObject<AccessListResourceMembershipDto>>();
        Assert.NotNull(memberships);

        memberships.Items.Should().HaveCount(2);
        memberships.Items.Should().Contain(m => m.Party == user1 && m.Resource == resource1);
        memberships.Items.Should().Contain(m => m.Party == user1 && m.Resource == resource2);
    }

    [Fact]
    public async Task By_Party_And_Resource_Returns_Single_Resource()
    {
        const string RESOURCE1 = "resource1";
        const string RESOURCE2 = "resource2";

        await AddResource(RESOURCE1);
        await AddResource(RESOURCE2);

        var resource1 = ResourceUrn.ResourceId.Create(ResourceIdentifier.CreateUnchecked(RESOURCE1));
        var resource2 = ResourceUrn.ResourceId.Create(ResourceIdentifier.CreateUnchecked(RESOURCE2));
        var user1 = PartyUrn.PartyUuid.Create(GenerateUserId());
        var user2 = PartyUrn.PartyUuid.Create(GenerateUserId());

        var list1 = await Repository.CreateAccessList(
            resourceOwner: ORG_NR,
            identifier: "access-list1",
            name: "Access List 1",
            description: "description1");

        list1.AddResourceConnection(RESOURCE1, []);
        list1.AddResourceConnection(RESOURCE2, []);
        list1.AddMembers([user1.Value, user2.Value]);
        await list1.SaveChanges();

        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/memberships?party={user1}&resource={resource1}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var memberships = await response.Content.ReadFromJsonAsync<ListObject<AccessListResourceMembershipDto>>();
        Assert.NotNull(memberships);

        memberships.Items.Should().HaveCount(1);
        memberships.Items.Should().Contain(m => m.Party == user1 && m.Resource == resource1);
    }

    #region Authorization
    public class Authorization(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : AccessListMembershipsControllerTests(dbFixture, webApplicationFixture)
    {
        [Fact]
        public async Task Unauthenticated_Returns_Unauthorized()
        {
            using var client = CreateClient();

            var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/memberships?party=urn:altinn:party:uuid:{GenerateUserId()}");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task MissingScope_Returns_Forbidden()
        {
            using var client = CreateClient();

            var token = PrincipalUtil.GetOrgToken("skd", "974761076", "some.scope");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/memberships?party=urn:altinn:party:uuid:{GenerateUserId()}");
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CorrectScopeAndAdmin_Returns_Ok()
        {
            using var client = CreateClient();

            var token = PrincipalUtil.GetOrgToken("skd", "974761076", $"{AuthzConstants.SCOPE_RESOURCE_ADMIN}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/memberships?party=urn:altinn:party:uuid:{GenerateUserId()}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
    #endregion
}
