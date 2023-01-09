using System.Net.Http;

using Altinn.ResourceRegistry.Controllers;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRgistryTest.Tests.Mocks.Authentication;
using AltinnCore.Authentication.JwtCookie;
using Altinn.ResourceRegistry.Core.Clients.Interfaces;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ResourceRegistryTest.Mocks;



namespace ResourceRegistryTest.Utils
{
    public static class SetupUtil
    {
        public static HttpClient GetTestClient(
            CustomWebApplicationFactory<ResourceController> customFactory)
        {
            WebApplicationFactory<ResourceController> factory = customFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
                    services.AddSingleton<IResourceRegistryRepository, RegisterResourceRepositoryMock>();
                    services.AddSingleton<IPolicyRepository, PolicyRepositoryMock>();
                    services.AddSingleton<IPRP, PRPMock>();
                    services.AddSingleton<IAccessManagementClient, AccessManagementMock>();

                });
            });
            factory.Server.AllowSynchronousIO = true;
            return factory.CreateClient();
        }


        public static HttpClient GetTestClient(
        CustomWebApplicationFactory<ExportController> customFactory)
        {
            WebApplicationFactory<ExportController> factory = customFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<IResourceRegistryRepository, RegisterResourceRepositoryMock>();
                    services.AddSingleton<IPolicyRepository, PolicyRepositoryMock>();
                    services.AddSingleton <IPRP, PRPMock>();
                });
            });
            factory.Server.AllowSynchronousIO = true;
            return factory.CreateClient();
        }
    }
}
