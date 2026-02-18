using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Transponder.Persistence.EntityFramework;

public class UlidToStringConverter : ValueConverter<Ulid, string>
{
    private static readonly ConverterMappingHints DefaultHints = new(size: 26);

    public UlidToStringConverter() : this(null)
    {
    }

    public UlidToStringConverter(ConverterMappingHints? mappingHints)
        : base(
            convertToProviderExpression: x => x.ToString(),
            convertFromProviderExpression: x => Ulid.Parse(x),
            mappingHints: DefaultHints.With(mappingHints))
    {
    }
}

public class NullableUlidToStringConverter : ValueConverter<Ulid?, string?>
{
    private static readonly ConverterMappingHints DefaultHints = new(size: 26);

    public NullableUlidToStringConverter() : this(null)
    {
    }

    public NullableUlidToStringConverter(ConverterMappingHints? mappingHints)
        : base(
            convertToProviderExpression: x => x.HasValue ? x.ToString() : null,
            convertFromProviderExpression: x => !string.IsNullOrWhiteSpace(x) ? Ulid.Parse(x) : null,
            mappingHints: DefaultHints.With(mappingHints))
    {
    }
}

public class UlidToBytesConverter : ValueConverter<Ulid, byte[]>
{
    private static readonly ConverterMappingHints DefaultHints = new(size: 16);

    public UlidToBytesConverter() : this(null)
    {
    }

    public UlidToBytesConverter(ConverterMappingHints? mappingHints)
        : base(
            convertToProviderExpression: x => x.ToByteArray(),
            convertFromProviderExpression: x => new Ulid(x),
            mappingHints: DefaultHints.With(mappingHints))
    {
    }
}

public class NullableUlidToBytesConverter : ValueConverter<Ulid?, byte[]?>
{
    private static readonly ConverterMappingHints DefaultHints = new(size: 16);

    public NullableUlidToBytesConverter() : this(null)
    {
    }

    public NullableUlidToBytesConverter(ConverterMappingHints? mappingHints)
        : base(
            convertToProviderExpression: x => x.HasValue ? x.Value.ToByteArray() : null,
            convertFromProviderExpression: x => x != null ? new Ulid(x) : null,
            mappingHints: DefaultHints.With(mappingHints))
    {
    }
}
