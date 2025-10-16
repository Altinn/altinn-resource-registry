using System.Text.Json;
using Altinn.ResourceRegistry.Core.Clients.Interfaces;
using Altinn.ResourceRegistry.Core.Extensions;
using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Tests.Mocks
{
    public class ConsentTemplatesClientMock : IConsentTemplatesClient
    {
        public async Task<List<ConsentTemplate>> GetConsentTemplates(CancellationToken cancellationToken)
        {
            return await GetConsentTemplatesTestData(cancellationToken);
        }

        private static async Task<List<ConsentTemplate>> GetConsentTemplatesTestData(CancellationToken cancellationToken)
        {
            string? unitTestFolder = Path.GetDirectoryName(new Uri(typeof(ConsentTemplatesClientMock).Assembly.Location).LocalPath);
            if (unitTestFolder != null)
            {
                var templatesPath = Path.Combine(unitTestFolder, "..", "..", "..", "Data", "ConsentTemplates", "consent_templates.json");
                templatesPath.AsFilePath(true);
                if (File.Exists(templatesPath))
                {
                    string content = await File.ReadAllTextAsync(templatesPath, cancellationToken);
                  
                    List<ConsentTemplate>? templates = JsonSerializer.Deserialize<List<ConsentTemplate>>(content, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                    return templates ?? [];
                }
            }

            return [];
        }
    }
}