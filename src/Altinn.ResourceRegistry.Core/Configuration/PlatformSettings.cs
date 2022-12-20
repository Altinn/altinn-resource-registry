using System.Diagnostics.CodeAnalysis;

namespace Altinn.ResourceRegistry.Core.Configuration
{
    /// <summary>
    /// Represents a set of configuration options when communicating with the platform API.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class PlatformSettings
    {
        /// <summary>
        /// Base URL to Altinn Bridge
        /// </summary>
        public string AccessManagementEndpoint { get; set; }

        /// <summary>
        /// Issuer to use in the generated token that will be used in calling Bridge API
        /// </summary>
        public string AccessTokenIssuer { get; set; }
    }
}
