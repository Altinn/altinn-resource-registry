using System.Runtime.Serialization;

namespace Altinn.ResourceRegistry.Core.Enums
{
    /// <summary>
    /// Enum for reference types of resources in the resource registry
    /// </summary>
    public enum ReferenceType : int
    {
        [EnumMember(Value = "Default")]
        Default = 0,

        [EnumMember(Value = "Uri")]
        Uri = 3,
        
        [EnumMember(Value = "DelegationSchemeId")]
        DelegationSchemeId = 4,

        [EnumMember(Value = "MaskinportenScope")]
        DelegationScope = 5,
        
        [EnumMember(Value = "ServiceCode")]
        ServiceCode = 6,

        [EnumMember(Value = "ServiceEditionCode")]
        ServiceEditionCode = 7,
    }
}
