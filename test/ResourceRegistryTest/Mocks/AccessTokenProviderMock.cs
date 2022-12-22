using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Altinn.ResourceRegistry.Core.Services.Interfaces;
using Microsoft.Azure.Services.AppAuthentication;
using ResourceRegistryTest.Utils;

namespace ResourceRegistryTest.Mocks
{
    internal class AccessTokenProviderMock : IAccessTokenProvider
    {
        public Task<string> GetAccessToken()
        {
            return Task.FromResult(PrincipalUtil.GetAccessToken("internal.authorization"));
        }
    }
}
