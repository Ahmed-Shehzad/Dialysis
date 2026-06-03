using System.ComponentModel;
using Nuke.Common.Tooling;

/// <summary>Build configuration — <c>Debug</c> (local default) or <c>Release</c> (CI default).</summary>
[TypeConverter(typeof(TypeConverter<Configuration>))]
public class Configuration : Enumeration
{
    public static Configuration Debug = new() { Value = nameof(Debug) };
    public static Configuration Release = new() { Value = nameof(Release) };

    public static implicit operator string(Configuration configuration) => configuration.Value;
}
