using Altinn.Urn.Json;

namespace Altinn.ResourceRegistry.Models
{
    /// <summary>
    /// Defines a  Policy Subject
    /// </summary>
    public class PolicySubjectDTO
    {
        /// <summary>
        /// Subject attributes that defines the subject
        /// </summary>
        public required IReadOnlyList<UrnJsonTypeValue> SubjectAttributes { get; init; }
    }
}
