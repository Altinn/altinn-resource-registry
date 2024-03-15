#nullable enable
namespace Altinn.ResourceRegistry.Core.Models
{
    /// <summary>
    /// All subjects for a given resource
    /// </summary>
    public class ResourceSubjects
    {
        /// <summary>
        /// Constructor with required params
        /// </summary>
        public ResourceSubjects(AttributeMatchV2 resource, List<AttributeMatchV2> subjects, string resourceOwner)
        {
            Resource = resource;
            Subjects = subjects;
            ResourceOwner = resourceOwner;
        }

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
