﻿using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Altinn.Common.PEP.Constants;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Models;
using AltinnCore.Authentication.Constants;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace Altinn.ResourceRegistry.Utils
{
    /// <summary>
    /// Custom authorization
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class AuthorizationUtil
    {
        /// <summary>
        /// Verifies that org string matches org in user claims.
        /// </summary>
        /// <param name="org">Organisation to match in claims.</param>
        /// <param name="user">Claim principal from http context.</param>
        /// <returns>true if the given ClaimsPrincipal contains the given org.</returns>
        public static bool VerifyOrgInClaimPrincipal(string org, ClaimsPrincipal user)
        {
            Console.WriteLine($"AuthorizationUtil // VerifyOrg // Trying to verify org in claims.");

            string orgClaim = user?.Claims.Where(c => c.Type.Equals(AltinnCoreClaimTypes.OrgNumber)).Select(c => c.Value).FirstOrDefault();

            Console.WriteLine($"AuthorizationUtil // VerifyOrg // Org claim: {orgClaim}.");

            if (org.Equals(orgClaim, StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Verifies a scope claim based on claimsprincipal.
        /// </summary>
        /// <param name="requiredScope">Requiered scope.</param>
        /// <param name="user">Claim principal from http context.</param>
        /// <returns>true if the given ClaimsPrincipal or on of its identities have contains the given scope.</returns>
        public static bool ContainsRequiredScope(List<string> requiredScope, ClaimsPrincipal user)
        {
            string contextScope = user.Identities?
               .FirstOrDefault(i => i.AuthenticationType != null && i.AuthenticationType.Equals("AuthenticationTypes.Federation"))
               ?.Claims
               .Where(c => c.Type.Equals("urn:altinn:scope"))
               ?.Select(c => c.Value).FirstOrDefault();

            contextScope ??= user.Claims.Where(c => c.Type.Equals("scope")).Select(c => c.Value).FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(contextScope))
            {
                return requiredScope.Any(scope => contextScope.Contains(scope, StringComparison.InvariantCultureIgnoreCase));
            }

            return false;
        }

        /// <summary>
        /// Checks if the authenticated user has admin scope
        /// </summary>
        /// <returns>true/false</returns>
        public static bool HasAdminAccess(ClaimsPrincipal user)
        {
            List<string> requiredScopes = new List<string>();
            requiredScopes.Add(AuthzConstants.SCOPE_RESOURCEREGISTRY_ADMIN);
            if (ContainsRequiredScope(requiredScopes, user))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if resource owner organisation matches organisation in claim
        /// </summary>
        /// <param name="resourceOwner">the organisation number that owns the resource</param>
        /// <param name="user">the authenticated user claim</param>
        /// <returns></returns>
        public static bool IsOwnerOfResource(string resourceOwner, ClaimsPrincipal user) 
        {
            Console.WriteLine($"AuthorizationUtil // IsOwnerOfResource // Checking organisation number in claims.");

            string orgClaim = user?.Claims.Where(c => c.Type.Equals(AltinnCoreClaimTypes.OrgNumber)).Select(c => c.Value).FirstOrDefault();

            Console.WriteLine($"AuthorizationUtil // IsOwnerOfResource // Org claim: {orgClaim}.");

            if (resourceOwner.Equals(orgClaim, StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if authenticated user has read access to the resource
        /// </summary>
        /// <param name="resourceOwner">the organisation number that owns the resource</param>
        /// <param name="user">the authenticated user claim</param>
        /// <returns></returns>
        public static bool HasReadAccess(string resourceOwner, ClaimsPrincipal user)
        {
            if (AuthorizationUtil.HasAdminAccess(user) || AuthorizationUtil.IsOwnerOfResource(resourceOwner, user))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if authenticated user has write access
        /// </summary>
        /// <param name="resourceOwner">the organisation number that owns the resource</param>
        /// <param name="user">the authenticated user claim</param>
        /// <returns></returns>
        public static bool HasWriteAccess(string resourceOwner, ClaimsPrincipal user)
        {
            List<string> requiredScopes = new List<string>();
            requiredScopes.Add(AuthzConstants.SCOPE_RESOURCEREGISTRY_WRITE);
            if (HasAdminAccess(user) || (IsOwnerOfResource(resourceOwner, user) && ContainsRequiredScope(requiredScopes, user)))
            {
                return true;
            }

            return false;
        }
    }
}
