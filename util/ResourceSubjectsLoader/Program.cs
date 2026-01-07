// See https://aka.ms/new-console-template for more information
using Altinn.ResourceRegistry.Core.Enums;
using Altinn.ResourceRegistry.Core.Models;
using ResourceSubjectsLoader.Clients.Altinn.AccessManagement.UI.Integration.Clients;
using ResourceSubjectsLoader.Enums;

Console.WriteLine("Hello, World!");

string at22base = "https://platform.tt02.altinn.no/resourceregistry/api/v1/";

HttpClient _httpClient = new HttpClient();
_httpClient.BaseAddress = new Uri(at22base);    

ResourceRegistryClient resourceRegistry = new ResourceRegistryClient(_httpClient);

 List<ServiceResource> resourceList = await resourceRegistry.GetResourceList();

for (int i = 0; i < resourceList.Count; i++)
{
    if (i % 100 == 0)
    {
        Console.WriteLine($"Index: {i}");
    }

    ServiceResource resource = resourceList[i];

    if (resource.ResourceType.Equals(ResourceType.GenericAccessResource))
    {
        await resourceRegistry.ReloadResourceSubects(resource.Identifier);
    }

    if (resource.ResourceType.Equals(ResourceType.AltinnApp))
    {
        await resourceRegistry.ReloadResourceSubects(resource.Identifier);
    }

    if (resource.ResourceType.Equals(ResourceType.Systemresource))
    {
        await resourceRegistry.ReloadResourceSubects(resource.Identifier);
    }


}
