using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks;

namespace BuildingBlocks.Serialization;

/// <summary>
/// JSON converter for <see cref="Enumeration{T}"/> types. Use when serializing integration events or DTOs that contain enumeration values.
/// </summary>
public sealed class EnumerationJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => FindEnumerationBaseType(typeToConvert) != null;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var enumerationBase = FindEnumerationBaseType(typeToConvert);
        if (enumerationBase == null)
            throw new InvalidOperationException($"Type '{typeToConvert.Name}' does not derive from Enumeration<T>.");

        var idType = enumerationBase.GetGenericArguments()[0];
        var converterType = typeof(EnumerationJsonConverter<,>).MakeGenericType(typeToConvert, idType);

        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private static Type? FindEnumerationBaseType(Type type)
    {
        for (var current = type; current != null; current = current.BaseType)
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Enumeration<>))
                return current;

        return null;
    }
}

public sealed class EnumerationJsonConverter<TEnumeration, TId> : JsonConverter<TEnumeration>
    where TEnumeration : Enumeration<TId>
    where TId : notnull
{
    public override TEnumeration? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var id = JsonSerializer.Deserialize<TId>(ref reader, options);
        return id is null ? throw new JsonException($"Failed to deserialize {typeof(TId).Name} for {typeof(TEnumeration).Name}.") : EnumerationJsonConverterCache<TEnumeration, TId>.FromId(id);
    }

    public override void Write(Utf8JsonWriter writer, TEnumeration? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, value.Id, options);
    }
}

internal static class EnumerationJsonConverterCache<TEnumeration, TId>
    where TEnumeration : Enumeration<TId>
    where TId : notnull
{
    private static readonly Lazy<IReadOnlyDictionary<TId, TEnumeration>> ValuesById = new(LoadValues);

    internal static TEnumeration FromId(TId id)
    {
        if (ValuesById.Value.TryGetValue(id, out var value))
            return value;

        throw new JsonException($"Unknown {typeof(TEnumeration).Name} id '{id}'.");
    }

    private static IReadOnlyDictionary<TId, TEnumeration> LoadValues()
    {
        var getAllMethod = GetGetAllMethod(typeof(TEnumeration))
            .MakeGenericMethod(typeof(TEnumeration));

        var values =
            (IEnumerable<TEnumeration>)getAllMethod.Invoke(null, null)!;

        return values.ToDictionary(value => value.Id, value => value, EqualityComparer<TId>.Default);
    }

    private static MethodInfo GetGetAllMethod(Type enumerationType)
    {
        for (var type = enumerationType; type is not null; type = type.BaseType)
        {
            var method = type.GetMethod("GetAll", BindingFlags.Public | BindingFlags.Static);
            if (method is not null)
                return method;
        }

        throw new InvalidOperationException($"No public static GetAll<T>() method found for '{enumerationType.Name}'.");
    }
}
