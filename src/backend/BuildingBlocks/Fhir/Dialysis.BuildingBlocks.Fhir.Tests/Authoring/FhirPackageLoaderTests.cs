using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using Dialysis.BuildingBlocks.Fhir.Validation;
using Dialysis.BuildingBlocks.Fhir.Validation.Authoring;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Dialysis.BuildingBlocks.Fhir.Tests.Authoring;

/// <summary>
/// Verifies loading an external FHIR package (NPM .tgz) makes its conformance resources
/// resolvable, so an IG that declares a dependency on it no longer warns.
/// </summary>
public sealed class FhirPackageLoaderTests
{
    private const string UsCoreIgUrl =
        "http://hl7.org/fhir/us/core/ImplementationGuide/hl7.fhir.us.core";

    private const string ManifestJson = """{"name":"hl7.fhir.us.core","version":"6.1.0"}""";

    private const string PatientProfileJson = """
    {"resourceType":"StructureDefinition",
     "url":"http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient",
     "name":"USCorePatient","status":"active","kind":"resource","abstract":false,
     "type":"Patient","baseDefinition":"http://hl7.org/fhir/StructureDefinition/Patient",
     "derivation":"constraint","fhirVersion":"4.0.1"}
    """;

    private const string IgJson = """
    {"resourceType":"ImplementationGuide",
     "url":"http://hl7.org/fhir/us/core/ImplementationGuide/hl7.fhir.us.core",
     "name":"USCore","status":"active","packageId":"hl7.fhir.us.core",
     "fhirVersion":["4.0.1"]}
    """;

    private static MemoryStream BuildPackageTarball()
    {
        var outer = new MemoryStream();
        using (var gz = new GZipStream(outer, CompressionLevel.Optimal, leaveOpen: true))
        using (var tar = new TarWriter(gz, leaveOpen: true))
        {
            Add(tar, "package/package.json", ManifestJson);
            Add(tar, "package/StructureDefinition-us-core-patient.json", PatientProfileJson);
            Add(tar, "package/ImplementationGuide-hl7.fhir.us.core.json", IgJson);
            Add(tar, "package/.index.json", """{"index-version":1}"""); // must be skipped
        }

        outer.Position = 0;
        return outer;
    }

    private static void Add(TarWriter tar, string name, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var entry = new PaxTarEntry(TarEntryType.RegularFile, name)
        {
            DataStream = new MemoryStream(bytes),
        };
        tar.WriteEntry(entry);
    }

    [Fact]
    public async Task Loads_Package_And_Registers_Canonicals_Async()
    {
        var services = new ServiceCollection();
        services.AddFhirArtifactAuthoring();
        var sp = services.BuildServiceProvider();
        var loader = sp.GetRequiredService<IFhirPackageLoader>();
        var registry = sp.GetRequiredService<IFhirConformanceRegistry>();

        await using var tarball = BuildPackageTarball();
        var result = await loader.LoadAsync(tarball, CancellationToken.None);

        result.PackageName.ShouldBe("hl7.fhir.us.core");
        result.PackageVersion.ShouldBe("6.1.0");
        result.Loaded.ShouldBe(2); // SD + IG; manifest + .index.json skipped
        registry.TryGet(UsCoreIgUrl, out _).ShouldBeTrue();
        registry.TryGet(
                "http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient", out _)
            .ShouldBeTrue();

        var resolved = await registry.ResolveByCanonicalUriAsync(UsCoreIgUrl);
        resolved.ShouldNotBeNull();
    }

    [Fact]
    public async Task Declared_Dependency_No_Longer_Warns_After_Loading_Package_Async()
    {
        var services = new ServiceCollection();
        services.AddFhirArtifactAuthoring();
        var sp = services.BuildServiceProvider();
        var loader = sp.GetRequiredService<IFhirPackageLoader>();
        var authoring = sp.GetRequiredService<IFhirArtifactAuthoringService>();

        await using (var tarball = BuildPackageTarball())
            await loader.LoadAsync(tarball, CancellationToken.None);

        var igSpec = new FhirImplementationGuideSpec
        {
            PackageId = "dialysis.fhir.core",
            Url = "https://dialysis.local/fhir/ImplementationGuide/dialysis-core",
            Name = "DialysisCoreIG",
            Version = "0.1.0",
            Profiles =
            [
                FhirProfileSpec.For(
                        "Observation",
                        "https://dialysis.local/fhir/StructureDefinition/dialysis-lab",
                        "DialysisLabObservation")
                    .Require("Observation.code")
                    .Build(),
            ],
            DependsOn = [new FhirIgDependency { Uri = UsCoreIgUrl, PackageId = "hl7.fhir.us.core" }],
        };

        var result = await authoring.AuthorImplementationGuideAsync(igSpec, CancellationToken.None);

        result.Verification.IsValid.ShouldBeTrue();
        result.Verification.Outcome.Issue
            .ShouldNotContain(i => i.Diagnostics != null
                && i.Diagnostics.Contains("could not be resolved", StringComparison.Ordinal));
    }
}
