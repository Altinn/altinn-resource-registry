using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Altinn.ResourceRegistry.Controllers;
using Microsoft.AspNetCore.TestHost;
using Altinn.ResourceRegistry.Tests.Utils;
using Xunit;

namespace Altinn.ResourceRegistry.Tests
{
    public class HealthCheckTests : IClassFixture<CustomWebApplicationFactory<ResourceController>>
    {
        private readonly CustomWebApplicationFactory<ResourceController> _factory;

        public HealthCheckTests(CustomWebApplicationFactory<ResourceController> factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Verify that component responds on health check
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task VerifyHeltCheck_OK()
        {
            HttpClient client = SetupUtil.GetTestClient(_factory);

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "/health")
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
            await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

    }
}
