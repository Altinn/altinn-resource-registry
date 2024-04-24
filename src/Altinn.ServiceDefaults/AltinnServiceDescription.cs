using System.Collections;

namespace Altinn.ServiceDefaults;

public sealed class AltinnServiceDescription
    : IEnumerable<KeyValuePair<string, object>>
{
    public string Name { get; }

    public bool IsLocalDev { get; }

    public AltinnServiceDescription(string name, bool isLocalDev)
    {
        Name = name;
        IsLocalDev = isLocalDev;
    }

    IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
    {
        yield return KeyValuePair.Create("service.name", (object)Name);
        if (IsLocalDev)
        {
            yield return KeyValuePair.Create("altinn.local_dev", (object)true);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() 
        => ((IEnumerable<KeyValuePair<string, object>>)this).GetEnumerator();
}
