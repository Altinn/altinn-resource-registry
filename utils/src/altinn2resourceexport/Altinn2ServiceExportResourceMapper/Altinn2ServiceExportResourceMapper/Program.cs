using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Altinn.ResourceRegistry.Core.Models;

namespace Altinn2ServiceExportResourceMapper
{
    public class ConsentResource
    {
        public string OrganizationCode { get; set; }
        public string ServiceCode { get; set; }
        public int ServiceEditionCode { get; set; }
        public int ServiceEditionVersionId { get; set; }
        public int? IsOneTimeConsent { get; set; }
        public string ConsentTemplate { get; set; }
        public int LanguageCode { get; set; }
        public string Text { get; set; }
        public int ResourceType { get; set; }

        public string GetServiceKey()
        {
            return $"{OrganizationCode}_{ServiceCode}_{ServiceEditionCode}_{ServiceEditionVersionId}";
        }

        public string GetResourceKey()
        {
            return $"{OrganizationCode}_{ServiceCode}_{ServiceEditionCode}";
        }

        public string[] GetConsentMetadata()
        {
            if (string.IsNullOrEmpty(Text))
            {
                return new string[0];
            }
            
            var matches = System.Text.RegularExpressions.Regex.Matches(Text, @"\{([^}]+)\}");
            return matches.Cast<System.Text.RegularExpressions.Match>()
                         .Select(m => m.Groups[1].Value)
                         .Distinct()
                         .ToArray();
        }
    }

    public class OrganizationInfo
    {
        public string Name { get; set; }
        public string NameEnglish { get; set; }
        public string NameNynorsk { get; set; }
        public string OrgNumber { get; set; }
        public string Logo { get; set; }
        public string Homepage { get; set; }
        public List<string> Environments { get; set; } = new List<string>();
    }

    public class AltinnOrg
    {
        public Dictionary<string, string> Name { get; set; }
        public string Orgnr { get; set; }
        public string Logo { get; set; }
        public string Homepage { get; set; }
        public List<string> Environments { get; set; }
    }

    public class AltinnOrgList
    {
        public Dictionary<string, AltinnOrg> Orgs { get; set; }
    }

    class Program
    {
        private static Dictionary<string, OrganizationInfo> _organizationLookup = new Dictionary<string, OrganizationInfo>();

