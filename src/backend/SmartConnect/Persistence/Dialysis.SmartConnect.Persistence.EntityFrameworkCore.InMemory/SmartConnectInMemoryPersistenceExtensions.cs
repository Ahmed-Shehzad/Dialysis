using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.InMemory;

/// <summary>In-memory persistence plugin for SmartConnect.</summary>
public static class SmartConnectInMemoryPersistenceExtensions
{
    /// <summary>Registers SmartConnect persistence using an in-memory database.</summary>
    public static IServiceCollection AddSmartConnectPersistenceInMemory(
        this IServiceCollection services,
        string? databaseName = null) =>
        services.AddSmartConnectPersistence(o =>
            o.UseInMemoryDatabase(databaseName ?? "SmartConnect"));
}
