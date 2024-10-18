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
        public List<AttributeMatchV3> Subject { get; set; }

        /// <summary>
        /// Defines the action that the subject is allowed to perform on the resource
        /// </summary>
        public AttributeMatchV3 Action { get; set; }

        /// <summary>
        /// The Resource attributes that identy one unique resource 
        /// </summary>
        public List<AttributeMatchV3> Resource { get; set; }    
    }
}
