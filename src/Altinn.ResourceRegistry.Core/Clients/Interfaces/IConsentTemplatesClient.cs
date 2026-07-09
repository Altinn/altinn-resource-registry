using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Core.Clients.Interfaces
{
    /// <summary>
    /// interface for Consent Templates Client
    /// </summary>
    public interface IConsentTemplatesClient
    {
        /// <summary>
        /// Get all versions of all consent templates
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>The http response</returns>
        Task<List<ConsentTemplate>> GetConsentTemplates(CancellationToken cancellationToken = default);
    }
}
