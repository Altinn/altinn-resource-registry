#nullable enable

namespace Altinn.ResourceRegistry.Core.Auth;

/// <summary>
/// Interface used for objects that have a resource owner
/// and can be authorized against such.
/// </summary>
public interface IHasResourceOwner
{
    /// <summary>
    /// Get the resource owner.
    /// </summary>
    string ResourceOwner { get; }
}
