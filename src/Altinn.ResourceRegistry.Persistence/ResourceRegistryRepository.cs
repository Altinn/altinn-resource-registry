using System.Data;
using Altinn.AccessGroups.Persistance;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Enums;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Altinn.ResourceRegistry.Persistence
{
    /// <summary>
    /// The repository implementation for postgre database operations on resource registry
    /// </summary>
    public class ResourceRegistryRepository : IResourceRegistryRepository
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;
        private readonly string getResource = "SELECT * FROM resourceregistry.get_resource(@_identifier)";
        private readonly string searchForResource = "SELECT * FROM resourceregistry.search_for_resource(@_id, @_title, @_description, @_resourcetype, @_keyword, @_includeexpired)";
        private readonly string createResource = "SELECT * FROM resourceregistry.create_resource(@_identifier, @_serviceresourcejson)";
        private readonly string updateResource = "SELECT * FROM resourceregistry.update_resource(@_identifier, @_serviceresourcejson)";
        private readonly string deleteResource = "SELECT * FROM resourceregistry.delete_resource(@_identifier)";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceRegistryRepository"/> class
        /// </summary>
        /// <param name="postgresSettings">The configuration settings for the postgresql server connection</param>
        /// <param name="logger">Logger</param>
        public ResourceRegistryRepository(IOptions<PostgreSQLSettings> postgresSettings, ILogger<ResourceRegistryRepository> logger)
        {
            _logger = logger;
            _connectionString = string.Format(
                postgresSettings.Value.ConnectionString,
                postgresSettings.Value.AuthorizationDbPwd);
            NpgsqlConnection.GlobalTypeMapper.MapEnum<ResourceType>("resourceregistry.resourcetype");
        }

        /// <inheritdoc/>
        public async Task<List<ServiceResource>> Search(ResourceSearch resourceSearch)
        {
            try
            {
                await using NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                NpgsqlCommand pgcom = new NpgsqlCommand(searchForResource, conn);
                pgcom.Parameters.AddWithValue("_id", resourceSearch.Id != null ? resourceSearch.Id : DBNull.Value);
                pgcom.Parameters.AddWithValue("_title", resourceSearch.Title != null ? resourceSearch.Title : DBNull.Value);
                pgcom.Parameters.AddWithValue("_description", resourceSearch.Description != null ? resourceSearch.Description : DBNull.Value);
                pgcom.Parameters.AddWithValue("_resourcetype", resourceSearch.ResourceType != null ? resourceSearch.ResourceType.Value.ToString().ToLower() : DBNull.Value);
                pgcom.Parameters.AddWithValue("_keyword", resourceSearch.Keyword != null ? resourceSearch.Keyword : DBNull.Value);
                pgcom.Parameters.AddWithValue("_includeexpired", resourceSearch.IncludeExpired);

                List<ServiceResource> serviceResources = new List<ServiceResource>();

                using NpgsqlDataReader reader = pgcom.ExecuteReader();
                while (reader.Read())
                {
                    serviceResources.Add(GetServiceResource(reader));
                }

                return serviceResources;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Authorization // ResourceRegistryRepository // Search // Exception");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResource> CreateResource(ServiceResource resource)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(resource, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
            try
            {
                await using NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                NpgsqlCommand pgcom = new NpgsqlCommand(createResource, conn);
                pgcom.Parameters.AddWithValue("_identifier", resource.Identifier);
                pgcom.Parameters.AddWithValue("_serviceresourcejson", NpgsqlTypes.NpgsqlDbType.Jsonb, json);

                using NpgsqlDataReader reader = pgcom.ExecuteReader();
                if (reader.Read())
                {
                    return GetServiceResource(reader);
                }

                return null;
            }
            catch (Exception e)
            {
                if (e.Message.Contains("duplicate key value violates unique constraint"))
                {
                    return new ServiceResource();
                }

                _logger.LogError(e, "Authorization // ResourceRegistryRepository // GetResource // Exception");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResource> DeleteResource(string id)
        {
            try
            {
                await using NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                NpgsqlCommand pgcom = new NpgsqlCommand(deleteResource, conn);
                pgcom.Parameters.AddWithValue("_identifier", id);

                using NpgsqlDataReader reader = pgcom.ExecuteReader();
                if (reader.Read())
                {
                    return GetServiceResource(reader);
                }

                return null;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Authorization // ResourceRegistryRepository // DeleteResource // Exception");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResource> GetResource(string id)
        {
            try
            {
                await using NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                NpgsqlCommand pgcom = new NpgsqlCommand(getResource, conn);
                pgcom.Parameters.AddWithValue("_identifier", id);
                
                using NpgsqlDataReader reader = pgcom.ExecuteReader();
                if (reader.Read())
                {
                    return GetServiceResource(reader);
                }

                return null;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Authorization // ResourceRegistryRepository // GetResource // Exception");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResource> UpdateResource(ServiceResource resource)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(resource, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
            try
            {
                await using NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                NpgsqlCommand pgcom = new NpgsqlCommand(updateResource, conn);
                pgcom.Parameters.AddWithValue("_identifier", resource.Identifier);
                pgcom.Parameters.AddWithValue("_serviceresourcejson", NpgsqlTypes.NpgsqlDbType.Jsonb, json);

                using NpgsqlDataReader reader = pgcom.ExecuteReader();
                if (reader.Read())
                {
                    return GetServiceResource(reader);
                }

                return null;
            }
            catch (Exception e)
            {
                if (e.Message.Contains("duplicate key value violates unique constraint"))
                {
                    return new ServiceResource();
                }

                _logger.LogError(e, "Authorization // ResourceRegistryRepository // GetResource // Exception");
                throw;
            }
        }

        private static ServiceResource GetServiceResource(NpgsqlDataReader reader)
        {
            if (reader["serviceresourcejson"] != DBNull.Value)
            {
                var jsonb = reader.GetString("serviceresourcejson");

                ServiceResource? resource = System.Text.Json.JsonSerializer.Deserialize<ServiceResource>(jsonb, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }) as ServiceResource;
                return resource;
            }

            return null;
        }
    }
}