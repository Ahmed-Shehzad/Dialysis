using System.Text.Json;
using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.DurableCommandBus;

/// <summary>
/// Composition-time builder for the durable command bus. Use <see cref="RegisterCommand{TCommand,TResult}"/>
/// to opt a command into the durable path; the closure built here captures the concrete CLR
/// types so the hot consumer path runs without reflection.
/// </summary>
public sealed class DurableCommandsBuilder
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly List<DurableCommandRegistration> _registrations = [];

    internal DurableCommandsBuilder(string moduleSlug) => ModuleSlug = moduleSlug;

    public string ModuleSlug { get; }

    internal IReadOnlyList<DurableCommandRegistration> Registrations => _registrations;

    /// <summary>
    /// Registers <typeparamref name="TCommand"/> as durable-routable. Builds the typed
    /// deserializer + dispatcher closures eagerly.
    /// </summary>
    /// <param name="requiredPermission">
    /// Permission the status endpoint requires before returning the row's result. Should
    /// match the command's <c>IPermissionedCommand.RequiredPermission</c> so a caller who
    /// can issue the command can also read its result.
    /// </param>
    public DurableCommandsBuilder RegisterCommand<TCommand, TResult>(string? requiredPermission = null)
        where TCommand : ICommand<TResult>
    {
        var typeKey = typeof(TCommand).FullName
            ?? throw new InvalidOperationException(
                $"{typeof(TCommand)} has no FullName; durable commands must be named types.");

        Func<string, object> deserialize = payloadJson =>
            JsonSerializer.Deserialize<TCommand>(payloadJson, _jsonOptions)
                ?? throw new JsonException($"Payload deserialized to null for {typeKey}.");

        Func<object, IServiceProvider, CancellationToken, Task<string?>> dispatch =
            async (cmd, sp, ct) =>
            {
                var gateway = sp.GetRequiredService<ICqrsGateway>();
                var result = await gateway.SendCommandAsync<TCommand, TResult>(
                    (TCommand)cmd, ct).ConfigureAwait(false);
                if (result is null)
                    return null;
                if (typeof(TResult) == typeof(string))
                    return (string)(object)result;
                return JsonSerializer.Serialize(result, typeof(TResult), _jsonOptions);
            };

        _registrations.Add(new DurableCommandRegistration(
            CommandTypeKey: typeKey,
            CommandType: typeof(TCommand),
            ResultType: typeof(TResult),
            ModuleSlug: ModuleSlug,
            RequiredPermission: requiredPermission,
            Deserialize: deserialize,
            Dispatch: dispatch));

        return this;
    }
}
