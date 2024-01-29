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
        public SubjectAttribute Subject { get; set; }    

        /// <summary>
        /// List of resources that the given subject has access to
        /// </summary>
        public List<ResourceAttribute> Resources { get; set; }
    }
}
