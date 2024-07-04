using Altinn.ResourceRegistry.Controllers;
using Altinn.ResourceRegistry.Core.Models.Altinn2;
using Altinn.ResourceRegistry.Tests.Utils;
using AngleSharp.Dom;
using Npgsql.Internal;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;

namespace Altinn.ResourceRegistry.Tests
{
    public class Altinn2ExportControllerTest : IClassFixture<CustomWebApplicationFactory<ResourceController>>
    {

        private readonly CustomWebApplicationFactory<ResourceController> _factory;

        public Altinn2ExportControllerTest(CustomWebApplicationFactory<ResourceController> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Export_Resource()
        {
            HttpClient client = SetupUtil.GetTestClient(_factory);
            string requestUri = "resourceregistry/api/v1/altinn2export/resource?serviceCode=4485&serviceEditionCode=2021";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
            response.EnsureSuccessStatusCode();
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.NotNull(responseContent);
        }


        [Fact]
        public async Task Export_Policy()
        {
            HttpClient client = SetupUtil.GetTestClient(_factory);
            string requestUri = "resourceregistry/api/v1/altinn2export/policy?serviceCode=4485&serviceEditionCode=2021";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
            response.EnsureSuccessStatusCode();
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.NotNull(responseContent);
        }

        [Fact]
        public async Task DelegationCount()
        {
            HttpClient client = SetupUtil.GetTestClient(_factory);
            string token = PrincipalUtil.GetAccessToken("studio.designer");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            string requestUri = "resourceregistry/api/v1/altinn2export/delegationcount?serviceCode=4485&serviceEditionCode=2021";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };
            httpRequestMessage.Headers.Accept.Clear();
            httpRequestMessage.Headers.Accept.ParseAdd("application/json");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
            response.EnsureSuccessStatusCode();
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.NotNull(responseContent);
            DelegationCountOverview? delegationCountOverview = JsonSerializer.Deserialize<DelegationCountOverview>(responseContent, new JsonSerializerOptions() {  PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
            Assert.NotNull(delegationCountOverview);
            Assert.Equal(13336, delegationCountOverview.NumberOfRelations);
            Assert.Equal(13337, delegationCountOverview.NumberOfDelegations);
        }

        /// <summary>
        /// Calls count endopint with wrong id in token
        /// </summary>
        [Fact]
        public async Task DelegationCount_WrongConsumer()
        {
            HttpClient client = SetupUtil.GetTestClient(_factory);
            string token = PrincipalUtil.GetAccessToken("studi2o.designer");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            string requestUri = "resourceregistry/api/v1/altinn2export/delegationcount?serviceCode=4485&serviceEditionCode=2021";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Calls count endopint with wrong id in token
        /// </summary>
        [Fact]
        public async Task DelegationCount_WithoutToken()
        {
            HttpClient client = SetupUtil.GetTestClient(_factory);
            string requestUri = "resourceregistry/api/v1/altinn2export/delegationcount?serviceCode=4485&serviceEditionCode=2021";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// Tries to trigger batch without Altinn Studio token
        /// </summary>
        [Fact]
        public async Task Trigger_Batch_Unauthorized()
        {
            HttpClient client = SetupUtil.GetTestClient(_factory);
            string requestUri = "resourceregistry/api/v1/altinn2export/exportdelegations";

            ExportDelegationsRequestBE exportDelegationsRequestBE = new ExportDelegationsRequestBE();
            exportDelegationsRequestBE.ServiceCode = "123";
            exportDelegationsRequestBE.ServiceEditionCode = 12314;
            exportDelegationsRequestBE.DateTimeForExport = DateTime.Now;

            using HttpContent content = JsonContent.Create(exportDelegationsRequestBE);
            HttpResponseMessage response = await client.PostAsync(requestUri, content);

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// Tries to trigger batch without resourceID is set. 
        /// </summary>
        [Fact]
        public async Task Trigger_Batch_MissingResource()
        {
            HttpClient client = SetupUtil.GetTestClient(_factory);
            string token = PrincipalUtil.GetAccessToken("studio.designer");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            string requestUri = "resourceregistry/api/v1/altinn2export/exportdelegations";

            ExportDelegationsRequestBE exportDelegationsRequestBE = new ExportDelegationsRequestBE();
            exportDelegationsRequestBE.ServiceCode = "123";
            exportDelegationsRequestBE.ServiceEditionCode = 12314;
            exportDelegationsRequestBE.DateTimeForExport = DateTime.Now;

            using HttpContent content = JsonContent.Create(exportDelegationsRequestBE);
            HttpResponseMessage response = await client.PostAsync(requestUri, content);

            string responseContent = await response.Content.ReadAsStringAsync();


            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        /// <summary>
        /// Tries to trigger batch required parameters
        /// </summary>
        [Fact]
        public async Task Trigger_Batch_Ok()
        {
            HttpClient client = SetupUtil.GetTestClient(_factory);
            string token = PrincipalUtil.GetAccessToken("studio.designer");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            string requestUri = "resourceregistry/api/v1/altinn2export/exportdelegations";

            ExportDelegationsRequestBE exportDelegationsRequestBE = new ExportDelegationsRequestBE();
            exportDelegationsRequestBE.ServiceCode = "4795";
            exportDelegationsRequestBE.ServiceEditionCode = 1;
            exportDelegationsRequestBE.DateTimeForExport = DateTime.Now;
            exportDelegationsRequestBE.ResourceId = "nav_tiltakAvtaleOmArbeidstrening";

            using HttpContent content = JsonContent.Create(exportDelegationsRequestBE);
            HttpResponseMessage response = await client.PostAsync(requestUri, content);

            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
        }

        /// <summary>
        /// Tries to trigger batch required parameters but org for resource and service does not match
        /// </summary>
        [Fact]
        public async Task Trigger_Batch_NoMatchingORg()
        {
            HttpClient client = SetupUtil.GetTestClient(_factory);
            string token = PrincipalUtil.GetAccessToken("studio.designer");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            string requestUri = "resourceregistry/api/v1/altinn2export/exportdelegations";

            ExportDelegationsRequestBE exportDelegationsRequestBE = new ExportDelegationsRequestBE();
            exportDelegationsRequestBE.ServiceCode = "4804";
            exportDelegationsRequestBE.ServiceEditionCode = 170223;
            exportDelegationsRequestBE.DateTimeForExport = DateTime.Now;
            exportDelegationsRequestBE.ResourceId = "nav_tiltakAvtaleOmArbeidstrening";

            using HttpContent content = JsonContent.Create(exportDelegationsRequestBE);
            HttpResponseMessage response = await client.PostAsync(requestUri, content);

            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
