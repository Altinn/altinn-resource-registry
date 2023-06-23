using System.Net;
using Altinn.ResourceRegistry.Core.Clients.Interfaces;
using Altinn.ResourceRegistry.Core.Exceptions;
using Altinn.ResourceRegistry.Core.Extensions;
using Altinn.ResourceRegistry.Core.Helpers;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Altinn2;
using Altinn.ResourceRegistry.Core.Services.Interfaces;
using Azure;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.ResourceRegistry.Core.Services
{
    /// <summary>
    /// Service implementation for operations on the resource registry
    /// </summary>
    public class ResourceRegistryService : IResourceRegistry
    {
        private readonly IResourceRegistryRepository _repository;
        private readonly IPolicyRepository _policyRepository;
        private readonly IAccessManagementClient _accessManagementClient;
        private readonly IAltinn2Services _altinn2ServicesClient;

        /// <summary>
        /// Creates a new instance of the <see cref="ResourceRegistryService"/> service.
        /// The ResourceRegistryService is responcible for business logic and implementations for working with the resource registry
        /// </summary>
        /// <param name="repository">Resource registry repository implementation for dependencies to its operations</param>
        /// <param name="policyRepository">Repository implementation for operations on policies</param>
        /// <param name="logger">Logger</param>
        /// <param name="accessManagementClient">client to send data to AccessManagement</param>
        public ResourceRegistryService(IResourceRegistryRepository repository, IPolicyRepository policyRepository, ILogger<ResourceRegistryService> logger, IAccessManagementClient accessManagementClient, IAltinn2Services altinn2ServicesClient)
        {
            _repository = repository;
            _policyRepository = policyRepository;
            _accessManagementClient = accessManagementClient;
            _altinn2ServicesClient = altinn2ServicesClient;
        }

        /// <inheritdoc/>
        public async Task CreateResource(ServiceResource serviceResource)
        {
            bool result = await UpdateResourceInAccessManagement(serviceResource);
            if (!result)
            {
                throw new AccessManagementUpdateException("Updating Access management failed");
            }

            await _repository.CreateResource(serviceResource);
        }

        /// <inheritdoc/>
        public async Task UpdateResource(ServiceResource serviceResource)
        {
            bool result = await UpdateResourceInAccessManagement(serviceResource);
            if (!result)
            {
                throw new AccessManagementUpdateException("Updating Access management failed");
            }

            await _repository.UpdateResource(serviceResource);
        }

        /// <inheritdoc/>
        public async Task Delete(string id)
        {
            await _repository.DeleteResource(id);
        }

        /// <inheritdoc/>
        public async Task<ServiceResource> GetResource(string id)
        {
            return await _repository.GetResource(id);
        }

        /// <inheritdoc/>
        public async Task<List<ServiceResource>> Search(ResourceSearch resourceSearch)
        {
            return await _repository.Search(resourceSearch);
        }

        /// <inheritdoc/>
        public async Task<bool> StorePolicy(ServiceResource serviceResource, Stream fileStream)
        {
            PolicyHelper.IsValidResourcePolicy(serviceResource, fileStream);
            Response<BlobContentInfo> response = await _policyRepository.WritePolicyAsync(serviceResource.Identifier, fileStream);

            return response?.GetRawResponse()?.Status == (int)HttpStatusCode.Created;
        }

        /// <inheritdoc/>
        public async Task<Stream> GetPolicy(string resourceId)
        {
              return await _policyRepository.GetPolicyAsync(resourceId);
        }

        /// <inheritdoc />
        public async Task<bool> UpdateResourceInAccessManagement(ServiceResource serviceResource)
        {
            AccessManagementResource convertedElement = new AccessManagementResource(serviceResource);
            List<AccessManagementResource> convertedElementList = convertedElement.ElementToList();
            HttpResponseMessage response = await _accessManagementClient.AddResourceToAccessManagement(convertedElementList);

            return response.StatusCode == HttpStatusCode.Created;
        }

        /// <inheritdoc />
        public async Task<List<ServiceResource>> GetResourceList(bool includeApps, bool includeAltinn2)
        {
            List<ServiceResource> serviceResources = new List<ServiceResource>();
            List<AvailableService> altinn2List1044 = await _altinn2ServicesClient.AvailableServices(1044);
            List<AvailableService> altinn2List2068 = await _altinn2ServicesClient.AvailableServices(2068);
            List<AvailableService> altinn2List1033 = await _altinn2ServicesClient.AvailableServices(1033);

            foreach (AvailableService service in altinn2List1044)
            {
                string nntext = string.Empty;
                string entext = string.Empty;

                AvailableService service2068 = altinn2List2068.FirstOrDefault(r => r.ExternalServiceCode == service.ExternalServiceCode && r.ExternalServiceEditionCode == service.ExternalServiceEditionCode);
                if (service2068 != null)
                {
                    nntext = service2068.ServiceEditionVersionName;
                }


                AvailableService service1033 = altinn2List1033.FirstOrDefault(r => r.ExternalServiceCode == service.ExternalServiceCode && r.ExternalServiceEditionCode == service.ExternalServiceEditionCode);
                if (service1033 != null)
                {
                    nntext = service1033.ServiceEditionVersionName;
                }

                serviceResources.Add(MapAltinn2ServiceToServiceResource(service, entext, nntext));

            }

            return serviceResources;
        }

        private ServiceResource MapAltinn2ServiceToServiceResource(AvailableService availableService, string enText, string nnText)
        {
            ServiceResource serviceResource = new ServiceResource();
            serviceResource.Title = new Dictionary<string, string>();
            serviceResource.Title.Add("nb", availableService.ServiceEditionVersionName);
            serviceResource.Title.Add("en", enText);
            serviceResource.Title.Add("nn", nnText);
            serviceResource.ResourceReferences = new List<ResourceReference>();
            serviceResource.ResourceReferences.Add(new ResourceReference() { ReferenceType = Enums.ReferenceType.ServiceCode, Reference = availableService.ExternalServiceCode, ReferenceSource = Enums.ReferenceSource.Altinn2 });
            serviceResource.ResourceReferences.Add(new ResourceReference() { ReferenceType = Enums.ReferenceType.ServiceEditionCode, Reference = availableService.ExternalServiceEditionCode.ToString(), ReferenceSource = Enums.ReferenceSource.Altinn2 });
            return serviceResource;
        }
    }
}
