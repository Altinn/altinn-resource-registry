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
        public ResourceAttribute ResourceAttribute { get; set; }

        /// <summary>
        /// A list of all subjectattribute for that resource 
        /// </summary>
        public List<SubjectAttribute> SubjectAttributes { get; set; }   
    }
}
