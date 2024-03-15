#nullable enable
namespace Altinn.ResourceRegistry.Core.Models
{
    /// <summary>
    /// Defines related 
    /// </summary>
    public class SubjectResources
    {
        /// <summary>
        /// The subject
        /// </summary>
        public required AttributeMatchV2 Subject { get; set; }    

        /// <summary>
        /// List of resources that the given subject has access to
        /// </summary>
        public required List<AttributeMatchV2> Resources { get; set; }
    }
}
