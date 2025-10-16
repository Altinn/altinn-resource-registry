using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Core.Services.Interfaces
{
    /// <summary>
    /// Interface for the ConsentTemplatesService implementation
    /// </summary>
    public interface IConsentTemplatesService
    {
        /// <summary>
        /// Get list of all consent templates. Will return newest version of each template.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>List of <see cref="ConsentTemplate"/></returns>
        Task<List<ConsentTemplate>> GetConsentTemplates(CancellationToken cancellationToken);

        /// <summary>
        /// Get a single consent template by id. If version is not specified, the newest version will be returned.
        /// </summary>
        /// <param name="id">Template id to get</param>
        /// <param name="version">Specific template version</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>A single <see cref="ConsentTemplate"/></returns>
        Task<ConsentTemplate> GetConsentTemplate(string id, int? version = null, CancellationToken cancellationToken = default);
    }
}
