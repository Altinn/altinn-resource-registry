﻿#nullable enable
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Altinn.ResourceRegistry.Core.Models
{
    /// <summary>
    /// This model describes a pair of AttributeId and AttributeValue for use in matching in XACML policies, for instance a resource, a user, a party or an action.
    /// </summary>
    public class AttributeMatchV2
    {
        /// <summary>
        /// Constructor with required params
        /// </summary>
        public AttributeMatchV2(string type, string value, string urn)
        {
            Type = type;
            Value = value;
            Urn = urn;
        }

        /// <summary>
        /// Gets or sets the attribute id for the match
        /// </summary>
        [Required]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the attribute value for the match
        /// </summary>
        [Required]
        public string Value { get; set; }

        /// <summary>
        /// The urn for the attribute
        /// </summary>
        [Required]
        public string Urn { get; set; }
    }
}
