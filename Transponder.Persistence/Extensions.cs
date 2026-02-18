using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence;

/// <summary>
/// Extension methods to register in-memory persistence services.
/// </summary>
public static class Extensions
{
    public static IServiceCollection AddTransponderInMemoryPersistence(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<InMemoryInboxStore>();
        services.TryAddSingleton<InMemoryOutboxStore>();
        services.TryAddSingleton<InMemoryScheduledMessageStore>();

        services.TryAddSingleton<IInboxStore>(sp => sp.GetRequiredService<InMemoryInboxStore>());
        services.TryAddSingleton<IOutboxStore>(sp => sp.GetRequiredService<InMemoryOutboxStore>());
        services.TryAddSingleton<IScheduledMessageStore>(sp => sp.GetRequiredService<InMemoryScheduledMessageStore>());

        services.TryAddSingleton<IStorageSessionFactory>(sp =>
            new InMemoryStorageSessionFactory(
                sp.GetRequiredService<InMemoryInboxStore>(),
                sp.GetRequiredService<InMemoryOutboxStore>()));

        return services;
    }
}
