using Altinn.ResourceRegistry.Core.Services.Interfaces;
using Newtonsoft.Json.Linq;

namespace Altinn.ResourceRegistry.Core.Services
{
    /// <summary>
    /// Local Dev version of the KeyVaultAccess to Provide data when not deployed to container
    /// </summary>
    public class KeyVaultServiceLocal : IKeyVaultService
    {
        /// <summary>
        /// Returns a certificate from disk instead of from KeyVault
        /// </summary>
        /// <param name="vaultUri">Placeholder for real service her ignored</param>
        /// <param name="secretId">Just because key vault needs it</param>
        /// <returns>string containing base64 certificate</returns>
        public Task<string> GetCertificateAsync(string vaultUri, string secretId)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), @"secrets.json");
            string? tokenString = string.Empty;

            if (File.Exists(path))
            {
                string jsonString = File.ReadAllText(path);
                JObject keyVault = JObject.Parse(jsonString);
                keyVault.TryGetValue(secretId, out JToken? token);
                
                tokenString = token != null ? token.ToString() : string.Empty;
            }

            return Task.FromResult(tokenString);
        }
    }
}
