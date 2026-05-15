using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.InMemory;

/// <summary>In-memory persistence plugin for SmartConnect.</summary>
public static class SmartConnectInMemoryPersistenceExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers SmartConnect persistence using an in-memory database.</summary>
        public IServiceCollection AddSmartConnectPersistenceInMemory(
            string? databaseName = null) =>
            services.AddSmartConnectPersistence(o =>
                o.UseInMemoryDatabase(databaseName ?? "SmartConnect"));
    }
}
