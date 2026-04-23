namespace Altinn.ResourceRegistry.Core.Models
{
    /// <summary>
    /// Defines texts used in consent templates
    /// </summary>
    public class ConsentTemplateTypeText
    {
        /// <summary>
        /// Texts used in consent for organization. Allowed keys: "no", "nb", "en".
        /// </summary>
        public Dictionary<string, string> Org { get; set; }

        /// <summary>
        /// Texts used in consent for person. Allowed keys: "no", "nb", "en".
        /// </summary>
        public Dictionary<string, string> Person { get; set; }
    }
}
