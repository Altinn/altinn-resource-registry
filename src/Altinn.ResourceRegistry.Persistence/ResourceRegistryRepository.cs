using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Resources;
using System.Text.Json;
using System.Xml.Linq;
using Altinn.Platform.Storage.Interface.Models;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Persistence.CustomTypes;
using Altinn.ResourceRegistry.Persistence.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;
using Npgsql.Internal.Postgres;
using NpgsqlTypes;

namespace Altinn.ResourceRegistry.Persistence;

/// <summary>
/// The repository implementation for postgre database operations on resource registry
/// </summary>
internal class ResourceRegistryRepository : IResourceRegistryRepository
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly NpgsqlDataSource _conn;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceRegistryRepository"/> class
    /// </summary>
    /// <param name="conn">The database connection</param>
    /// <param name="logger">Logger</param>
    public ResourceRegistryRepository(
        NpgsqlDataSource conn,
        ILogger<ResourceRegistryRepository> logger)
    {
        _conn = conn;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<ServiceResource>> Search(
        ResourceSearch resourceSearch,
        CancellationToken cancellationToken = default)
    {
        const string QUERY = /*strpsql*/@"
            SELECT identifier, created, modified, serviceresourcejson
            FROM resourceregistry.resources
            WHERE (@id IS NULL OR serviceresourcejson ->> 'identifier' ILIKE concat('%', @id, '%'))
            AND (@title IS NULL OR serviceresourcejson ->> 'title' ILIKE concat('%', @title, '%'))
            AND (@description IS NULL OR serviceresourcejson ->> 'description' ILIKE concat('%', @description, '%'))
            AND (@resourcetype IS NULL OR serviceresourcejson ->> 'resourceType' ILIKE @resourcetype::text)
            AND (@keyword IS NULL OR serviceresourcejson ->> 'keywords' ILIKE concat('%', @keyword, '%'))
            ";

        try
        {
            await using var pgcom = _conn.CreateCommand(QUERY);
            pgcom.Parameters.AddWithNullableValue("id", NpgsqlDbType.Text, resourceSearch.Id);
            pgcom.Parameters.AddWithNullableValue("title", NpgsqlDbType.Text, resourceSearch.Title);
            pgcom.Parameters.AddWithNullableValue("description", NpgsqlDbType.Text, resourceSearch.Description);
            pgcom.Parameters.AddWithNullableValue("resourcetype", NpgsqlDbType.Text, resourceSearch.ResourceType?.ToString());
            pgcom.Parameters.AddWithNullableValue("keyword", NpgsqlDbType.Text, resourceSearch.Keyword);

            return await pgcom.ExecuteEnumerableAsync(cancellationToken)
                .SelectAwait(GetServiceResource)
                .ToListAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Authorization // ResourceRegistryRepository // Search // Exception");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResource> CreateResource(ServiceResource resource, CancellationToken cancellationToken = default)
    {
        const string QUERY = /*strpsql*/@"
            INSERT INTO resourceregistry.resources(
	            identifier,
	            created,
	            modified,
	            serviceresourcejson
            )
            VALUES (
	            @identifier,
	            Now(),
	            Now(),
	            @serviceresourcejson
            )
            RETURNING identifier, created, modified, serviceresourcejson
            ";

        var json = JsonSerializer.SerializeToDocument(resource, JsonSerializerOptions);
        try
        {
            await using var pgcom = _conn.CreateCommand(QUERY);
            pgcom.Parameters.AddWithValue("identifier", NpgsqlDbType.Text, resource.Identifier);
            pgcom.Parameters.AddWithValue("serviceresourcejson", NpgsqlDbType.Jsonb, json);

            var serviceResource = await pgcom.ExecuteEnumerableAsync(cancellationToken)
                .SelectAwait(GetServiceResource)
                .FirstOrDefaultAsync(cancellationToken);

            return serviceResource ?? throw new SqlNullValueException("No result from database");
        }
        catch (Exception e)
        {
            if (!e.Message.Contains("duplicate key value violates unique constraint"))
            {
                _logger.LogError(e, "Authorization // ResourceRegistryRepository // GetResource // Exception");
            }

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResource?> DeleteResource(string id, CancellationToken cancellationToken = default)
    {
        const string QUERY = /*strpsql*/@"
            DELETE FROM resourceregistry.resources
            WHERE identifier = @identifier
            RETURNING identifier, created, modified, serviceresourcejson    
            ";

        try
        {
            await using var pgcom = _conn.CreateCommand(QUERY);
            pgcom.Parameters.AddWithValue("identifier", NpgsqlDbType.Text, id);

            var serviceResource = await pgcom.ExecuteEnumerableAsync(cancellationToken)
                .SelectAwait(GetServiceResource)
                .SingleOrDefaultAsync(cancellationToken);

            return serviceResource;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Authorization // ResourceRegistryRepository // DeleteResource // Exception");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResource?> GetResource(string id, CancellationToken cancellationToken = default)
    {
        const string QUERY = /*strpsql*/@"
            SELECT identifier, created, modified, serviceresourcejson
            FROM resourceregistry.resources
            WHERE identifier = @identifier   
            ";

        try
        {
            await using var pgcom = _conn.CreateCommand(QUERY);
            pgcom.Parameters.AddWithValue("identifier", NpgsqlDbType.Text, id);

            var serviceResource = await pgcom.ExecuteEnumerableAsync(cancellationToken)
                .SelectAwait(GetServiceResource)
                .SingleOrDefaultAsync(cancellationToken);

            return serviceResource;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Authorization // ResourceRegistryRepository // GetResource // Exception");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResource> UpdateResource(ServiceResource resource, CancellationToken cancellationToken = default)
    {
        const string QUERY = /*strpsql*/@"
            UPDATE resourceregistry.resources
            SET
	            modified = Now(),
	            serviceresourcejson = @serviceresourcejson
            WHERE identifier = @identifier
            RETURNING identifier, created, modified, serviceresourcejson
            ";
        var json = JsonSerializer.SerializeToDocument(resource, JsonSerializerOptions);
        try
        {
            await using var pgcom = _conn.CreateCommand(QUERY);
            pgcom.Parameters.AddWithValue("identifier", NpgsqlDbType.Text, resource.Identifier);
            pgcom.Parameters.AddWithValue("serviceresourcejson", NpgsqlDbType.Jsonb, json);

            var serviceResource = await pgcom.ExecuteEnumerableAsync(cancellationToken)
                .SelectAwait(GetServiceResource)
                .FirstOrDefaultAsync(cancellationToken);

            return serviceResource ?? throw new SqlNullValueException("No result from database");
        }
        catch (Exception e)
        {
            if (!e.Message.Contains("duplicate key value violates unique constraint"))
            {
                _logger.LogError(e, "Authorization // ResourceRegistryRepository // GetResource // Exception");
            }

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<SubjectResources>> FindResourcesForSubjects(List<string> subjects, CancellationToken cancellationToken = default)
    {
        string findResourcesSQL = "select * from resourceregistry.resourcesubjects WHERE subject_urn = ANY(:subjects)";

        await using NpgsqlCommand pgcom = _conn.CreateCommand(findResourcesSQL);
        pgcom.Parameters.AddWithValue("subjects", subjects.ToArray());
        await using NpgsqlDataReader reader = await pgcom.ExecuteReaderAsync(cancellationToken);

        List<SubjectResources> subjectResources = new List<SubjectResources>();

        while (await reader.ReadAsync())
        {
            SubjectResources resourceSubjects = new SubjectResources();
            resourceSubjects.Resources = new List<AttributeMatchV2>();
            resourceSubjects.Subject = new AttributeMatchV2();

            AttributeMatchV2 resourceAttributeMatch = new AttributeMatchV2();

            resourceAttributeMatch.Type = reader.GetFieldValue<string>("resource_type");
            resourceAttributeMatch.Value = reader.GetFieldValue<string>("resource_value");
            resourceAttributeMatch.Urn = reader.GetFieldValue<string>("resource_urn");
            resourceSubjects.Resources.Add(resourceAttributeMatch);

            resourceSubjects.Subject.Type = reader.GetFieldValue<string>("subject_type");
            resourceSubjects.Subject.Value = reader.GetFieldValue<string>("subject_value");
            resourceSubjects.Subject.Urn = reader.GetFieldValue<string>("subject_urn");
            subjectResources.Add(resourceSubjects);
        }

        return subjectResources;
    }

    /// <inheritdoc/>
    public async Task<List<ResourceSubjects>> FindSubjectsForResources(List<string> resources, CancellationToken cancellationToken = default)
    {
        string findResourcesSQL = "select * from resourceregistry.resourcesubjects WHERE resource_urn = ANY(:resources)";

        await using NpgsqlCommand pgcom = _conn.CreateCommand(findResourcesSQL);
        pgcom.Parameters.AddWithValue("resources", resources.ToArray());
        await using NpgsqlDataReader reader = await pgcom.ExecuteReaderAsync(cancellationToken);

        List<ResourceSubjects> subjectResources = new List<ResourceSubjects>();

        while (await reader.ReadAsync())
        {
            ResourceSubjects resourceSubjects = new ResourceSubjects();
            resourceSubjects.Subjects = new List<AttributeMatchV2>();
            resourceSubjects.Resource = new AttributeMatchV2();

            AttributeMatchV2 subjectAttributeMatch = new AttributeMatchV2();

            resourceSubjects.Resource.Type = reader.GetFieldValue<string>("resource_type");
            resourceSubjects.Resource.Value = reader.GetFieldValue<string>("resource_value");
            resourceSubjects.Resource.Urn = reader.GetFieldValue<string>("resource_urn");

            subjectAttributeMatch.Type = reader.GetFieldValue<string>("subject_type");
            subjectAttributeMatch.Value = reader.GetFieldValue<string>("subject_value");
            subjectAttributeMatch.Urn = reader.GetFieldValue<string>("subject_urn");
            resourceSubjects.Subjects.Add(subjectAttributeMatch);

            subjectResources.Add(resourceSubjects);
        }

        return subjectResources;
    }

    private static async ValueTask<ServiceResource> GetServiceResource(NpgsqlDataReader reader)
    {
        var json = await reader.GetFieldValueAsync<JsonDocument>("serviceresourcejson");

        return json.Deserialize<ServiceResource>(JsonSerializerOptions) ??
            throw new SqlNullValueException("Got null when trying to parse ServiceResource");
    }

    private AttributeTypeValue[] MapToAttributeType(List<SubjectAttribute> subjectAttributes)
    {
        AttributeTypeValue[] attributeTypeValues = new AttributeTypeValue[subjectAttributes.Count];
        for (int i = 0; i < subjectAttributes.Count; i++)
        {
            attributeTypeValues[i] = new AttributeTypeValue() { Type = subjectAttributes[i].Type, Value = subjectAttributes[i].Value };
        }

        return attributeTypeValues;
    }

    private AttributeTypeValue[] MapToAttributeType(List<ResourceAttribute> resourceAttributes)
    {
        AttributeTypeValue[] attributeTypeValues = new AttributeTypeValue[resourceAttributes.Count];
        for (int i = 0; i < resourceAttributes.Count; i++)
        {
            attributeTypeValues[i] = new AttributeTypeValue() { Type = resourceAttributes[i].Type, Value = resourceAttributes[i].Value };
        }

        return attributeTypeValues;
    }
}
