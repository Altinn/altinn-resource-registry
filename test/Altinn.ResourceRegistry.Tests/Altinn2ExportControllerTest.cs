using Altinn.ResourceRegistry.TestUtils;

namespace Altinn.ResourceRegistry.Tests
{
    public class Altinn2ExportControllerTest(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : WebApplicationTests(dbFixture, webApplicationFixture)
    {
        [Fact]
        public async Task Export_Resource()
        {
            HttpClient client = CreateClient();
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
            HttpClient client = CreateClient();
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

            ExportDelegationsRequestBE exportDelegationsRequestBE = new ExportDelegationsRequestBE()
               {
                ServiceCode = "123",
                ServiceEditionCode = 12314,
                DateTimeForExport = DateTime.Now,
                ResourceId = "nav_tiltakAvtaleOmArbeidstrening"
            };

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

            ExportDelegationsRequestBE exportDelegationsRequestBE = new ExportDelegationsRequestBE()
            {
                ServiceCode = "123",
                ServiceEditionCode = 12314,
                DateTimeForExport = DateTime.Now,
                ResourceId = string.Empty
            };

            exportDelegationsRequestBE.ResourceId = null;

            using HttpContent content = JsonContent.Create(exportDelegationsRequestBE);
            HttpResponseMessage response = await client.PostAsync(requestUri, content);

            string responseContent = await response.Content.ReadAsStringAsync();


            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("The ResourceId field is required", responseContent);
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

            ExportDelegationsRequestBE exportDelegationsRequestBE = new ExportDelegationsRequestBE()
            {
                ServiceCode = "4795",
                ServiceEditionCode = 1,
                DateTimeForExport = DateTime.Now,
                ResourceId = "nav_tiltakAvtaleOmArbeidstrening"
            };

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

            ExportDelegationsRequestBE exportDelegationsRequestBE = new ExportDelegationsRequestBE() 
            { 
                ResourceId = "nav_tiltakAvtaleOmArbeidstrening" , 
                DateTimeForExport = DateTime.Now, 
                ServiceCode = "4804", 
                ServiceEditionCode = 170223
            };
            
            using HttpContent content = JsonContent.Create(exportDelegationsRequestBE);
            HttpResponseMessage response = await client.PostAsync(requestUri, content);

            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
