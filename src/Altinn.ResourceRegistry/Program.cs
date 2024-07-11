using Altinn.ResourceRegistry;

WebApplication app = ResourceRegistryHost.Create(args);

app.AddDefaultAltinnMiddleware(errorHandlingPath: "/resourceregistry/api/v1/error");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultAltinnEndpoints();
app.MapControllers();

app.Run();

/// <summary>
/// Startup class.
/// </summary>
public partial class Program 
{
}
