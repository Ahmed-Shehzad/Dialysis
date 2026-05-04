using Microsoft.Extensions.Configuration;

namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;

public static class TransponderPersistenceConfiguration
{
    /// <summary>
    /// Resolves the connection string: <see cref="TransponderPersistenceOptions.ConnectionString"/> first, then named connection from <paramref name="configuration"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">When no connection string can be resolved.</exception>
    public static string ResolveConnectionString(TransponderPersistenceOptions options, IConfiguration? configuration)
    {
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
            return options.ConnectionString!;

        if (configuration is null)
        {
            throw new InvalidOperationException(
                "Transponder persistence: ConnectionString is not set on TransponderPersistenceOptions and IConfiguration is not available. " +
                "Set ConnectionString in configuration, register IConfiguration, or call AddTransponder*Persistence with an overload that supplies the connection string.");
        }

        var name = string.IsNullOrWhiteSpace(options.ConnectionStringName)
            ? "TransponderPersistence"
            : options.ConnectionStringName;

        var cs =
            configuration.GetConnectionString(name)
            ?? configuration[$"ConnectionStrings:{name}"];

        if (string.IsNullOrWhiteSpace(cs))
        {
            throw new InvalidOperationException(
                $"Transponder persistence: no connection string found. Set TransponderPersistenceOptions.ConnectionString, or configuration key ConnectionStrings:{name}, or GetConnectionString(\"{name}\").");
        }

        return cs;
    }
}
