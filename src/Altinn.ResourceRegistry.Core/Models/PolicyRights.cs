using Altinn.Urn;
using Altinn.Urn.Json;

namespace Altinn.ResourceRegistry.Core.Models
{
    /// <summary>
    /// Definees a flatten Policy Rule
    /// </summary>
    public class PolicyRights
    {
        /// <summary>
        /// Defines the action that the subject is allowed to perform on the resource
        /// </summary>
        public required UrnJsonTypeValue Action { get; set; }

        /// <summary>
        /// The Resource attributes that identy one unique resource 
        /// </summary>
        public required IReadOnlyList<UrnJsonTypeValue> Resource { get; set; }

        /// <summary>
        /// List of subjects that is allowed to perform the action on the resource
        /// </summary>
        public required List<PolicySubject> Subjects { get; init; }

        /// <summary>
        /// Returns the right key for the right part of policy resource action
        /// </summary>
        public string RightKey
        {
            get
            {
                string key = Action.Value.ValueSpan.ToString().ToLowerInvariant();
                foreach (var res in Resource.OrderBy(x => x.Value.ToString().ToLowerInvariant()))
                {
                    key += ";" + res.Value.ToString().ToLowerInvariant();
                }

                return key;
            }
        }

        /// <summary>
        /// Returns a list of subject types that is allowed to perform the action on the resource
        /// IS used for filtering the 
        /// </summary>
        public List<string> GetSubjectTypes()
        {
            List<string> subjectTypes = new List<string>();
            foreach (var subject in Subjects)
            {
                foreach (var attr in subject.SubjectAttributes)
                {
                    if (!subjectTypes.Contains(attr.Value.PrefixSpan.ToString().ToLowerInvariant()))
                    {
                        subjectTypes.Add(attr.Value.PrefixSpan.ToString().ToLowerInvariant());
                    }
                }
            }

            return subjectTypes;
        }

        // Convert to property
        public string RightKeyProperty => RightKey;
    }
}
