using Altinn.Urn;
using Altinn.Urn.Json;

namespace Altinn.ResourceRegistry.Core.Models
{
    /// <summary>
    /// Definees a flatten Policy Rule
    /// </summary>
    public class PolicyRule
    {
        /// <summary>
        /// The Subject target in rule
        /// </summary>
        public IReadOnlyList<UrnJsonTypeValue> Subject { get; set; }

        /// <summary>
        /// Defines the action that the subject is allowed to perform on the resource
        /// </summary>
        public UrnJsonTypeValue Action { get; set; }

        /// <summary>
        /// The Resource attributes that identy one unique resource 
        /// </summary>
        public IReadOnlyList<UrnJsonTypeValue> Resource { get; set; }    
    }
}
