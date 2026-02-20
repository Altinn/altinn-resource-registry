using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using Altinn.ResourceRegistry.Core.Clients.Interfaces;
using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Core.Clients
{
    /// <summary>
    /// Client to get consent templates
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ConsentTemplatesClient : IConsentTemplatesClient
    {
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        private readonly HttpClient _client;

        /// <summary>
        /// Creates a new instance of the <see cref="ConsentTemplatesClient"/> client.
        /// The ConsentTemplatesClient is responsible for getting consent templates from altinn-studio-docs
        /// </summary>
        public ConsentTemplatesClient(HttpClient client)
        {
            _client = client;
        }

        /// <inheritdoc />
        public async Task<List<ConsentTemplate>> GetConsentTemplates(CancellationToken cancellationToken = default)
        {
            // Get consent templates from altinn-studio-docs. Will be moved to database later.
            string endpointUrl = "https://raw.githubusercontent.com/Altinn/altinn-studio-docs/master/content/authorization/architecture/resourceregistry/consent_templates.json";

            HttpResponseMessage response = await _client.GetAsync(endpointUrl, cancellationToken);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string content = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<List<ConsentTemplate>>(content, SerializerOptions);
            }

            return null;
        }
    }
}
