using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace Dialysis.Configuration;

/// <summary>
/// Configuration extensions for loading secrets from Azure Key Vault.
/// Enable via <c>KeyVault__VaultUri</c> env var or <c>KeyVault:VaultUri</c> in appsettings.
/// Uses DefaultAzureCredential (Managed Identity, Azure CLI, env vars).
/// See docs/PRODUCTION-CONFIG.md.
/// </summary>
public static class KeyVaultExtensions
{
    /// <summary>
    /// Adds Azure Key Vault as a configuration source when vault URI is configured.
    /// Call early in Program.cs, e.g. before <c>var app = builder.Build();</c>.
    /// Secret names use double-dash for hierarchy (e.g. <c>ServiceBus--ConnectionString</c>).
    /// </summary>
    /// <param name="builder">Configuration builder (e.g. from WebApplication.CreateBuilder).</param>
    public static IConfigurationBuilder AddKeyVaultIfConfigured(this IConfigurationBuilder builder)
    {
        var vaultUri = Environment.GetEnvironmentVariable("KeyVault__VaultUri")
            ?? Environment.GetEnvironmentVariable("KEYVAULT__VAULTURI");
        if (string.IsNullOrWhiteSpace(vaultUri))
        {
            var temp = builder.Build();
            vaultUri = temp["KeyVault:VaultUri"];
        }
        if (string.IsNullOrWhiteSpace(vaultUri) || !Uri.TryCreate(vaultUri.Trim(), UriKind.Absolute, out var uri))
            return builder;

        builder.AddAzureKeyVault(uri, new DefaultAzureCredential());
        return builder;
    }
}
