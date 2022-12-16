using System.Diagnostics.CodeAnalysis;

namespace Altinn.ResourceRegistry.Core.Configuration
{
    /// <summary>
    /// Configuration object used to hold settings for the KeyVault.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class SecretsSettings
    {
        /// <summary>
        /// Uri to keyVault
        /// </summary>
        public string KeyVaultUri { get; set; }

        /// <summary>
        /// Name of the certificate secret
        /// </summary>
        public string PlatformCertSecretId { get; set; } = "JWTCertificate";
    }
}
