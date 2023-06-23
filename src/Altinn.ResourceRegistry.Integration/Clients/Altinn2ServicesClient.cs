using Altinn.ResourceRegistry.Core.Configuration;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Altinn2;
using Altinn.ResourceRegistry.Core.Services;
using Microsoft.Extensions.Options;

namespace Altinn.ResourceRegistry.Integration.Clients
{
    /// <summary>
    /// Http client used for 
    /// </summary>
    public class Altinn2ServicesClient : IAltinn2Services
    {
        private readonly HttpClient _client;
        private readonly PlatformSettings _settings;
        
        /// <summary>
        /// Client implemenation
        /// </summary>
        public Altinn2ServicesClient(HttpClient client, IOptions<PlatformSettings> settings)
        {
            _client = client;
            _settings = settings.Value;
        }

        /// <summary>
        /// Returns a list of Available services from Altinn 2 Bridge
        /// </summary>
        /// <returns></returns>
        public async Task<List<AvailableService>> AvailableServices(int languageId)
        {
            List<AvailableService> orgList;
            string availabbleServicePath = _settings.BridgeApiEndpoint + $"metadata/api/availableServices?languageID={languageId}&appTypesToInclude=1&includeExpired=false";

            try
            {
                HttpResponseMessage response = await _client.GetAsync(availabbleServicePath);
                
                string orgListString = await response.Content.ReadAsStringAsync();
                orgList = System.Text.Json.JsonSerializer.Deserialize<List<AvailableService>>(orgListString, new System.Text.Json.JsonSerializerOptions());
                return orgList;
            }
            catch (Exception ex)
            {
                throw new Exception($"Something went wrong when retrieving Action options", ex);
            }
         
        }
        
    }
}
