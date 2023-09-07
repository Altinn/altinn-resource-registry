using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Altinn.ResourceRegistry.Core.Helpers;
using static Altinn.ResourceRegistry.Core.Helpers.EnumConverterFactory;

namespace Altinn.ResourceRegistry.Core.Enums
{
    /// <summary>
    /// Enum for reference types of resources in the resource registry
    /// </summary>
    [JsonConverter(typeof(EnumStringValueConverter<ReferenceType>))]
    public enum ReferenceType : int
    {
        [EnumMember(Value = "Default")]
        Default = 0,

        [EnumMember(Value = "Uri")]
        Uri = 1,

        [EnumMember(Value = "DelegationSchemeId")]
        DelegationSchemeId = 2,

        [EnumMember(Value = "MaskinportenScope")]
        MaskinportenScope = 3,

        [EnumMember(Value = "urn:altinn:servicecode")]
        ServiceCode = 4,

        [EnumMember(Value = "urn:altinn:serviceeditioncode")]
        ServiceEditionCode = 5,

        [EnumMember(Value = "urn:altinn:app")]
        App = 6,

        [EnumMember(Value = "urn:altinn:org")]
        Org = 7,
    }
}
