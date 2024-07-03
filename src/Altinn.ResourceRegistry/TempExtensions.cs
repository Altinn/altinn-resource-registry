using Azure.Identity;

namespace Altinn.ResourceRegistry;

/// <summary>
/// Temporary only, to be moved to ServiceDefaults once it's been shown to work in the resource registry.
/// </summary>
internal static class TempExtensions
{
    /// <summary>
    /// Temp
    /// </summary>
    public static IHostApplicationBuilder AddDefaultConfiguration(this IHostApplicationBuilder builder)
    {
        var basePath = Path.GetDirectoryName(Directory.GetCurrentDirectory());

        Console.WriteLine($"Program // Loading Configuration from basePath={basePath}");

        builder.Configuration.SetBasePath(basePath);
        string configJsonFile1 = $"{basePath}/altinn-appsettings/altinn-dbsettings-secret.json";

        Console.WriteLine($"Loading configuration file: '{configJsonFile1}'");
        builder.Configuration.AddJsonFile(configJsonFile1, optional: true, reloadOnChange: true);

        builder.AddKeyVaultConfiguration();

        return builder;
    }

    private static IHostApplicationBuilder AddKeyVaultConfiguration(this IHostApplicationBuilder builder)
    {
        var clientId = builder.Configuration.GetValue<string>("kvSetting:ClientId");
        var tenantId = builder.Configuration.GetValue<string>("kvSetting:TenantId");
        var clientSecret = builder.Configuration.GetValue<string>("kvSetting:ClientSecret");
        var keyVaultUri = builder.Configuration.GetValue<string>("kvSetting:SecretUri");

        if (!string.IsNullOrEmpty(clientId)
            && !string.IsNullOrEmpty(tenantId)
            && !string.IsNullOrEmpty(clientSecret)
            && !string.IsNullOrEmpty(keyVaultUri))
        {
            Console.WriteLine($"// {nameof(AltinnServiceDefaultsExtensions)}.{nameof(AddKeyVaultConfiguration)}: adding config from keyvault using client-secret credentials");
            var credential = new ClientSecretCredential(
                tenantId: tenantId,
                clientId: clientId,
                clientSecret: clientSecret);
            builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), credential);
        }

        return builder;
    }
}
