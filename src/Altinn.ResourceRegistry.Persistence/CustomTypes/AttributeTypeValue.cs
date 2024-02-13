using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.ResourceRegistry.Persistence.CustomTypes
{
    /// <summary>
    /// C# definition for custom datatype
    /// </summary>
    public record AttributeTypeValue
    {
        /// <summary>
        /// The type
        /// </summary>
        public required string Type { get; init; }
    
        /// <summary>
        /// The value
        /// </summary>
        public required string Value { get; init; }
    }
}
