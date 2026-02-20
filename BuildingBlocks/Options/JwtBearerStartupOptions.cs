namespace BuildingBlocks.Options;

/// <summary>
/// Options for startup validation of JWT Bearer configuration.
/// Validates Authority is set when not in Development.
/// </summary>
public sealed class JwtBearerStartupOptions
{
    public const string SectionName = "Authentication:JwtBearer";

    public string? Authority { get; set; }

    public string Audience { get; set; } = "api://dialysis-pdms";
}
