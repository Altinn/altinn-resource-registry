using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Tests.Mocks
{
    public class RegisterResourceRepositoryMock : IResourceRegistryRepository
    {
        public async Task<ServiceResource> CreateResource(ServiceResource resource, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult<ServiceResource>(null);
        }

        public async Task<ServiceResource> UpdateResource(ServiceResource resource, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult<ServiceResource>(resource);
        }

        public async Task<ServiceResource> DeleteResource(string id, CancellationToken cancellationToken = default)
        {
            return await GetResource(id);
        }

        public async Task<ServiceResource> GetResource(string id, CancellationToken cancellationToken = default)
        {
            string resourcePath = GetResourcePath(id);
            if (File.Exists(resourcePath))
            {
                try
                {
                    string content = System.IO.File.ReadAllText(resourcePath);
                    ServiceResource? resource = System.Text.Json.JsonSerializer.Deserialize<ServiceResource>(content, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }) as ServiceResource;
                    return resource;
                }
                catch(Exception ex)
                {
                    throw;
                }
            }

            return null;
        }

        public async Task<List<ServiceResource>> Search(ResourceSearch resourceSearch, CancellationToken cancellationToken = default)
        {
            List<ServiceResource> resources = new List<ServiceResource>();
            string[] files =  Directory.GetFiles(GetResourcePath());
            if(files != null)
            {
                foreach (string file in files)
                {
                    string content = System.IO.File.ReadAllText(file);
                    ServiceResource? resource = System.Text.Json.JsonSerializer.Deserialize<ServiceResource>(content, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }) as ServiceResource;
                    resources.Add(resource);
                }
            }

            return resources;
        }

        private static string GetResourcePath(string id)
        {
            return Path.Combine(GetResourcePath(), id + ".json");
        }

        private static string GetResourcePath()
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(RegisterResourceRepositoryMock).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "Resources");
        }

        public Task<List<SubjectResources>> FindResourcesForSubjects(List<string> subjects, CancellationToken cancellationToken = default)
        {
           List<SubjectResources> resources = new List<SubjectResources>();
          resources.Add(GetSubjectResource("urn:altinn:rolecode:utinn", new List<string>{ "urn:altinn:resource:skd_mva", "urn:altinn:resource:skd_ax" }));
           return Task.FromResult(resources);
        }

        /// <inheritdoc/>
        public Task<List<ResourceSubjects>> FindSubjectsForResources(List<string> resources, CancellationToken cancellationToken = default)
        {
            List<ResourceSubjects> resourceSubjects = new List<ResourceSubjects>();
            resourceSubjects.Add(GetResourceSubjects("urn:altinn:resource:skd_mva", new List<string> { "urn:altinn:rolecode:utinn", "urn:altinn:rolecode:dagl" }));
            return Task.FromResult(resourceSubjects);
        }


        private SubjectResources GetSubjectResource(string subjectUrn, List<string> resources)
        {
            SubjectResources subjectResources = new SubjectResources();
            subjectResources.Subject = new AttributeMatchV2() { Urn = subjectUrn, Type = subjectUrn.Substring(0, subjectUrn.LastIndexOf(":")), Value = subjectUrn.Substring(subjectUrn.LastIndexOf(":")+1) };
            subjectResources.Resources = new List<AttributeMatchV2>();
            foreach(string resource in resources)
            {
                subjectResources.Resources.Add(new AttributeMatchV2() 
                { 
                    Urn = resource, 
                    Value = resource.Substring(resource.LastIndexOf(":")+1), 
                    Type = resource.Substring(0, resource.LastIndexOf(":")) 
                });    
            }
            return subjectResources;
        }

        private ResourceSubjects GetResourceSubjects(string resourceUrn, List<string> subjects)
        {
            ResourceSubjects subjectResources = new ResourceSubjects();
            subjectResources.Resource = new AttributeMatchV2() { Urn = resourceUrn, Type = resourceUrn.Substring(0, resourceUrn.LastIndexOf(":")), Value = resourceUrn.Substring(resourceUrn.LastIndexOf(":")+1) };
            subjectResources.Subjects = new List<AttributeMatchV2>();
            foreach (string subject in subjects)
            {
                subjectResources.Subjects.Add(new AttributeMatchV2()
                {
                    Urn = subject,
                    Value = subject.Substring(subject.LastIndexOf(":")+1),
                    Type = subject.Substring(0, subject.LastIndexOf(":"))
                });
            }
            return subjectResources;
        }

        public Task SetResourceSubjects(ResourceSubjects resourceSubjects, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
