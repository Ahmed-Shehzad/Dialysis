namespace Dialysis.Module.Bff.Configuration;

/// <summary>
/// Identity of one bounded-context BFF. Each context (his, ehr, pdms, …) runs its own BFF host
/// behind the shared gateway origin; <see cref="BasePath"/> path-scopes its routes and cookie so
/// the per-context sessions never collide on that single origin.
/// </summary>
public sealed class ModuleBffOptions
{
    public const string SectionName = "Bff:Module";

    /// <summary>Bounded-context slug, e.g. <c>his</c>. Lower-case, matches the backend module slug.</summary>
    public string Slug { get; set; } = "";

    /// <summary>
    /// URL prefix every route for this BFF lives under, e.g. <c>/his</c>. The gateway routes
    /// <c>{BasePath}/{**}</c> to this host without stripping the prefix, so the OIDC callback and
    /// cookie path stay consistent end-to-end. Defaults to <c>/{Slug}</c> when unset.
    /// </summary>
    public string? BasePath { get; set; }

    /// <summary>Session cookie name. Defaults to <c>Dialysis.{Slug}.Bff</c> when unset.</summary>
    public string? CookieName { get; set; }

    /// <summary>
    /// Absolute base address of the owning module API (e.g. <c>http://localhost:5288/</c>). The
    /// BFF proxies <c>{BasePath}/api/*</c> and <c>{BasePath}/hubs/*</c> here, stripping
    /// <see cref="BasePath"/> so the module receives <c>/api/v1.0/...</c> / <c>/hubs/...</c> and
    /// attaching the session's bearer token. When empty, the proxy is not mapped (auth-only BFF).
    /// </summary>
    public string ModuleApiAddress { get; set; } = "";

    /// <summary>Resolved base path: explicit <see cref="BasePath"/> or <c>/{Slug}</c>.</summary>
    public string ResolveBasePath()
    {
        if (!string.IsNullOrWhiteSpace(BasePath))
            return "/" + BasePath.Trim('/');
        if (string.IsNullOrWhiteSpace(Slug))
            throw new InvalidOperationException($"Set {SectionName}:Slug (or {SectionName}:BasePath).");
        return "/" + Slug.Trim('/');
    }

    /// <summary>Resolved cookie name: explicit <see cref="CookieName"/> or <c>Dialysis.{Slug}.Bff</c>.</summary>
    public string ResolveCookieName()
    {
        if (!string.IsNullOrWhiteSpace(CookieName))
            return CookieName;
        var slug = Slug.Trim('/');
        var pascal = string.IsNullOrEmpty(slug)
            ? "Module"
            : char.ToUpperInvariant(slug[0]) + slug[1..];
        return $"Dialysis.{pascal}.Bff";
    }
}
