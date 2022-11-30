using System.Diagnostics.CodeAnalysis;
using System.Net;
using Altinn.AccessGroups.Persistance;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Extensions;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.ResourceRegistry.Persistence
{
    /// <summary>
    /// Repository for handling policy files
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class PolicyRepository : IPolicyRepository
    {
        private readonly ILogger<PolicyRepository> _logger;
        private readonly AzureStorageConfiguration _storageConfig;
        private readonly BlobContainerClient _resourceRegisterContainerClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="PolicyRepository"/> class
        /// </summary>
        /// <param name="storageConfig">The storage configuration for Azure Blob Storage.</param>
        /// <param name="logger">logger</param>
        public PolicyRepository(
            IOptions<AzureStorageConfiguration> storageConfig,
            ILogger<PolicyRepository> logger)
        {
            _logger = logger;
            _storageConfig = storageConfig.Value;

            StorageSharedKeyCredential resourceRegisterCredentials = new StorageSharedKeyCredential(_storageConfig.ResourceRegistryAccountName, _storageConfig.ResourceRegistryAccountKey);
            BlobServiceClient resourceRegisterServiceClient = new BlobServiceClient(new Uri(_storageConfig.ResourceRegistryBlobEndpoint), resourceRegisterCredentials);
            _resourceRegisterContainerClient = resourceRegisterServiceClient.GetBlobContainerClient(_storageConfig.ResourceRegistryContainer);
        }

        /// <inheritdoc/>
        public async Task<Stream> GetPolicyAsync(string resourceId)
        {
            string filePath = $"{resourceId.AsFilePath()}/resourcepolicy.xml";
            BlobClient blobClient = CreateBlobClient(filePath);

            return await GetBlobStreamInternal(blobClient);
        }

        /// <inheritdoc/>
        public async Task<Stream> GetPolicyVersionAsync(string resourceId, string version)
        {
            string filePath = $"{resourceId.AsFilePath()}/resourcepolicy.xml";
            BlobClient blobClient = CreateBlobClient(filePath).WithVersion(version);

            return await GetBlobStreamInternal(blobClient);
        }

        /// <inheritdoc/>
        public async Task<Response<BlobContentInfo>> WritePolicyAsync(string resourceId, Stream fileStream)
        {
            string filePath = $"{resourceId.AsFilePath()}/resourcepolicy.xml";
            BlobClient blobClient = CreateBlobClient(filePath);

            return await WriteBlobStreamInternal(blobClient, fileStream);
        }

        /// <inheritdoc/>
        public async Task<Response<BlobContentInfo>> WritePolicyConditionallyAsync(string resourceId, Stream fileStream, string blobLeaseId)
        {
            string filePath = $"{resourceId.AsFilePath()}/resourcepolicy.xml";
            BlobClient blobClient = CreateBlobClient(filePath);

            BlobUploadOptions blobUploadOptions = new BlobUploadOptions()
            {
                Conditions = new BlobRequestConditions()
                {
                    LeaseId = blobLeaseId
                }
            };

            return await WriteBlobStreamInternal(blobClient, fileStream, blobUploadOptions);
        }

        /// <inheritdoc/>
        public async Task<string> TryAcquireBlobLease(string resourceId)
        {
            string filePath = $"{resourceId.AsFilePath()}/resourcepolicy.xml";
            BlobClient blobClient = CreateBlobClient(resourceId);
            BlobLeaseClient blobLeaseClient = blobClient.GetBlobLeaseClient();

            try
            {
                BlobLease blobLease = await blobLeaseClient.AcquireAsync(TimeSpan.FromSeconds(_storageConfig.BlobLeaseTimeout));
                return blobLease.LeaseId;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Failed to acquire blob lease for policy file at {filepath}. RequestFailedException", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to acquire blob lease for policy file at {filepath}. Unexpected error", filePath);
            }

            return null;
        }

        /// <inheritdoc/>
        public async void ReleaseBlobLease(string resourceId, string leaseId)
        {
            string filePath = $"{resourceId.AsFilePath()}/resourcepolicy.xml";
            BlobClient blobClient = CreateBlobClient(filePath);
            BlobLeaseClient blobLeaseClient = blobClient.GetBlobLeaseClient(leaseId);
            await blobLeaseClient.ReleaseAsync();
        }

        /// <inheritdoc/>
        public async Task<bool> PolicyExistsAsync(string resourceId)
        {
            string filePath = $"{resourceId.AsFilePath()}/resourcepolicy.xml";
            try
            {
                BlobClient blobClient = CreateBlobClient(filePath);
                return await blobClient.ExistsAsync();
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Failed to check if blob exists for policy file at {filepath}. RequestFailedException", filePath);
            }

            return false;
        }

        /// <inheritdoc/>
        public async Task<Response> DeletePolicyVersionAsync(string resourceId, string version)
        {
            string filePath = $"{resourceId.AsFilePath()}/resourcepolicy.xml";
            try
            {
                BlobClient blockBlob = CreateBlobClient(filePath);

                return await blockBlob.WithVersion(version).DeleteAsync();
            }
            catch (RequestFailedException ex)
            {
                if (ex.Status == (int)HttpStatusCode.Forbidden && ex.ErrorCode == "OperationNotAllowedOnRootBlob")
                {
                    _logger.LogError(ex, "Failed to delete version {version} of policy file at {filepath}. Not allowed to delete current version.", version, filePath);
                    throw;
                }

                _logger.LogError(ex, "Failed to delete version {version} of policy file at {filepath}. RequestFailedException", version, filePath);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete version {version} of policy file at {filepath}. Unexpected error", version, filePath);
                throw;
            }
        }

        private BlobClient CreateBlobClient(string blobName)
        {
            return _resourceRegisterContainerClient.GetBlobClient(blobName);
        }

        private async Task<Stream> GetBlobStreamInternal(BlobClient blobClient)
        {
            try
            {
                Stream memoryStream = new MemoryStream();

                if (await blobClient.ExistsAsync())
                {
                    await blobClient.DownloadToAsync(memoryStream);
                    memoryStream.Position = 0;

                    return memoryStream;
                }

                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read policy file at {blobClient.Name}.", blobClient.Name);
                throw;
            }
        }

        private async Task<Response<BlobContentInfo>> WriteBlobStreamInternal(BlobClient blobClient, Stream fileStream, BlobUploadOptions blobUploadOptions = null)
        {
            try
            {
                if (blobUploadOptions != null)
                {
                    return await blobClient.UploadAsync(fileStream, blobUploadOptions);
                }

                return await blobClient.UploadAsync(fileStream, true);
            }
            catch (RequestFailedException ex)
            {
                if (ex.Status == (int)HttpStatusCode.PreconditionFailed)
                {
                    _logger.LogError(ex, "Failed to save policy file {blobClient.Name}. Precondition failed", blobClient.Name);
                    throw;
                }

                _logger.LogError(ex, "Failed to save policy file {blobClient.Name}. RequestFailedException", blobClient.Name);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save policy file {blobClient.Name}. Unexpected exception", blobClient.Name);
                throw;
            }
        }
    }
}
