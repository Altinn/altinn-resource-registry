using System.Security.Cryptography.X509Certificates;
using Altinn.Common.AccessTokenClient.Configuration;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.ResourceRegistry.Core.Configuration;
using Altinn.ResourceRegistry.Core.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Altinn.ResourceRegistry.Core.Services
{
    /// <inheritdoc />
    public class AccessTokenProvider : IAccessTokenProvider
    {
        private readonly IKeyVaultService _keyVaultService;
        private readonly IAccessTokenGenerator _accessTokenGenerator;
        private readonly PlatformSettings _platformSettings;
        private readonly AccessTokenSettings _accessTokenSettings;
        private readonly SecretsSettings _secretsSettings;
        private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
        private DateTime _cacheTokenUntil = DateTime.MinValue;
        private string? _accessToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccessTokenProvider"/> class.
        /// </summary>
        /// <param name="keyVaultService">The key vault service.</param>
        /// <param name="accessTokenGenerator">The access token generator.</param>
        /// <param name="accessTokenSettings">The access token settings.</param>
        /// <param name="keyVaultSettings">The key vault settings.</param>
        /// <param name="platformSettings">The platform settings.</param>
        public AccessTokenProvider(
            IKeyVaultService keyVaultService,
            IAccessTokenGenerator accessTokenGenerator,
            IOptions<AccessTokenSettings> accessTokenSettings,
            IOptions<SecretsSettings> keyVaultSettings,
            IOptions<PlatformSettings> platformSettings)
        {
            _keyVaultService = keyVaultService;
            _accessTokenGenerator = accessTokenGenerator;
            _platformSettings = platformSettings.Value;
            _accessTokenSettings = accessTokenSettings.Value;
            _secretsSettings = keyVaultSettings.Value;
        }

        /// <inheritdoc />
        public async Task<string?> GetAccessToken()
        {
            await Semaphore.WaitAsync();

            try
            {
                if (_accessToken == null || _cacheTokenUntil < DateTime.UtcNow)
                {
                    string? certBase64 = await _keyVaultService.GetCertificateAsync(_secretsSettings.KeyVaultUri, _secretsSettings.PlatformCertSecretId);
                    if (certBase64 != null)
                    {
                        _accessToken = _accessTokenGenerator.GenerateAccessToken(
                            _platformSettings.AccessTokenIssuer,
                            "internal.authorization",
                            new X509Certificate2(
                                Convert.FromBase64String(certBase64),
                                (string)null!,
                                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable));
                    }

                    _cacheTokenUntil = DateTime.UtcNow.AddSeconds(_accessTokenSettings.TokenLifetimeInSeconds - 2); // Add some slack to avoid tokens expiring in transit
                }

                return _accessToken;
            }
            finally
            {
                Semaphore.Release();
            }
        }
    }
}
