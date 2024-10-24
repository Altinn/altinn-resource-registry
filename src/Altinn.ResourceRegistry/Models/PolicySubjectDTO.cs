using Altinn.ResourceRegistry.Core.Models;
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

        /// <summary>
        /// Map to DTO List
        /// </summary>
        public static List<PolicySubjectDTO> MapToDTO(IReadOnlyList<PolicySubject> policySubjects)
        {
            if (policySubjects == null)
            {
                return null;
            }

            List<PolicySubjectDTO> policySubjectsDTOs = new List<PolicySubjectDTO>(policySubjects.Count);

            foreach (PolicySubject policySubject in policySubjects)
            {
                policySubjectsDTOs.Add(MapToDTO(policySubject));
            }

            return policySubjectsDTOs;
        }

        /// <summary>
        /// Map to DTO
        /// </summary>
        public static PolicySubjectDTO MapToDTO(PolicySubject policySubject)
        {
            if (policySubject == null)
            {
                return null;
            }

            PolicySubjectDTO policySubjectDTO = new PolicySubjectDTO
            {
                SubjectAttributes = policySubject.SubjectAttributes
            };

            return policySubjectDTO;
        }
    }
}
