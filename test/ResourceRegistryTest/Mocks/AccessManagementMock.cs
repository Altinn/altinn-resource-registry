using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Altinn.ResourceRegistry.Core.Clients.Interfaces;
using Altinn.ResourceRegistry.Core.Extensions;
using Altinn.ResourceRegistry.Core.Models;

namespace ResourceRegistryTest.Mocks
{
    public class AccessManagementMock : IAccessManagementClient
    {
        public Task<HttpResponseMessage> AddResourceToAccessManagement(List<AccessManagementResource> resources)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage();
            responseMessage.Content = JsonContent.Create(resources);

            return Task.FromResult(responseMessage);
        }
    }
}
