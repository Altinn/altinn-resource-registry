using System.Data;
using System.Data.SqlTypes;
using System.Text.Json;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Persistence.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;
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

        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(resource.Identifier);
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

        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(resource.Identifier);
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
    public async Task<List<SubjectResources>> FindResourcesForSubjects(IEnumerable<string> subjects, CancellationToken cancellationToken = default)
    {
        const string findResourcesSQL = /*strpsql*/@"select * from resourceregistry.resourcesubjects WHERE subject_urn = ANY(@subjects) AND deleted = false";

        await using NpgsqlCommand pgcom = _conn.CreateCommand(findResourcesSQL);
        pgcom.Parameters.AddWithValue("subjects", subjects.ToArray());
        await using NpgsqlDataReader reader = await pgcom.ExecuteReaderAsync(cancellationToken);

        Dictionary<string, SubjectResources> allSubjectResources = new Dictionary<string, SubjectResources>();

        while (await reader.ReadAsync(cancellationToken))
        {
            AttributeMatchV2 subjectMatch = GetSubjectAttribute(reader);
            AttributeMatchV2 resourceAttributeMatch = GetResourceAttribute(reader);

            if (allSubjectResources.TryGetValue(subjectMatch.Urn, out SubjectResources? existingSubjectResource))
            {
                existingSubjectResource.Resources.Add(resourceAttributeMatch);
            }
            else
            {
                SubjectResources subjectResources = new SubjectResources
                {
                    Subject = subjectMatch,
                    Resources = new List<AttributeMatchV2>() { resourceAttributeMatch }
                };
                allSubjectResources.Add(subjectResources.Subject.Urn, subjectResources);
            }
        }

        List<SubjectResources> subjectResourcesList = new List<SubjectResources>();

        foreach (KeyValuePair<string, SubjectResources> kvp in allSubjectResources)
        {
            subjectResourcesList.Add(kvp.Value);
        }

        return subjectResourcesList;
    }

    private static AttributeMatchV2 GetSubjectAttribute(NpgsqlDataReader reader)
    {
        return new AttributeMatchV2
        {
            Type = reader.GetFieldValue<string>("subject_type"),
            Value = reader.GetFieldValue<string>("subject_value"),
            Urn = reader.GetFieldValue<string>("subject_urn")
        };
    }

    private static AttributeMatchV2 GetResourceAttribute(NpgsqlDataReader reader)
    {
        return new AttributeMatchV2
        {
            Type = reader.GetFieldValue<string>("resource_type"),
            Value = reader.GetFieldValue<string>("resource_value"),
            Urn = reader.GetFieldValue<string>("resource_urn")
        };
    }

    /// <inheritdoc/>
    public async Task<List<ResourceSubjects>> FindSubjectsForResources(IEnumerable<string> resources, CancellationToken cancellationToken = default)
    {
        const string findResourcesSQL = /*strpsql*/@"select * from resourceregistry.resourcesubjects WHERE resource_urn = ANY(@resources) AND deleted = false";

        await using NpgsqlCommand pgcom = _conn.CreateCommand(findResourcesSQL);
        pgcom.Parameters.AddWithValue("resources", resources.ToArray());
        await using NpgsqlDataReader reader = await pgcom.ExecuteReaderAsync(cancellationToken);

        Dictionary<string, ResourceSubjects> allResourceSubjects = new Dictionary<string, ResourceSubjects>();

        while (await reader.ReadAsync(cancellationToken))
        {
            AttributeMatchV2 subjectMatch = GetSubjectAttribute(reader);
            AttributeMatchV2 resourceAttributeMatch = GetResourceAttribute(reader);

            string owner = reader.GetFieldValue<string>("resource_owner");

            if (allResourceSubjects.TryGetValue(resourceAttributeMatch.Urn, out ResourceSubjects? resourceSubjectsExt))
            {
                resourceSubjectsExt.Subjects.Add(subjectMatch);
            }
            else 
            {
                ResourceSubjects resourceSubjects = new ResourceSubjects
                {
                   Resource = resourceAttributeMatch,
                   Subjects = new List<AttributeMatchV2>() { subjectMatch },
                   ResourceOwner = owner
                };
                allResourceSubjects.Add(resourceSubjects.Resource.Urn, resourceSubjects);
            }
        }

        List<ResourceSubjects> subjectResources = new List<ResourceSubjects>();

        foreach (KeyValuePair<string,ResourceSubjects> kvp in allResourceSubjects)
        {
            subjectResources.Add(kvp.Value);
        }

        return subjectResources;
    }

    /// <inheritdoc />
    public async Task<List<UpdatedResourceSubject>> FindUpdatedResourceSubjects(DateTimeOffset lastUpdated, CancellationToken cancellationToken = default)
    {
        const string selecUpdatedPairs = /*strpsql*/@"SELECT resource_urn, subject_urn, updated_at, deleted FROM resourceregistry.resourcesubjects WHERE updated_at > @updated_at";

        await using NpgsqlCommand pgcom = _conn.CreateCommand(selecUpdatedPairs);
        pgcom.Parameters.AddWithValue("updated_at", NpgsqlDbType.TimestampTz, lastUpdated);
        await using NpgsqlDataReader reader = await pgcom.ExecuteReaderAsync(cancellationToken);

        List<UpdatedResourceSubject> updatedResourceSubjects = new List<UpdatedResourceSubject>();

        while (await reader.ReadAsync(cancellationToken))
        {
            updatedResourceSubjects.Add(new UpdatedResourceSubject
            {
                ResourceUrn = new Uri(reader.GetFieldValue<string>("resource_urn")),
                SubjectUrn = new Uri(reader.GetFieldValue<string>("subject_urn")),
                UpdatedAt = reader.GetFieldValue<DateTimeOffset>("updated_at"),
                Deleted = reader.GetFieldValue<bool>("deleted")
            });
        }

        return updatedResourceSubjects;
    }

    /// <inheritdoc/>
    public async Task SetResourceSubjects(ResourceSubjects resourceSubjects, CancellationToken cancellationToken = default)
    {
        const string selectExistingPairsSQL = /*strpsql*/@"SELECT resource_urn, subject_urn FROM resourceregistry.resourcesubjects WHERE resource_urn = @resourceurn AND deleted = false";
        const string updateResourceSubjectsSQL = /*strpsql*/@"UPDATE resourceregistry.resourcesubjects SET deleted = true WHERE resource_urn = @resourceurn AND subject_urn = @subjecturn";
        const string insertOrUpdateResourceSubjectsSQL = /*strpsql*/@"
            INSERT INTO resourceregistry.resourcesubjects (
                resource_type, resource_value, resource_urn, subject_type, subject_value, subject_urn, resource_owner, deleted
            ) VALUES (
                @resourcetype, @resourcevalue, @resourceurn, @subjecttype, @subjectvalue, @subjecturn, @owner, false
            )
            ON CONFLICT (resource_urn, subject_urn) 
            DO UPDATE SET 
                resource_type = EXCLUDED.resource_type,
                resource_value = EXCLUDED.resource_value,
                subject_type = EXCLUDED.subject_type,
                subject_value = EXCLUDED.subject_value,
                resource_owner = EXCLUDED.resource_owner,
                deleted = false;";

        await using NpgsqlConnection conn = await _conn.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction tx = await conn.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);

        // Step 1: Retrieve existing resource-subject pairs from the database
        var existingPairs = new HashSet<(string ResourceUrn, string SubjectUrn)>();

        await using (var selectCmd = _conn.CreateCommand(selectExistingPairsSQL))
        {
            selectCmd.Parameters.AddWithValue("resourceurn", resourceSubjects.Resource.Urn);
            await using (var reader = await selectCmd.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    existingPairs.Add((reader.GetString(0), reader.GetString(1)));
                }
            }
        }

        // Step 2: Mark pairs as deleted if they are no longer in resourceSubjects
        foreach (var existingPair in
                 existingPairs.Where(existingPair =>
                     resourceSubjects.Subjects.All(s => s.Urn != existingPair.SubjectUrn)))
        {
            await using var updateCmd = _conn.CreateCommand(updateResourceSubjectsSQL);
            updateCmd.Parameters.AddWithValue("resourceurn", existingPair.ResourceUrn);
            updateCmd.Parameters.AddWithValue("subjecturn", existingPair.SubjectUrn);
            await updateCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // Step 3: Insert or update resource-subject pairs
        await using (var resourceCmd = _conn.CreateCommand(insertOrUpdateResourceSubjectsSQL))
        {
            resourceCmd.Parameters.Add("resourcetype", NpgsqlTypes.NpgsqlDbType.Text);
            resourceCmd.Parameters.Add("resourcevalue", NpgsqlTypes.NpgsqlDbType.Text);
            resourceCmd.Parameters.Add("resourceurn", NpgsqlTypes.NpgsqlDbType.Text);
            resourceCmd.Parameters.Add("subjecttype", NpgsqlTypes.NpgsqlDbType.Text);
            resourceCmd.Parameters.Add("subjectvalue", NpgsqlTypes.NpgsqlDbType.Text);
            resourceCmd.Parameters.Add("subjecturn", NpgsqlTypes.NpgsqlDbType.Text);
            resourceCmd.Parameters.Add("owner", NpgsqlTypes.NpgsqlDbType.Text);

            foreach (var subjectAttribute in resourceSubjects.Subjects)
            {
                resourceCmd.Parameters["resourcetype"].Value = resourceSubjects.Resource.Type;
                resourceCmd.Parameters["resourcevalue"].Value = resourceSubjects.Resource.Value;
                resourceCmd.Parameters["resourceurn"].Value = resourceSubjects.Resource.Urn;
                resourceCmd.Parameters["subjecttype"].Value = subjectAttribute.Type;
                resourceCmd.Parameters["subjectvalue"].Value = subjectAttribute.Value;
                resourceCmd.Parameters["subjecturn"].Value = subjectAttribute.Urn;
                resourceCmd.Parameters["owner"].Value = resourceSubjects.ResourceOwner;
                await resourceCmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        await tx.CommitAsync(cancellationToken);
    }

    private static async ValueTask<ServiceResource> GetServiceResource(NpgsqlDataReader reader)
    {
        var json = await reader.GetFieldValueAsync<JsonDocument>("serviceresourcejson");

        return json.Deserialize<ServiceResource>(JsonSerializerOptions) ??
            throw new SqlNullValueException("Got null when trying to parse ServiceResource");
    }
}
