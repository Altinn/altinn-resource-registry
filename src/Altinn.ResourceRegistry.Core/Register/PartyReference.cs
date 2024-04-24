#nullable enable

using System.Diagnostics;
using Altinn.Urn;

namespace Altinn.ResourceRegistry.Core.Register;

/// <summary>
/// A unique reference to a party in the form of an URN.
/// </summary>
[Urn]
public abstract partial record PartyReference
{
    /// <summary>
    /// Try to get the urn as a party id.
    /// </summary>
    /// <param name="partyId">The resulting party id.</param>
    /// <returns><see langword="true"/> if this party reference is a party id, otherwise <see langword="false"/>.</returns>
    [UrnType("altinn:party:id")]
    public partial bool AsPartyId(out int partyId);

    /// <summary>
    /// Try to get the urn as a party uuid.
    /// </summary>
    /// <param name="partyUuid">The resulting party uuid.</param>
    /// <returns><see langword="true"/> if this party reference is a party uuid, otherwise <see langword="false"/>.</returns>
    [UrnType("altinn:party:uuid")]
    [UrnType("altinn:organization:uuid")]
    public partial bool AsPartyUuid(out Guid partyUuid);

    /// <summary>
    /// Try to get the urn as an organization number.
    /// </summary>
    /// <param name="organizationNumber">The resulting organization number.</param>
    /// <returns><see langword="true"/> if this party reference is an organization number, otherwise <see langword="false"/>.</returns>
    [UrnType("altinn:organization:identifier-no")]
    public partial bool AsOrganizationIdentifier(out OrganizationNumber organizationNumber);

    public partial record PartyUuid
    {
        /// <summary>
        /// Creates a new <see cref="PartyUuid"/> from a party uuid.
        /// </summary>
        /// <param name="partyUuid">The party uuid.</param>
        /// <returns>The created party uuid.</returns>
        public static PartyUuid CreateOrg(Guid partyUuid)
        {
            var urn = $"urn:altinn:organization:uuid:{partyUuid}";
            var ret = new PartyUuid(urn, partyUuid);

            Debug.Assert(PartyReference.TryParse(urn, out var parsed) && parsed == ret);
            return ret;
        }
    }
}
