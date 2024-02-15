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
        public AttributeMatchV2 Subject { get; set; }    

        /// <summary>
        /// List of resources that the given subject has access to
        /// </summary>
        public List<AttributeMatchV2> Resources { get; set; }
    }
}
