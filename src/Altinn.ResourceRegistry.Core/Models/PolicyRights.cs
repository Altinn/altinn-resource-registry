using System.Security.Cryptography;
using System.Text;
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
        public required UrnJsonTypeValue Action { get; init; }

        /// <summary>
        /// The Resource attributes that identy one unique resource 
        /// </summary>
        public required IReadOnlyList<UrnJsonTypeValue> Resource { get; init; }

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
                string shortKey = key;
                foreach (var res in Resource.OrderBy(x => x.Value.ToString()))
                {
                    key += ";" + res.Value.ToString();
                    shortKey += ";" + res.Value.ValueSpan.ToString();
                }

                using (MD5 md5 = MD5.Create())
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(key);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("x2"));
                    }

                    return shortKey + ";" + sb.ToString();
                }
               
            }
        }

        /// <summary>
        /// Returns a list of subject types that is allowed to perform the action on the resource
        /// IS used for filtering the 
        /// </summary>
        public List<string> SubjectTypes
        {
            get
            {
                HashSet<string> subjectTypes = new HashSet<string>();

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

                return subjectTypes.ToList();
            }
        }
    }
}