        static void Main(string[] args)
        {
            string csvFilePath = @"Data\Samtykke_ressurser.csv";
            
            try
            {
                // Initialize organization lookup from altinn-orgs.json
                InitializeOrganizationLookup();
                
                var resources = ReadCsvFile(csvFilePath);
                
                Console.WriteLine($"Successfully read {resources.Count} consent resources from CSV file.");
                Console.WriteLine();
                
                // Display summary statistics
                DisplaySummaryStatistics(resources);
                
                // Display first few records as examples
                DisplaySampleRecords(resources);
                
                // Demonstrate organization lookup
                DemonstrateOrganizationLookup(resources);

                Dictionary<string, ServiceResource> serviceResources = [];

                foreach (ConsentResource resource in resources)
                {
                    string serviceKey = resource.GetServiceKey();

                    Dictionary<string, string> ConsentText = new Dictionary<string, string>();
                    Dictionary<string, string> Title = new Dictionary<string, string>();

                    if (resource.LanguageCode == 1044) // Norwegian Bokmål
                    {
                        if (resource.ResourceType == 1)
                        {
                            Title["nb"] = resource.Text;
                        }
                        else
                        {
                            ConsentText["nb"] = resource.Text;
                        }
                    }
                    else if (resource.LanguageCode == 1033) // English
                    {
                        if (resource.ResourceType == 1)
                        {
                            Title["en"] = resource.Text;
                        }
                        else
                        {
                            ConsentText["en"] = resource.Text;
                        }
                    }
                    else if (resource.LanguageCode == 2068) // Norwegian Nynorsk
                    {
                        if (resource.ResourceType == 1)
                        {
                            Title["nn"] = resource.Text;
                        }
                        else
                        {
                            ConsentText["nn"] = resource.Text;
                        }
                    }
                 
                    List<ResourceReference> resourceReferences = new List<ResourceReference>();

                    if (!string.IsNullOrEmpty(resource.ServiceCode))
                    {
                        ResourceReference resourceReference = new ResourceReference
                        {
                            ReferenceSource = Altinn.ResourceRegistry.Core.Enums.ReferenceSource.Altinn2,
                            ReferenceType = Altinn.ResourceRegistry.Core.Enums.ReferenceType.ServiceEditionCode,
                            Reference = resource.ServiceCode
                        };
                        resourceReferences.Add(resourceReference);
                    }

                    if (resource.ServiceEditionCode != 0)
                    {
                        ResourceReference resourceReference = new ResourceReference
                        {
                            ReferenceSource = Altinn.ResourceRegistry.Core.Enums.ReferenceSource.Altinn2,
                            ReferenceType = Altinn.ResourceRegistry.Core.Enums.ReferenceType.ServiceCode,
                            Reference = resource.ServiceEditionVersionId.ToString()
                        };
                        resourceReferences.Add(resourceReference);
                    }

                    if(resource.ServiceEditionVersionId != 0)
                    {
                        ResourceReference resourceReference = new ResourceReference
                        {
                            ReferenceSource = Altinn.ResourceRegistry.Core.Enums.ReferenceSource.Altinn2,
                            ReferenceType = Altinn.ResourceRegistry.Core.Enums.ReferenceType.ServiceEditionVersion,
                            Reference = resource.ServiceEditionCode.ToString()
                        };
                        resourceReferences.Add(resourceReference);
                    }

                    Dictionary<string, ConsentMetadata> ConsentMetadata = new Dictionary<string, ConsentMetadata>();

                    string[] metadataKeys = resource.GetConsentMetadata();

                    foreach (string key in metadataKeys)
                    {
                        ConsentMetadata[key] = new ConsentMetadata
                        {
                            Optional = false
                        };
                    }

                    if (!serviceResources.ContainsKey(serviceKey))
                    {
                        serviceResources[serviceKey] = new ServiceResource
                        {
                            HasCompetentAuthority = new CompetentAuthority()
                            {
                                Orgcode = resource.OrganizationCode
                            },

                            ConsentTemplate = resource.ConsentTemplate,
                            ConsentText = ConsentText,
                            ConsentMetadata = ConsentMetadata,
                            ResourceReferences = resourceReferences,
                            Title = Title,
                            Description = Title,
                            Identifier = resource.GetResourceKey(),
                            Version = "1.0",
                            ResourceType = Altinn.ResourceRegistry.Core.Enums.ResourceType.Consent,
                            Status = "Discontinued",
                            IsOneTimeConsent = resource.IsOneTimeConsent == 1 ? true : false,
                            Visible = false,
                            Delegable = false, // Default to false. No need to be able to delegate this migrated consent resource
                            ContactPoints = [new ContactPoint()
                            {
                                Category = "Support",
                                Email = "servicedesk@altinn.no"
                            }]
                         };
                    }
                    else
                    {
                        foreach (var kvp in ConsentText)
                        {
                            serviceResources[serviceKey].ConsentText[kvp.Key] = kvp.Value;
                        }

                        foreach(var kvpTitle in Title)
                        {
                            serviceResources[serviceKey].Title[kvpTitle.Key] = kvpTitle.Value;
                        }

                        foreach (var kvp in ConsentMetadata)
                        {
                            if (!serviceResources[serviceKey].ConsentMetadata.ContainsKey(kvp.Key))
                            {
                                serviceResources[serviceKey].ConsentMetadata[kvp.Key] = kvp.Value;
                            }
                        }

                        foreach (var resRef in resourceReferences)
                        {
                            if (!serviceResources[serviceKey].ResourceReferences.Any(r => r.ReferenceSource == resRef.ReferenceSource &&
                                                                                         r.ReferenceType == resRef.ReferenceType &&
                                                                                         r.Reference == resRef.Reference))
                            {
                                serviceResources[serviceKey].ResourceReferences.Add(resRef);
                            }
                        }
                    }
                }

                // Ensure all languages have text by falling back to Norwegian Bokmål
                foreach (ServiceResource sr in serviceResources.Values)
                {
                    if (sr.ConsentText.ContainsKey("nb") && !sr.ConsentText.ContainsKey("en"))
                    {
                        sr.ConsentText["en"] = sr.ConsentText["nb"];
                    }

                    if (sr.ConsentText.ContainsKey("nb") && !sr.ConsentText.ContainsKey("nn"))
                    {
                        sr.ConsentText["nn"] = sr.ConsentText["nb"];
                    }

                    if (sr.Title.ContainsKey("nb") && !sr.Title.ContainsKey("en"))
                    {
                        sr.Title["en"] = sr.Title["nb"];
                    }

                    if (sr.Title.ContainsKey("nb") && !sr.Title.ContainsKey("nn"))
                    {
                        sr.Title["nn"] = sr.Title["nb"];
                    }

                    sr.HasCompetentAuthority.Organization = GetOrganizationInfo(sr.HasCompetentAuthority.Orgcode).OrgNumber;
                }

                // Save each ServiceResource as JSON file
                SaveServiceResourcesToJson(serviceResources);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading CSV file: {ex.Message}");
            }
        }

