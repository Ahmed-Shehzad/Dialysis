using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Transponder.Serialization;

public sealed class EnumerationJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => FindEnumerationBaseType(typeToConvert) != null;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type? enumerationBase = FindEnumerationBaseType(typeToConvert);
        if (enumerationBase == null)
            throw new InvalidOperationException($"Type '{typeToConvert.Name}' does not derive from Enumeration<T>.");

        Type idType = enumerationBase.GetGenericArguments()[0];
        Type converterType = typeof(EnumerationJsonConverter<,>).MakeGenericType(typeToConvert, idType);

        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private static Type? FindEnumerationBaseType(Type type)
    {
        for (Type? current = type; current != null; current = current.BaseType)
            if (current.IsGenericType
                && current.GetGenericTypeDefinition().Name == "Enumeration`1"
                && string.Equals(current.Namespace, "Sol9.Core", StringComparison.Ordinal))
                return current;

        return null;
    }
}

public sealed class EnumerationJsonConverter<TEnumeration, TId> : JsonConverter<TEnumeration>
    where TId : notnull
{
    public override TEnumeration? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return default;

        TId? id = JsonSerializer.Deserialize<TId>(ref reader, options);
        return id is null ? throw new JsonException($"Failed to deserialize {typeof(TId).Name} for {typeof(TEnumeration).Name}.") : EnumerationJsonConverterCache<TEnumeration, TId>.FromId(id);
    }

    public override void Write(Utf8JsonWriter writer, TEnumeration value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        TId id = EnumerationJsonConverterCache<TEnumeration, TId>.GetId(value);
        JsonSerializer.Serialize(writer, id, options);
    }
}

internal static class EnumerationJsonConverterCache<TEnumeration, TId>
    where TId : notnull
{
    private static readonly Lazy<IReadOnlyDictionary<TId, TEnumeration>> ValuesById = new(LoadValues);
    private static readonly Lazy<Func<TEnumeration, TId>> IdAccessor = new(CreateIdAccessor);

    internal static TEnumeration FromId(TId id) => ValuesById.Value.TryGetValue(id, out TEnumeration? value) ? value : throw new JsonException($"Unknown {typeof(TEnumeration).Name} id '{id}'.");

    internal static TId GetId(TEnumeration value) => IdAccessor.Value(value);

    private static Func<TEnumeration, TId> CreateIdAccessor()
    {
        PropertyInfo? idProperty = typeof(TEnumeration).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        return idProperty == null ? throw new InvalidOperationException($"Type '{typeof(TEnumeration).Name}' does not have a public Id property.") : value => (TId)idProperty.GetValue(value)!;
    }

    private static IReadOnlyDictionary<TId, TEnumeration> LoadValues()
    {
        MethodInfo getAllMethod = GetGetAllMethod(typeof(TEnumeration)).MakeGenericMethod(typeof(TEnumeration));
        var values =
            (IEnumerable<TEnumeration>)getAllMethod.Invoke(null, null)!;

        return values.ToDictionary(GetId, value => value, EqualityComparer<TId>.Default);
    }

    private static MethodInfo GetGetAllMethod(Type enumerationType)
    {
        for (Type? type = enumerationType; type is not null; type = type.BaseType)
        {
            MethodInfo? method = type.GetMethod("GetAll", BindingFlags.Public | BindingFlags.Static);
            if (method is not null)
                return method;
        }

        throw new InvalidOperationException($"No public static GetAll<T>() method found for '{enumerationType.Name}'.");
    }
}
