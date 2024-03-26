using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.AccessManagement.UI.Core.ClientInterfaces
{
    /// <summary>
    ///     Interface for client integration with the Resource Registry
    /// </summary>
    public interface IResourceRegistryClient
    {
        /// <summary>
        ///     Integration point for retrieving the full list of resources
        /// </summary>
        /// <returns>The resource full list of all resources if exists</returns>
        Task<List<ServiceResource>> GetResourceList();

        Task ReloadResourceSubects(string id);
    }
}
