using Altinn.ResourceRegistry.Core.Models;
using System.Net.Http.Json;
using Altinn.ResourceRegistry.TestUtils;
using Microsoft.Extensions.DependencyInjection;
using Altinn.ResourceRegistry.Tests.Mocks;
using Altinn.ResourceRegistry.Core.Clients.Interfaces;
using System.Net;

namespace Altinn.ResourceRegistry.Tests
{
    public class ConsentTemplatesControllerTests(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : WebApplicationTests(dbFixture, webApplicationFixture)
    {
        protected override void ConfigureTestServices(IServiceCollection services)
        {
            services.AddSingleton<IConsentTemplatesClient, ConsentTemplatesClientMock>();

            base.ConfigureTestServices(services);
        }

        [Fact]
        public async Task GetConsentTemplates_OK()
        {
            var client = CreateClient();
            string requestUri = "resourceregistry/api/v1/consent-templates";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            var content = await response.Content.ReadFromJsonAsync<List<ConsentTemplate>>();

            Assert.NotNull(content);
            Assert.Equal(5, content.Count);
            Assert.Equal(3, content.First(c => c.Id == "samtykkemal_simpleconsent").Version);
            Assert.Equal(2, content.First(c => c.Id == "default").Version);
            Assert.Equal(1, content.First(c => c.Id == "bst_krav").Version);
            Assert.Equal(1, content.First(c => c.Id == "sblanesoknad").Version);
            Assert.Equal(1, content.First(c => c.Id == "poa").Version);
        }

        [Fact]
        public async Task GetConsentTemplate_UnspecifiedVersion_ReturnHighestVersion()
        {
            var client = CreateClient();
            string requestUri = "resourceregistry/api/v1/consent-templates/samtykkemal_simpleconsent";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            var content = await response.Content.ReadFromJsonAsync<ConsentTemplate>();

            Assert.NotNull(content);
            Assert.Equal("samtykkemal_simpleconsent", content.Id);
            Assert.Equal(3, content.Version);
        }

        [Fact]
        public async Task GetConsentTemplate_SpecificVersion_ReturnSpecificVersion()
        {
            var client = CreateClient();
            string requestUri = "resourceregistry/api/v1/consent-templates/samtykkemal_simpleconsent?version=2";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            var content = await response.Content.ReadFromJsonAsync<ConsentTemplate>();

            Assert.NotNull(content);
            Assert.Equal("samtykkemal_simpleconsent", content.Id);
            Assert.Equal(2, content.Version);
        }
        
        [Fact]
        public async Task GetConsentTemplate_SpecificNonExistingVersion_ReturnNotFound()
        {
            var client = CreateClient();
            string requestUri = "resourceregistry/api/v1/consent-templates/samtykkemal_simpleconsent?version=4";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);


            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}