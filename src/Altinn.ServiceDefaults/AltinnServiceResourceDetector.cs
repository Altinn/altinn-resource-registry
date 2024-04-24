using Altinn.ServiceDefaults;
using OpenTelemetry.Resources;

namespace Microsoft.Extensions.Hosting;

internal class AltinnServiceResourceDetector
    : IResourceDetector
{
    private readonly AltinnServiceDescription _serviceDescription;

    public AltinnServiceResourceDetector(AltinnServiceDescription serviceDescription)
    {
        _serviceDescription = serviceDescription;
    }

    public Resource Detect()
    {
        return new Resource(_serviceDescription);
    }
}
