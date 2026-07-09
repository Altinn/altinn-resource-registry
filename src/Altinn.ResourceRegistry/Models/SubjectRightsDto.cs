namespace Altinn.ResourceRegistry.Models
{
    /// <summary>
    /// Represents a rule for a subject in the resource registry.
    /// </summary>
    public class SubjectRightsDto
    {
        /// <summary>
        /// Defined as the subject. This will be package, application, organization, user or similar. The value will be used to match against the subject in the policy.
        /// </summary>
        public List<AttributeMatchDTO> Subject { get; set; }

        /// <summary>
        /// The List of rights that are associated with the subject. The rights will be used to match against the rights in the policy.
        /// </summary>
        public List<RightDto> Rights { get; set; }
    }
}
