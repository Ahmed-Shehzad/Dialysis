using System.Text.Json;

using Cysharp.Serialization.Json;

using Transponder.Persistence.Abstractions;
using Transponder.Serialization;

namespace Transponder.Persistence.EntityFramework;

/// <summary>
/// EF Core persistence model for saga state.
/// </summary>
public sealed class SagaStateRecord
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public Ulid CorrelationId { get; set; }

    public Ulid? ConversationId { get; set; }

    public string StateType { get; set; } = string.Empty;

    public string StateData { get; set; } = string.Empty;

    public DateTimeOffset UpdatedTime { get; set; }

    public int Version { get; set; }

    internal static SagaStateRecord FromState<TState>(TState state)
        where TState : class, ISagaState
    {
        ArgumentNullException.ThrowIfNull(state);

        return new SagaStateRecord
        {
            CorrelationId = state.CorrelationId,
            ConversationId = state.ConversationId,
            StateType = typeof(TState).FullName ?? typeof(TState).Name,
            StateData = JsonSerializer.Serialize(state, SerializerOptions),
            UpdatedTime = DateTimeOffset.UtcNow,
            Version = state.Version
        };
    }

    internal TState ToState<TState>()
        where TState : class, ISagaState
    {
        return JsonSerializer.Deserialize<TState>(StateData, SerializerOptions)
            ?? throw new InvalidOperationException("Failed to deserialize saga state.");
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new UlidJsonConverter());
        options.Converters.Add(new EnumerationJsonConverterFactory());
        return options;
    }
}
