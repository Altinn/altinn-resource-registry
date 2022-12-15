using System.Net.Http.Headers;
using System.Net.Http.Json;
using Altinn.ResourceRegistry.Core.Clients.Interfaces;
using Altinn.ResourceRegistry.Core.Configuration;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.ResourceRegistry.Core.Clients
{
    /// <summary>
    /// Client to access Access Managment component
    /// </summary>
    public class AccessManagementClient : IAccessManagementClient
    {
        private readonly IAccessTokenProvider _accessTokenProvider;
        private readonly ILogger<IAccessManagementClient> _logger;
        private readonly PlatformSettings _settings;
        private const string AccessManagmentEndpoint = "resources";

        /// <summary>
        /// Gets an instance of httpclient from httpclientfactory
        /// </summary>
        public HttpClient Client { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccessManagementClient"/> class
        /// </summary>
        /// <param name="client">httpClient</param>
        /// <param name="accessTokenProvider">The token provider to create the token needed for comunication</param>
        /// <param name="platformSettings">The resource registry config settings</param>
        /// <param name="logger">Logger instance for this ResourceRegistryClient</param>
        public AccessManagementClient(HttpClient client, IAccessTokenProvider accessTokenProvider, IOptions<PlatformSettings> platformSettings, ILogger<AccessManagementClient> logger)
        {
            _accessTokenProvider = accessTokenProvider;
            _logger = logger;
            _settings = platformSettings.Value;
            Client = client;
            Client.BaseAddress = new Uri(_settings.AccessManagementEndpoint);
            Client.Timeout = new TimeSpan(0, 0, 30);
            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Posts a list of delegation events to the Altinn Bridge API endpoint
        /// </summary>
        /// <param name="resources">A list of resources to add to Access Management</param>
        /// <returns>A HTTP response message</returns>
        public async Task<HttpResponseMessage> AddResourceToAccessManagement(List<AccessManagementResource> resources)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, AccessManagmentEndpoint)
            {
                Content = JsonContent.Create(resources),
                
                Headers =
                {
                    Authorization = new AuthenticationHeaderValue("Bearer", await _accessTokenProvider.GetAccessToken())
                }
            };

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "AccessManagementClient posting resource list to {url} with token {token} and body {body}",
                    request.RequestUri,
                    request.Headers.Authorization.ToString(),
                    await request.Content.ReadAsStringAsync());
            }

            return await Client.SendAsync(request);
        }
    }
}
