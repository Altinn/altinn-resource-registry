using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.ResourceRegistry.Core.Services.Interfaces
{
    /// <summary>
    /// Interface 
    /// </summary>
    public interface IApplications
    {
        /// <summary>
        /// Get a full list of Altinn 3 applications
        /// </summary>
        /// <param name="includeMigratedResources">filter migrated resources from A1/A2, default er false</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task<ApplicationList> GetApplicationList(bool includeMigratedResources = false, CancellationToken cancellationToken = default);
    }
}
