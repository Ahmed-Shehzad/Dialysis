using Dialysis.Identity.Bff.Configuration;
using Dialysis.Identity.Bff.Federation;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Dialysis.Identity.Tests.Bff;

/// <summary>
/// Locks down the BFF identity-provider catalog: enabled-only filtering, alias whitelisting, and
/// display-name fallback. The catalog is the only gate between caller-supplied alias strings and
/// Keycloak's <c>kc_idp_hint</c>, so its <c>IsKnown</c> contract is security-relevant.
/// </summary>
public sealed class IdentityProviderCatalogTests
{
    [Fact]
    public void List_Returns_Only_Enabled_Entries_With_A_Nonblank_Alias()
    {
        var catalog = BuildCatalog(
            new IdentityProviderEntry { Alias = "okta", DisplayName = "Okta", Enabled = true },
            new IdentityProviderEntry { Alias = "auth0", DisplayName = "Auth0", Enabled = false },
            new IdentityProviderEntry { Alias = "", DisplayName = "Blank alias", Enabled = true });

        var providers = catalog.List();

        providers.Count.ShouldBe(1);
        providers[0].Alias.ShouldBe("okta");
        providers[0].DisplayName.ShouldBe("Okta");
    }

    [Fact]
    public void List_Falls_Back_To_Alias_When_Displayname_Is_Blank()
    {
        var catalog = BuildCatalog(
            new IdentityProviderEntry { Alias = "entra", DisplayName = "", Enabled = true });

        var only = catalog.List().ShouldHaveSingleItem();
        only.DisplayName.ShouldBe("entra");
    }

    [Fact]
    public void List_Normalises_Whitespace_Iconuri_To_Null()
    {
        var catalog = BuildCatalog(
            new IdentityProviderEntry { Alias = "okta", DisplayName = "Okta", IconUri = "   ", Enabled = true });

        catalog.List().ShouldHaveSingleItem().IconUri.ShouldBeNull();
    }

    [Fact]
    public void Isknown_Is_Case_Insensitive_For_Enabled_Aliases()
    {
        var catalog = BuildCatalog(
            new IdentityProviderEntry { Alias = "Okta", DisplayName = "Okta", Enabled = true });

        catalog.IsKnown("okta").ShouldBeTrue();
        catalog.IsKnown("OKTA").ShouldBeTrue();
        catalog.IsKnown("Okta").ShouldBeTrue();
    }

    [Fact]
    public void Isknown_Rejects_Disabled_Entries()
    {
        var catalog = BuildCatalog(
            new IdentityProviderEntry { Alias = "auth0", DisplayName = "Auth0", Enabled = false });

        catalog.IsKnown("auth0").ShouldBeFalse();
    }

    [Fact]
    public void Isknown_Rejects_Unconfigured_Aliases()
    {
        var catalog = BuildCatalog(
            new IdentityProviderEntry { Alias = "okta", DisplayName = "Okta", Enabled = true });

        catalog.IsKnown("malicious-broker").ShouldBeFalse();
        catalog.IsKnown("").ShouldBeFalse();
        catalog.IsKnown("   ").ShouldBeFalse();
    }

    [Fact]
    public void Empty_Configuration_Produces_An_Empty_Catalog()
    {
        var catalog = BuildCatalog();

        catalog.List().ShouldBeEmpty();
        catalog.IsKnown("okta").ShouldBeFalse();
    }

    private static ConfiguredIdentityProviderCatalog BuildCatalog(params IdentityProviderEntry[] providers)
    {
        var options = new IdentityFederationOptions { Providers = providers };
        var monitor = new StaticOptionsMonitor<IdentityFederationOptions>(options);
        return new ConfiguredIdentityProviderCatalog(monitor);
    }

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = value;
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
