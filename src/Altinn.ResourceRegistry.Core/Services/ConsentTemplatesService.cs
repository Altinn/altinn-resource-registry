using Altinn.ResourceRegistry.Core.Clients.Interfaces;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Services.Interfaces;

namespace Altinn.ResourceRegistry.Core.Services
{
    /// <summary>
    /// Service implementation for operations on consent templates
    /// </summary>
    public class ConsentTemplatesService : IConsentTemplatesService
    {
        private readonly IConsentTemplatesClient _client;

        /// <summary>
        /// Creates a new instance of the <see cref="ConsentTemplatesService"/> service.
        /// The ConsentTemplatesService is responsible for business logic and implementations for working with consent templates
        /// </summary>
        public ConsentTemplatesService(IConsentTemplatesClient client)
        {
            _client = client;
        }

        /// <inheritdoc/>
        public async Task<List<ConsentTemplate>> GetConsentTemplates(CancellationToken cancellationToken)
        {
            var allTemplates = await _client.GetConsentTemplates(cancellationToken);
            return allTemplates
                .GroupBy(x => x.Id)
                .Select(g => g.OrderByDescending(x => x.Version).First())
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<ConsentTemplate> GetConsentTemplate(string id, int? version = null, CancellationToken cancellationToken = default)
        {
            var templates = await _client.GetConsentTemplates(cancellationToken);
            if (version != null)
            {
                return templates.Find(x => x.Id == id && x.Version == version);
            }
            else
            {
                return templates
                    .Where(x => x.Id == id)
                    .OrderByDescending(x => x.Version)
                    .First();
            }
        }
    }
}
