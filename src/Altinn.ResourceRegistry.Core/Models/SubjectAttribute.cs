using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.ResourceRegistry.Core.Models
{
    /// <summary>
    /// Defines related 
    /// </summary>
    public class SubjectAttribute
    {
        /// <summary>
        /// Subject that has access to a subject. Defined with id type and subject type. Example urn:altinn:rolecode:utinn
        /// </summary>
        public string Type { get; set; }   
        
        /// <summary>
        /// The subject value. Depends on the subject type.  Could be "utinn" (roleclode)
        /// </summary>
        public string Value { get; set; }
    }
}
