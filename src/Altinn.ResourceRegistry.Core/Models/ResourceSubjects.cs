namespace Altinn.ResourceRegistry.Core.Models
{
    /// <summary>
    /// All subjects for a given resource
    /// </summary>
    public class ResourceSubjects
    {
        /// <summary>
        /// The resource itself defined with a resource attribute
        /// </summary>
        public AttributeMatchV2 Resource { get; set; }

        /// <summary>
        /// A list of all subjectattribute for that resource 
        /// </summary>
        public List<AttributeMatchV2> Subjects { get; set; }   

        /// <summary>
        /// The resource owner
        /// </summary>
        public string ResourceOwner { get; set; }   
    }
}
