namespace ResourceSubjectsLoader.Clients
{
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using global::Altinn.AccessManagement.UI.Core.ClientInterfaces;
    using global::Altinn.ResourceRegistry.Core.Models;

    namespace Altinn.AccessManagement.UI.Integration.Clients
    {
        /// <summary>
        ///     Client implementation for integration with the Resource Registry
        /// </summary>
        [ExcludeFromCodeCoverage]
        public class ResourceRegistryClient : IResourceRegistryClient
        {
            private readonly HttpClient _httpClient;

            /// <summary>
            ///     Initializes a new instance of the <see cref="ResourceRegistryClient" /> classß
            /// </summary>
            /// <param name="settings">The resource registry config settings</param>
            /// <param name="logger">Logger instance for this ResourceRegistryClient</param>
            public ResourceRegistryClient(HttpClient httpClient)
            {
                _httpClient = httpClient;
                _httpClient.Timeout = new TimeSpan(0, 0, 30);
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            /// <summary>
            ///     Gets all resources no matter if it's an AltinnApp or GenericAccessResource
            /// </summary>
            /// <returns>List of all resources</returns>
            public async Task<List<ServiceResource>> GetResourceList()
            {
                List<ServiceResource> resources = null;

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };
                try
                {
                    string endpointUrl = "resource/resourcelist";

                    HttpResponseMessage response = await _httpClient.GetAsync(endpointUrl);
                    string content = await response.Content.ReadAsStringAsync();

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return JsonSerializer.Deserialize<List<ServiceResource>>(content, options);
                    }
                }
                catch (Exception ex)
                {
                     throw;
                }

                return resources;
            }

            public async Task ReloadResourceSubects(string id)
            {

                List<ServiceResource> resources = null;

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };
                try
                {
                    string endpointUrl = $"resource/{id}/policy/subjects?reloadFromXacml=true";

                    HttpResponseMessage response = await _httpClient.GetAsync(endpointUrl);
                    string content = await response.Content.ReadAsStringAsync();

                  
                }
                catch (Exception ex)
                {
                    throw;
                }

            }
        }
    }
}
