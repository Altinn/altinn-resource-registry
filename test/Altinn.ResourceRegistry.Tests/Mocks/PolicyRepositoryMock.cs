using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Altinn.ResourceRegistry.Core;
using Azure;
using Azure.Storage.Blobs.Models;
using Moq;

namespace Altinn.ResourceRegistry.Tests.Mocks
{
    public class PolicyRepositoryMock : IPolicyRepository
    {
        public Task<Response> DeletePolicyVersionAsync(string filepath, string version, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<Stream> GetPolicyAsync(string resourceId, CancellationToken cancellationToken)
        {
            resourceId = Path.Combine(GetPolicyContainerPath(), resourceId, "resourcepolicy.xml");
            if (File.Exists(resourceId))
            {
                return new FileStream(resourceId, FileMode.Open, FileAccess.Read, FileShare.Read); 
            }

            return null;
        }

        public async Task<Stream> GetAppPolicyAsync(string org, string app, CancellationToken cancellationToken = default)
        {
            string policyFile = Path.Combine(GetAppPolicyContainerPath(), org, app, "policy.xml");
            if (File.Exists(policyFile))
            {
                return new FileStream(policyFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            }

            return null;
        }

        public Task<Stream> GetPolicyVersionAsync(string filepath, string version, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> PolicyExistsAsync(string filepath, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task ReleaseBlobLease(string filepath, string leaseId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> TryAcquireBlobLease(string filepath, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Response<BlobContentInfo>> WritePolicyAsync(string filepath, Stream fileStream, CancellationToken cancellationToken)
        {
            BlobContentInfo mockedBlobInfo = BlobsModelFactory.BlobContentInfo(new ETag("ETagSuccess"), DateTime.Now, new byte[1], DateTime.Now.ToUniversalTime().ToString(), "encryptionKeySha256", "encryptionScope", 1);
            Mock<Response<BlobContentInfo>> mockResponse = new Mock<Response<BlobContentInfo>>();
            mockResponse.SetupGet(r => r.Value).Returns(mockedBlobInfo);

            Mock<Response> responseMock = new Mock<Response>();
            responseMock.SetupGet(r => r.Status).Returns((int)HttpStatusCode.Created);
            mockResponse.Setup(r => r.GetRawResponse()).Returns(responseMock.Object);

            return Task.FromResult(mockResponse.Object);
        }

        public Task<Response<BlobContentInfo>> WritePolicyConditionallyAsync(string filepath, Stream fileStream, string blobLeaseId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private static string GetPolicyContainerPath()
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(PolicyRepositoryMock).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "ResourcePolicies");
        }

        private static string GetAppPolicyContainerPath()
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(PolicyRepositoryMock).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "AppPolicies");
        }
    }
}
