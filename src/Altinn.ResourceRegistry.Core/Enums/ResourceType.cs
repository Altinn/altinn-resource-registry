using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Altinn.Authorization.ModelUtils;
using NpgsqlTypes;

namespace Altinn.ResourceRegistry.Core.Enums
{
    /// <summary>
    /// Enum representation of the different types of resources supported by the resource registry
    /// </summary>
    [StringEnumConverter]
    public enum ResourceType
    {
        [EnumMember(Value = "Default")]
        [JsonStringEnumMemberName("Default")]
        [PgName("default")]
        Default = 0,

        [EnumMember(Value = "Systemresource")]
        [JsonStringEnumMemberName("Systemresource")]
        [PgName("systemresource")]
        Systemresource = 1 << 0,

        [EnumMember(Value = "MaskinportenSchema")]
        [JsonStringEnumMemberName("MaskinportenSchema")]
        [PgName("maskinportenschema")]
        MaskinportenSchema = 1 << 1,

        [EnumMember(Value = "Altinn2Service")]
        [JsonStringEnumMemberName("Altinn2Service")]
        [PgName("altinn2service")]
        Altinn2Service = 1 << 2,

        [EnumMember(Value = "AltinnApp")]
        [JsonStringEnumMemberName("AltinnApp")]
        [PgName("altinnapp")]
        AltinnApp = 1 << 3,

        [EnumMember(Value = "GenericAccessResource")]
        [JsonStringEnumMemberName("GenericAccessResource")]
        [PgName("genericaccessresource")]
        GenericAccessResource = 1 << 4,

        [EnumMember(Value = "BrokerService")]
        [JsonStringEnumMemberName("BrokerService")]
        [PgName("brokerservice")]
        BrokerService = 1 << 5,

        [EnumMember(Value = "CorrespondenceService")]
        [JsonStringEnumMemberName("CorrespondenceService")]
        [PgName("correspondenceservice")]
        CorrespondenceService = 1 << 6,

        [EnumMember(Value = "Consent")]
        [JsonStringEnumMemberName("Consent")]
        [PgName("consent")]
        Consent = 1 << 7,
    }
}
