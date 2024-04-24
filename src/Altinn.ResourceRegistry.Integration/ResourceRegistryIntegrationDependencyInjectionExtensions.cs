using System.Net.Http.Headers;
using Altinn.ResourceRegistry.Core.Register;
using Altinn.ResourceRegistry.Integration.Clients;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods <see cref="IServiceCollection"/>.
/// </summary>
public static class ResourceRegistryIntegrationDependencyInjectionExtensions
{
    /// <summary>
    /// Add the register client to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns><paramref name="services"/>.</returns>
    public static IServiceCollection AddRegisterClient(this IServiceCollection services)
    {
        services.AddTransient<AuthorizationEnricher>();
        services.AddHttpClient<IRegisterClient, RegisterClient>()
            .ConfigureHttpClient(static (client) =>
            {
                client.BaseAddress = new Uri("http://register");
            })
            .AddHttpMessageHandler<AuthorizationEnricher>();

        return services;
    }

    private class AuthorizationEnricher : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "token");

            return base.SendAsync(request, cancellationToken);
        }
    }
}
