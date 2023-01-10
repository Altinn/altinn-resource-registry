﻿using Altinn.Common.Authentication.Configuration;
using Altinn.Common.Authentication.Models;
using Microsoft.IdentityModel.Protocols;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ResourceRegistryTest.Mocks
{
    public class OidcProviderSettingsStub : IConfigurationManager<OidcProviderSettings>
    {
        /// <inheritdoc />
        public async Task<OidcProviderSettings> GetConfigurationAsync(CancellationToken cancel)
        {
            OidcProvider provider = new OidcProvider();
            provider.Issuer = "www.altinn.no";
            provider.WellKnownConfigEndpoint = "https://testEndpoint.no";

            OidcProviderSettings settings = new OidcProviderSettings()
            {
                { "altinn", provider}
            };

            return settings;
        }


        public void RequestRefresh()
        {
            throw new System.NotImplementedException();
        }
    }
}