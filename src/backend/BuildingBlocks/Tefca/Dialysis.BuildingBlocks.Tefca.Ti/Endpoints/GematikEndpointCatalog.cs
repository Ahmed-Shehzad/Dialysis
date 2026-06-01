namespace Dialysis.BuildingBlocks.Tefca.Ti.Endpoints;

/// <summary>
/// Operator-selectable gematik TI environments. Per gematik convention:
/// <list type="bullet">
///   <item><see cref="Referenz"/> (RU) — Referenzumgebung, conformance-testing only.</item>
///   <item><see cref="Test"/> (TU) — Testumgebung, integration tests with synthetic patients.</item>
///   <item><see cref="Produktion"/> (PU) — Produktivumgebung; real patient data, requires
///     a valid SMC-B and operator opt-in via <c>SmartConnect:DataProtection:TiProductionMode</c>.</item>
/// </list>
/// </summary>
public enum GematikEnvironment
{
    Referenz = 0,
    Test = 1,
    Produktion = 2,
}

/// <summary>
/// URL registry for the gematik TI endpoints per environment. The endpoint base URLs are
/// hard-coded here because gematik publishes them as part of their conformance documentation;
/// operators only select the environment, never the URL.
///
/// Updates to URLs land via gematik release notes — when one changes, update the catalog
/// constants and ship a new release.
/// </summary>
public sealed record GematikEndpoint(
    GematikEnvironment Environment,
    Uri DiscoveryDocument,
    Uri EpaUpload,
    Uri EpaDownload,
    Uri TokenIssuer);

public static class GematikEndpointCatalog
{
    public static GematikEndpoint For(GematikEnvironment environment) => environment switch
    {
        GematikEnvironment.Referenz => new GematikEndpoint(
            Environment: GematikEnvironment.Referenz,
            DiscoveryDocument: new Uri("https://idp-ref.app.ti-dienste.de/.well-known/openid-configuration"),
            EpaUpload: new Uri("https://epa.ru.ti-dienste.de/api/v1/documents"),
            EpaDownload: new Uri("https://epa.ru.ti-dienste.de/api/v1/documents"),
            TokenIssuer: new Uri("https://idp-ref.app.ti-dienste.de")),

        GematikEnvironment.Test => new GematikEndpoint(
            Environment: GematikEnvironment.Test,
            DiscoveryDocument: new Uri("https://idp-test.app.ti-dienste.de/.well-known/openid-configuration"),
            EpaUpload: new Uri("https://epa.tu.ti-dienste.de/api/v1/documents"),
            EpaDownload: new Uri("https://epa.tu.ti-dienste.de/api/v1/documents"),
            TokenIssuer: new Uri("https://idp-test.app.ti-dienste.de")),

        GematikEnvironment.Produktion => new GematikEndpoint(
            Environment: GematikEnvironment.Produktion,
            DiscoveryDocument: new Uri("https://idp.app.ti-dienste.de/.well-known/openid-configuration"),
            EpaUpload: new Uri("https://epa.app.ti-dienste.de/api/v1/documents"),
            EpaDownload: new Uri("https://epa.app.ti-dienste.de/api/v1/documents"),
            TokenIssuer: new Uri("https://idp.app.ti-dienste.de")),

        _ => throw new ArgumentOutOfRangeException(nameof(environment), environment, null),
    };
}
