using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Altinn.Common.AccessTokenClient.Services;
using ResourceRegistryTest.Utils;

namespace ResourceRegistryTest.Mocks
{
    public class AccessTokenGeneratorMock : IAccessTokenGenerator
    {
        public string GenerateAccessToken(string issuer, string app)
        {
            return PrincipalUtil.GetAccessToken("ResourceRegister");
        }

        public string GenerateAccessToken(string issuer, string app, X509Certificate2 certificate)
        {
            return PrincipalUtil.GetAccessToken("ResourceRegister");
        }
    }
}
