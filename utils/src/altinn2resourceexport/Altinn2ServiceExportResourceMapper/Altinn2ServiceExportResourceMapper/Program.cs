using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Altinn2ServiceExportResourceMapper
{
    public class ConsentResource
    {
        public string OrganizationCode { get; set; }
        public int ServiceCode { get; set; }
        public int ServiceEditionCode { get; set; }
        public int ExternalServiceCode { get; set; }
        public int? ExternalServiceEditionCode { get; set; }
        public string FormTask { get; set; }
        public int LanguageCode { get; set; }
        public string Text { get; set; }
        public int ResourceType { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string csvFilePath = @"Data\Samtykke_ressurser.csv";
            
            try
            {
                var resources = ReadCsvFile(csvFilePath);
                
                Console.WriteLine($"Successfully read {resources.Count} consent resources from CSV file.");
                Console.WriteLine();
                
                // Display summary statistics
                DisplaySummaryStatistics(resources);
                
                // Display first few records as examples
                DisplaySampleRecords(resources);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading CSV file: {ex.Message}");
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
                    ServiceCode = int.Parse(fields[1]),
                    ServiceEditionCode = int.Parse(fields[2]),
                    ExternalServiceCode = int.Parse(fields[3]),
                    ExternalServiceEditionCode = fields[4] == "NULL" ? null : int.Parse(fields[4]),
                    FormTask = fields[5] == "NULL" ? null : fields[5],
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
                Console.WriteLine($"  {org.Key}: {org.Count()} records");
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
                Console.WriteLine($"External Service: {resource.ExternalServiceCode}/{resource.ExternalServiceEditionCode}");
                Console.WriteLine($"Form Task: {resource.FormTask ?? "NULL"}");
                Console.WriteLine($"Language: {resource.LanguageCode}");
                Console.WriteLine($"Resource Type: {resource.ResourceType} ({(resource.ResourceType == 1 ? "Title" : "Description")})");
                Console.WriteLine($"Text: {resource.Text.Substring(0, Math.Min(100, resource.Text.Length))}...");
                Console.WriteLine(new string('-', 50));
            }
        }
    }
}
