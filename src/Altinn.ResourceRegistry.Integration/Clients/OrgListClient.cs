﻿using Altinn.ResourceRegistry.Core.Configuration;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Altinn.ResourceRegistry.Integration.Clients
{
    /// <summary>
    /// Client responsible for collection 
    /// </summary>
    public class OrgListClient : IOrgListClient
    {
        private readonly HttpClient _client;
        private readonly ResourceRegistrySettings _settings;
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// Default constroctur
        /// </summary>
        public OrgListClient(HttpClient client, IOptions<ResourceRegistrySettings> resourceRegistrySettings, IMemoryCache memoryCache)
        {
            _client = client;
            _settings = resourceRegistrySettings.Value;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Returns configured org list
        /// </summary>
        public async Task<OrgList> GetOrgList()
        {
            OrgList orgList;

            try
            {
                HttpResponseMessage response = await _client.GetAsync(_settings.OrgListEndpoint);
                if (response.IsSuccessStatusCode)
                {
                    response = await _client.GetAsync(_settings.OrgListAlternativeEndpoint);
                }

                string orgListString = await response.Content.ReadAsStringAsync();
                orgList = System.Text.Json.JsonSerializer.Deserialize<OrgList>(orgListString, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

                return orgList;
            }
            catch (Exception ex)
            {
                throw new Exception($"Something went wrong when retrieving Action options", ex);
            }
        }
    }
}
