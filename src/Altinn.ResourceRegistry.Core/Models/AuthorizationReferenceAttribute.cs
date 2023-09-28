﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.ResourceRegistry.Core.Models
{
    /// <summary>
    /// The reference
    /// </summary>
    public class AuthorizationReferenceAttribute
    {
        /// <summary>
        /// The key for authorization reference. Used for authorization api related to resource
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The value for authorization reference. Used for authorization api related to resource
        /// </summary>
        public string Value { get; set; }
    }
}
