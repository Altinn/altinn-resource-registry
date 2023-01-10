﻿using System;
using AltinnCore.Authentication.JwtCookie;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using ResourceRegistryTest.Mocks.Authentication;

namespace Altinn.ResourceRegistryTest.Tests.Mocks.Authentication
{
    /// <summary>
    /// Represents a stub for the <see cref="JwtCookiePostConfigureOptions"/> class to be used in integration tests.
    /// </summary>
    public class JwtCookiePostConfigureOptionsStub : IPostConfigureOptions<JwtCookieOptions>
    {
        /// <inheritdoc />
        public void PostConfigure(string name, JwtCookieOptions options)
        {
            if (string.IsNullOrEmpty(options.JwtCookieName))
            {
                options.JwtCookieName = JwtCookieDefaults.CookiePrefix + name;
            }

            if (options.CookieManager == null)
            {
                options.CookieManager = new ChunkingCookieManager();
            }

            if (!string.IsNullOrEmpty(options.MetadataAddress))
            {
                if (!options.MetadataAddress.EndsWith("/", StringComparison.Ordinal))
                {
                    options.MetadataAddress += "/";
                }
            }

            if (options.MetadataAddress != ".well-known/openid-configuration/")
            {
                options.MetadataAddress += ".well-known/openid-configuration";
            }
            
            options.ConfigurationManager = new ConfigurationManagerStub();
        }
    }
}