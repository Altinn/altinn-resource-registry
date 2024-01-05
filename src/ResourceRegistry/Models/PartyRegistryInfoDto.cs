using Altinn.ResourceRegistry.Core.PartyRegistry;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// Represents public party registry metadata.
/// </summary>
/// <param name="Identifier">The party registry identifier</param>
/// <param name="Name">The party registry name</param>
/// <param name="Description">The party registry description</param>
/// <param name="CreatedAt">When the party registry was created</param>
/// <param name="UpdatedAt">When the party registry was updated</param>
[SwaggerSchemaFilter(typeof(SchemaFilter))]
public record PartyRegistryInfoDto(
    string Identifier,
    string Name,
    string Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>
    /// Create a <see cref="PartyRegistryInfoDto"/> from a <see cref="PartyRegistryInfo"/>.
    /// </summary>
    /// <param name="info">The <see cref="PartyRegistryInfo"/></param>
    /// <returns>A <see cref="PartyRegistryInfoDto"/> matching the <see cref="PartyRegistryInfo"/></returns>
    public static PartyRegistryInfoDto From(PartyRegistryInfo info)
        => new(
            info.Identifier,
            info.Name,
            info.Description,
            info.CreatedAt,
            info.UpdatedAt);

    private sealed class SchemaFilter : ISchemaFilter
    {
        /// <inheritdoc/>
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            foreach (var prop in schema.Properties)
            {
                schema.Required.Add(prop.Key);
            }

            schema.Properties["identifier"].Nullable = false;
            schema.Properties["identifier"].Format = "slug";
            schema.Properties["identifier"].Example = new OpenApiString("godkjente-banker");
            schema.Properties["name"].Nullable = false;
            schema.Properties["name"].Example = new OpenApiString("Godkjente banker");
            schema.Properties["description"].Nullable = false;
            schema.Properties["description"].Example = new OpenApiString("En liste over godkjente banker");
        }
    }
}
