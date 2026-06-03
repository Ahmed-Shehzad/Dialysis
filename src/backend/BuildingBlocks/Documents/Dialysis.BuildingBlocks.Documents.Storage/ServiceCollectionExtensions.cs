using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Documents.Storage;

/// <summary>
/// DI registration helpers for <see cref="IDocumentBlobStore"/>. Hosts call one of these
/// from their composition root and bind the matching options off configuration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-memory store as the singleton <see cref="IDocumentBlobStore"/>.
    /// Suitable for tests and the Aspire dev loop.
    /// </summary>
    public static IServiceCollection AddInMemoryDocumentBlobStore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IDocumentBlobStore, InMemoryDocumentBlobStore>();
        return services;
    }

    /// <summary>
    /// Registers the filesystem-backed store. Binds <see cref="FileSystemDocumentBlobStoreOptions"/>
    /// from <paramref name="configurationSection"/>.
    /// </summary>
    public static IServiceCollection AddFileSystemDocumentBlobStore(
        this IServiceCollection services,
        IConfiguration configurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);
        services.Configure<FileSystemDocumentBlobStoreOptions>(configurationSection);
        services.AddSingleton<IDocumentBlobStore, FileSystemDocumentBlobStore>();
        return services;
    }
}