        static void InitializeOrganizationLookup()
        {
            string altinnOrgsFilePath = @"Data\altinn-orgs.json";
            
            try
            {
                if (!File.Exists(altinnOrgsFilePath))
                {
                    Console.WriteLine($"Warning: altinn-orgs.json file not found at {altinnOrgsFilePath}");
                    return;
                }

                string jsonContent = File.ReadAllText(altinnOrgsFilePath);
                var altinnOrgList = JsonSerializer.Deserialize<AltinnOrgList>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (altinnOrgList?.Orgs != null)
                {
                    foreach (var kvp in altinnOrgList.Orgs)
                    {
                        string orgCode = kvp.Key.ToLowerInvariant();
                        var org = kvp.Value;
                        
                        _organizationLookup[orgCode] = new OrganizationInfo
                        {
                            Name = org.Name?.GetValueOrDefault("nb", ""),
                            NameEnglish = org.Name?.GetValueOrDefault("en", ""),
                            NameNynorsk = org.Name?.GetValueOrDefault("nn", ""),
                            OrgNumber = org.Orgnr ?? "",
                            Logo = org.Logo ?? "",
                            Homepage = org.Homepage ?? "",
                            Environments = org.Environments ?? new List<string>()
                        };
                    }

                    Console.WriteLine($"Loaded {_organizationLookup.Count} organizations from altinn-orgs.json");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading organization lookup: {ex.Message}");
            }
        }

        static OrganizationInfo GetOrganizationInfo(string orgCode)
        {
            if (string.IsNullOrEmpty(orgCode))
                return null;

            string key = orgCode.ToLowerInvariant();
            return _organizationLookup.GetValueOrDefault(key);
        }

        static string GetOrganizationNumber(string orgCode)
        {
            var orgInfo = GetOrganizationInfo(orgCode);
            return orgInfo?.OrgNumber ?? "";
        }

        static string GetOrganizationName(string orgCode, string language = "nb")
        {
            var orgInfo = GetOrganizationInfo(orgCode);
            if (orgInfo == null) return "";

            return language.ToLowerInvariant() switch
            {
                "en" => orgInfo.NameEnglish ?? orgInfo.Name,
                "nn" => orgInfo.NameNynorsk ?? orgInfo.Name,
                _ => orgInfo.Name
            };
        }

        static void DemonstrateOrganizationLookup(List<ConsentResource> resources)
        {
            Console.WriteLine("=== Organization Lookup Demo ===");
            
            var uniqueOrgCodes = resources.Select(r => r.OrganizationCode).Distinct().Take(5);
            
            foreach (string orgCode in uniqueOrgCodes)
            {
                var orgInfo = GetOrganizationInfo(orgCode);
                if (orgInfo != null)
                {
                    Console.WriteLine($"Org Code: {orgCode.ToUpperInvariant()}");
                    Console.WriteLine($"  Name (NB): {orgInfo.Name}");
                    Console.WriteLine($"  Name (EN): {orgInfo.NameEnglish}");
                    Console.WriteLine($"  Org Number: {orgInfo.OrgNumber}");
                    Console.WriteLine($"  Homepage: {orgInfo.Homepage}");
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine($"Org Code: {orgCode.ToUpperInvariant()} - No information found");
                    Console.WriteLine();
                }
            }
        }

        static List<ConsentResource> ReadCsvFile(string filePath)
        {
            var resources = new List<ConsentResource>();
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"CSV file not found: {filePath}");
            }
            
            var lines = File.ReadAllLines(filePath);
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                    
                var resource = ParseCsvLine(line);
                if (resource != null)
                {
                    resources.Add(resource);
                }
            }
            
