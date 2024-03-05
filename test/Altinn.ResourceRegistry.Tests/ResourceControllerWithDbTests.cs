using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Models;
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
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Altinn.ResourceRegistry.Tests;

public class ResourceControllerWithDbTests(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : WebApplicationTests(dbFixture, webApplicationFixture)
{
    private const string ORG_NR = "974761076";

    protected IResourceRegistryRepository Repository => Services.GetRequiredService<IResourceRegistryRepository>();
    protected AdvanceableTimeProvider TimeProvider => Services.GetRequiredService<AdvanceableTimeProvider>();
    protected NpgsqlDataSource DataSource => Services.GetRequiredService<NpgsqlDataSource>();


    private HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();

        var token = PrincipalUtil.GetOrgToken("skd", "974761076", AuthzConstants.SCOPE_ACCESS_LIST_WRITE);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    [Fact]
    public async Task GetResourceForSubjects()
    {
        ResourceSubjects resourceSubjects = CreateResourceSubjects("urn:altinn:resource:skd_mva", new List<string>{ "urn:altinn:rolecode:utinn"}, "skd");

        await Repository.SetResourceSubjects(resourceSubjects);
        using var client = CreateAuthenticatedClient();

        List<string> subjects = new List<string>();
        subjects.Add("urn:altinn:rolecode:utinn");

        string requestUri = "resourceregistry/api/v1/resource/findforsubjects/";

        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(JsonSerializer.Serialize(subjects), Encoding.UTF8, "application/json")
        };

        httpRequestMessage.Headers.Add("Accept", "application/json");
        httpRequestMessage.Headers.Add("ContentType", "application/json");

        HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
        List<SubjectResources>? subjectResources = await response.Content.ReadFromJsonAsync<List<SubjectResources>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(subjectResources);
        Assert.NotNull(subjectResources.FirstOrDefault(r => r.Subject.Urn.Contains("utinn")));
    }

    #region Utils
    private ResourceSubjects CreateResourceSubjects(string resourceurn, List<string> subjecturns, string owner)
    {
        ResourceSubjects resourceSubjects = new ResourceSubjects()
        {
            Resource = new AttributeMatchV2()
            {
                Type = resourceurn.Substring(0, resourceurn.LastIndexOf(":")),
                Value = resourceurn.Substring(resourceurn.LastIndexOf(":") + 1),
                Urn = resourceurn
            },
            ResourceOwner = "owner",
        };

        resourceSubjects.Subjects = new List<AttributeMatchV2>();
        foreach(string subjecturn in subjecturns)
        {
            resourceSubjects.Subjects.Add(new AttributeMatchV2 { Type = subjecturn.Substring(0, subjecturn.LastIndexOf(":")), Value = subjecturn.Substring(subjecturn.LastIndexOf(":") + 1), Urn = subjecturn });
        }

        return resourceSubjects;
    }
    #endregion
}
