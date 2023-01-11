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
        /// Base URL to AccessManagement
        /// </summary>
        public string AccessManagementEndpoint { get; set; }

        /// <summary>
        /// Issuer to use in the generated token that will be used in calling AccessManagement
        /// </summary>
        public string AccessTokenIssuer { get; set; } = "Platform";

        /// <summary>
        /// App to use in the generated token that will be used in calling AccessManagement
        /// </summary>
        public string AccessTokenApp { get; set; } = "ResourceRegister";

        /// <summary>
        /// The headder value to use for ACcessToken
        /// </summary>
        public string AccessTokenHeaderId { get; set; } = "PlatformAccessToken";

        /// <summary>
        /// Open Id Connect Well known endpoint
        /// </summary>
        public string? OpenIdWellKnownEndpoint { get; set; }

        /// <summary>
        /// Name of the cookie for where JWT is stored
        /// </summary>
        public string? JwtCookieName { get; set; }

        /// <summary>
        /// Endpoint for authentication
        /// </summary>
        public string? ApiAuthenticationEndpoint { get; set; }
    }
}