            return resources;
        }

        static ConsentResource ParseCsvLine(string line)
        {
            try
            {
                var fields = ParseCsvFields(line);
                
                if (fields.Length < 9)
                {
                    Console.WriteLine($"Warning: Invalid line format (expected 9 fields, got {fields.Length}): {line.Substring(0, Math.Min(50, line.Length))}...");
                    return null;
                }

                return new ConsentResource
                {
                    OrganizationCode = fields[0],
                    ServiceCode = fields[1],
                    ServiceEditionCode = int.Parse(fields[2]),
                    ServiceEditionVersionId = int.Parse(fields[3]),
                    IsOneTimeConsent = fields[4] == "NULL" ? null : int.Parse(fields[4]),
                    ConsentTemplate = fields[5] == "NULL" ? null : fields[5],
                    LanguageCode = int.Parse(fields[6]),
                    Text = fields[7],
                    ResourceType = int.Parse(fields[8])
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing line: {ex.Message}");
                Console.WriteLine($"Line: {line.Substring(0, Math.Min(100, line.Length))}...");
                return null;
            }
        }

        static string[] ParseCsvFields(string line)
        {
            var fields = new List<string>();
            var currentField = "";
            bool inQuotes = false;
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Escaped quote
                        currentField += '"';
                        i++; // Skip next quote
                    }
                    else
                    {
                        // Toggle quote state
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    // Field separator
                    fields.Add(currentField);
                    currentField = "";
                }
                else
                {
                    currentField += c;
                }
            }
            
            // Add the last field
            fields.Add(currentField);
            
            return fields.ToArray();
        }

        static void DisplaySummaryStatistics(List<ConsentResource> resources)
        {
            Console.WriteLine("=== Summary Statistics ===");
            Console.WriteLine($"Total records: {resources.Count}");
            
            var organizationGroups = resources.GroupBy(r => r.OrganizationCode).OrderBy(g => g.Key);
            Console.WriteLine($"Organizations: {organizationGroups.Count()}");
            foreach (var org in organizationGroups)
            {
                string orgName = GetOrganizationName(org.Key);
                string displayName = !string.IsNullOrEmpty(orgName) ? $"{org.Key.ToUpperInvariant()} ({orgName})" : org.Key.ToUpperInvariant();
                Console.WriteLine($"  {displayName}: {org.Count()} records");
            }
            
            var languageGroups = resources.GroupBy(r => r.LanguageCode).OrderBy(g => g.Key);
            Console.WriteLine($"Languages:");
            foreach (var lang in languageGroups)
            {
                string langName = lang.Key switch
                {
                    1044 => "Norwegian (Bokmål)",
                    1033 => "English", 
                    2068 => "Norwegian (Nynorsk)",
                    _ => "Unknown"
                };
                Console.WriteLine($"  {lang.Key} ({langName}): {lang.Count()} records");
            }
            
            var resourceTypeGroups = resources.GroupBy(r => r.ResourceType).OrderBy(g => g.Key);
            Console.WriteLine($"Resource types:");
            foreach (var type in resourceTypeGroups)
            {
                string typeName = type.Key switch
                {
                    1 => "Title",
                    18 => "Description",
                    _ => "Unknown"
                };
                Console.WriteLine($"  {type.Key} ({typeName}): {type.Count()} records");
            }
            
            Console.WriteLine();
        }

        static void DisplaySampleRecords(List<ConsentResource> resources)
        {
            Console.WriteLine("=== Sample Records ===");
            
            var sampleRecords = resources.Take(5);
            
            foreach (var resource in sampleRecords)
            {
                Console.WriteLine($"Organization: {resource.OrganizationCode}");
                Console.WriteLine($"Service: {resource.ServiceCode}/{resource.ServiceEditionCode}");
                Console.WriteLine($"External Service: {resource.ServiceEditionVersionId}/{resource.IsOneTimeConsent}");
                Console.WriteLine($"Form Task: {resource.ConsentTemplate ?? "NULL"}");
                Console.WriteLine($"Language: {resource.LanguageCode}");
                Console.WriteLine($"Resource Type: {resource.ResourceType} ({(resource.ResourceType == 1 ? "Title" : "Description")})");
                Console.WriteLine($"Text: {resource.Text.Substring(0, Math.Min(100, resource.Text.Length))}...");
                Console.WriteLine(new string('-', 50));
            }
        }

        static void SaveServiceResourcesToJson(Dictionary<string, ServiceResource> serviceResources)
        {
            string outputDirectory = "Data/Resources";
            
            // Create directory if it doesn't exist
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            foreach (var kvp in serviceResources)
            {
                string serviceKey = kvp.Key;
                ServiceResource serviceResource = kvp.Value;
                
                string fileName = $"{serviceKey}.json";
                string filePath = Path.Combine(outputDirectory, fileName);
                
                try
                {
                    string json = JsonSerializer.Serialize(serviceResource, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    
                    File.WriteAllText(filePath, json);
                    Console.WriteLine($"Saved service resource: {fileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving {fileName}: {ex.Message}");
                }
            }
            
            Console.WriteLine($"\nSaved {serviceResources.Count} service resources to {outputDirectory}");
        }
    }
}
