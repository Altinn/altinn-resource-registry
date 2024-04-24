using Altinn.ResourceRegistry.AppHost;
using Aspire.Hosting.Lifecycle;
using System.Diagnostics;

var builder = DistributedApplication.CreateBuilder(args);

var dbServer = builder.AddPostgresContainer("db", port: 11059, password: "altinn-db")
    .WithPersistentData("altinn-db")
    .WithOtlpExporter();

var resourceRegistryDb = dbServer.WithAltinnDatabase("resource-registry");
var registerDb = dbServer.WithAltinnDatabase("register");

//var registerApi = builder.AddAltinnProject(
//    "register",
//    "ghcr.io/altinn/register/test:latest",
//    "altinn-register",
//    "src/Altinn.Register.csproj")
//    .WithReference(registerDb);

var registerApi = builder.AddProject<Projects.Altinn_Register>("register")
    .WithReference(registerDb);

builder.AddProject<Projects.Altinn_ResourceRegistry>("resource-registry")
    .WithReference(resourceRegistryDb)
    .WithReference(registerApi);

builder.Services.AddLifecycleHook<Lifecycle>();

builder.Build().Run();

class Lifecycle : IDistributedApplicationLifecycleHook
{
    /// <summary>
    /// Executes before the distributed application starts.
    /// </summary>
    /// <param name="appModel">The distributed application model.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes after the orchestrator allocates endpoints for resources in the application model.
    /// </summary>
    /// <param name="appModel">The distributed application model.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
