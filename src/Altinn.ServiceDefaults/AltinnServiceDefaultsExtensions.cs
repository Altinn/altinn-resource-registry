using Altinn.ServiceDefaults;
using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Hosting;

public static class AltinnServiceDefaultsExtensions
{
    public static IHostApplicationBuilder AddAltinnServiceDefaults(this IHostApplicationBuilder builder, string name)
    {
        Guard.IsNotNull(builder);
        Guard.IsNotNullOrEmpty(name, nameof(name));

        if (builder.Services.TryFindAltinnServiceDescription(out var serviceDescription))
        {
            Guard.IsEqualTo(name, serviceDescription.Name);
            return builder;
        }
        else
        {
            serviceDescription = new AltinnServiceDescription(name, builder.IsLocalDevelopment());
            builder.Services.AddSingleton(serviceDescription);
            builder.Services.AddSingleton<AltinnServiceResourceDetector>();
        }

        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.UseServiceDiscovery();
        });

        return builder;
    }

    private static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddDetector(services => services.GetRequiredService<AltinnServiceResourceDetector>());
            })
            .WithMetrics(metrics =>
            {
                metrics.AddRuntimeInstrumentation()
                       .AddBuiltInMeters();
            })
            .WithTracing(tracing =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    // We want to view all traces in development
                    tracing.SetSampler(new AlwaysOnSampler());
                }

                tracing.AddAspNetCoreInstrumentation()
                       .AddGrpcClientInstrumentation()
                       .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.Configure<OpenTelemetryLoggerOptions>(logging => logging.AddOtlpExporter());
            builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddOtlpExporter());
            builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddOtlpExporter());
        }

        // Uncomment the following lines to enable the Prometheus exporter (requires the OpenTelemetry.Exporter.Prometheus.AspNetCore package)
        // builder.Services.AddOpenTelemetry()
        //    .WithMetrics(metrics => metrics.AddPrometheusExporter());

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        // builder.Services.AddOpenTelemetry()
        //    .UseAzureMonitor();

        return builder;
    }

    private static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultAltinnEndpoints(this WebApplication app)
    {
        // Uncomment the following line to enable the Prometheus endpoint (requires the OpenTelemetry.Exporter.Prometheus.AspNetCore package)
        // app.MapPrometheusScrapingEndpoint();

        // All health checks must pass for app to be considered ready to accept traffic after starting
        app.MapHealthChecks("/health");

        // Only health checks tagged with the "live" tag must pass for app to be considered alive
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return app;
    }

    public static bool IsLocalDevelopment(this IHostApplicationBuilder builder)
        => builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Altinn:LocalDev");

    public static bool IsLocalDevelopment(this IServiceCollection services)
    {
        if (!services.TryFindAltinnServiceDescription(out var serviceDescription))
        {
            ThrowHelper.ThrowInvalidOperationException("Service of type AltinnServiceDescription not registered - did you forget to call AddAltinnServiceDefaults?");
        }

        return serviceDescription.IsLocalDev;
    }

    private static MeterProviderBuilder AddBuiltInMeters(this MeterProviderBuilder meterProviderBuilder) =>
        meterProviderBuilder.AddMeter(
            "Microsoft.AspNetCore.Hosting",
            "Microsoft.AspNetCore.Server.Kestrel",
            "System.Net.Http");

    private static bool TryFindAltinnServiceDescription(this IServiceCollection services, [NotNullWhen(true)] out AltinnServiceDescription? serviceDescription)
    {
        var descriptor = services.SingleOrDefault(s => s.ServiceType == typeof(AltinnServiceDescription));
        if (descriptor is null)
        {
            serviceDescription = null;
            return false;
        }

        if (descriptor.Lifetime != ServiceLifetime.Singleton)
        {
            ThrowHelper.ThrowInvalidOperationException("Service of type AltinnServiceDescription registered as non-singleton");
        }

        if (descriptor.ImplementationInstance is AltinnServiceDescription instance)
        {
            serviceDescription = instance;
            return true;
        }

        serviceDescription = null;
        return ThrowHelper.ThrowInvalidOperationException<bool>("Service of type AltinnServiceDescription registered without an instance");
    }

    public static AltinnServiceDescription GetAltinnServiceDescriptor(this IHostApplicationBuilder builder)
    {
        if (builder.Services.TryFindAltinnServiceDescription(out var serviceDescription))
        {
            return serviceDescription;
        }

        return ThrowHelper.ThrowInvalidOperationException<AltinnServiceDescription>("Service of type AltinnServiceDescription not registered - did you forget to call AddAltinnServiceDefaults?");
    }
}
