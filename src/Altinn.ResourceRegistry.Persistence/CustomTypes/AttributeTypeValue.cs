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
        /// </summary>t
        public string Type { get; set; }
    
        /// <summary>
        /// The value
        /// </summary>
        public string Value { get; set; }
    }
}
