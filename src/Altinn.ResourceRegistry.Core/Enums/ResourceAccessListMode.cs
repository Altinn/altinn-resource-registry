using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Altinn.Authorization.ModelUtils;

namespace Altinn.ResourceRegistry.Core.Enums
{
    /// <summary>
    /// Enum representation of the different types of ResourceAccessListModes supported by the resource registry
    /// </summary>
    [StringEnumConverter]
    public enum ResourceAccessListMode
    {
        [EnumMember(Value = "Disabled")]
        [JsonStringEnumMemberName("Disabled")]
        Disabled = 0,

        [EnumMember(Value = "Enabled")]
        [JsonStringEnumMemberName("Enabled")]
        Enabled = 1,
     }
}
