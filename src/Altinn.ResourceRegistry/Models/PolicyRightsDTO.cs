﻿using Altinn.ResourceRegistry.Core.Models;
using Altinn.Urn.Json;

namespace Altinn.ResourceRegistry.Models
{
    /// <summary>
    /// Definees a flatten Policy Rule
    /// </summary>
    public class PolicyRightsDTO
    {
        /// <summary>
        /// Defines the action that the subject is allowed to perform on the resource
        /// </summary>
        public required UrnJsonTypeValue Action { get; init; }

        /// <summary>
        /// The Resource attributes that identy one unique resource 
        /// </summary>
        public required IReadOnlyList<UrnJsonTypeValue> Resource { get; init; }

        /// <summary>
        /// List of subjects that is allowed to perform the action on the resource
        /// </summary>
        public required List<PolicySubjectDTO> Subjects { get; init; }

        /// <summary>
        /// Returns the right key for the right part of policy resource action
        /// </summary>
        public string RightKey { get; init; }
    
        /// <summary>
        /// Returns a list of subject types that is allowed to perform the action on the resource
        /// IS used for filtering the 
        /// </summary>
        public IReadOnlySet<string> SubjectTypes { get; init; }

        /// <summary>
        /// Map to DTO List
        /// </summary>
        public static List<PolicyRightsDTO> MapToDTO(List<PolicyRights> policyRights)
        {
            if (policyRights == null)
            {
                return null;
            }

            List<PolicyRightsDTO> policyRightsDTOs = new List<PolicyRightsDTO>();

            foreach (PolicyRights policyRight in policyRights)
            {
                policyRightsDTOs.Add(MapToDTO(policyRight));
            }

            return policyRightsDTOs;
        }

        /// <summary>
        /// MAP to DTO
        /// </summary>
        public static PolicyRightsDTO MapToDTO(PolicyRights policyRights)
        {
            PolicyRightsDTO policyRightsDTO = new PolicyRightsDTO
            {
                Action = policyRights.Action,
                Resource = policyRights.Resource,
                Subjects = PolicySubjectDTO.MapToDTO(policyRights.Subjects),
                RightKey = policyRights.RightKey,
                SubjectTypes = policyRights.SubjectTypes,
            };

            return policyRightsDTO;
        }
    }
}
