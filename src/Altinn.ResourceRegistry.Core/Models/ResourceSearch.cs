#nullable enable
using Altinn.ResourceRegistry.Core.Enums;

namespace Altinn.ResourceRegistry.Core.Models
{
    /// <summary>
    /// Model for performing search for resources in the resource registry
    /// </summary>
    public class ResourceSearch
    {
        /// <summary>
        /// ID
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Title
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// ResourceType
        /// </summary>
        public ResourceType? ResourceType { get; set; }

        /// <summary>
        /// Keywords
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// To search for a specific reference
        /// </summary>
        public string? Reference { get; set; }

        /// <summary>
        /// To search for a specific organization code
        /// </summary>
        public string? OrgCode { get; set; }

        /// <summary>
        /// To search for a specific organization number
        /// </summary>
        public string? OrganizationId { get; set; }

        /// <summary>
        /// Generates a cache key based on all properties in the class.
        /// </summary>
        /// <returns>A string cache key representing the current search parameters.</returns>
        public string GetCacheKey()
        {
            return string.Join(
                "|",
                $"Id={Id}",
                $"Title={Title}",
                $"Description={Description}",
                $"ResourceType={ResourceType}",
                $"Keyword={Keyword}",
                $"Reference={Reference}",
                $"OrgCode={OrgCode}",
                $"OrganizationId={OrganizationId}");
        }
    }
}
