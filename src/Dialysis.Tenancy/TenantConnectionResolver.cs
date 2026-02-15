using Microsoft.Extensions.Options;

namespace Dialysis.Tenancy;

public sealed class TenantConnectionOptions
{
    public const string SectionName = "Tenancy";
    public string ConnectionStringTemplate { get; set; } = "Host=localhost;Port=5432;Database=dialysis_{TenantId};Username=postgres;Password=postgres";
}

public sealed class TenantConnectionResolver : ITenantConnectionResolver
{
    private readonly TenantConnectionOptions _options;

    public TenantConnectionResolver(IOptions<TenantConnectionOptions> options)
    {
        _options = options.Value;
    }

    public string GetConnectionString(string? tenantId)
    {
        var sanitized = string.Join("", (tenantId ?? string.Empty).Where(c => char.IsLetterOrDigit(c) || c == '_'));
        if (string.IsNullOrEmpty(sanitized)) sanitized = "default";
        return _options.ConnectionStringTemplate.Replace("{TenantId}", sanitized);
    }
}
