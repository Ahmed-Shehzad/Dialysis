using Hl7.Fhir.Specification.Source;

namespace Dialysis.BuildingBlocks.Fhir.Validation.Authoring;

/// <summary>
/// Resolves the bundled FHIR R4 core conformance package (<c>specification.zip</c>, shipped by
/// <c>Hl7.Fhir.Specification.Data.R4</c>). Firely ships the zip as <c>contentFiles</c>, which does
/// not reliably copy to output across SDK project types, so we probe the standard locations rather
/// than assuming it sits next to the executing assembly.
/// </summary>
public static class CoreSpecificationSource
{
    // ZipSource extracts specification.zip into a shared /tmp cache on first use. Multiple
    // instances extracting concurrently race ("Directory not empty"), so the resolver is
    // memoized process-wide: one ZipSource, one extraction, shared by every registry.
    private static readonly Lazy<IAsyncResourceResolver> _shared =
        new(Build, LazyThreadSafetyMode.ExecutionAndPublication);

    public static IAsyncResourceResolver Create() => _shared.Value;

    private static IAsyncResourceResolver Build()
    {
        // Fast path: zip already beside the executing assembly (normal published layout).
        try
        {
            return ZipSource.CreateValidationSource();
        }
        catch (FileNotFoundException)
        {
            // Fall through to explicit probing.
        }

        var path = LocateSpecificationZip()
            ?? throw new FileNotFoundException(
                "Could not locate 'specification.zip'. Ensure the Hl7.Fhir.Specification.Data.R4 " +
                "package is restored, or copy specification.zip next to the application binaries.");

        return new ZipSource(path);
    }

    private static string? LocateSpecificationZip()
    {
        const string fileName = "specification.zip";

        var probes = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, fileName),
        };

        var conformanceAssemblyDir = Path.GetDirectoryName(typeof(ZipSource).Assembly.Location);
        if (!string.IsNullOrEmpty(conformanceAssemblyDir))
            probes.Add(Path.Combine(conformanceAssemblyDir, fileName));

        foreach (var probe in probes)
        {
            if (File.Exists(probe))
                return probe;
        }

        return LocateInNuGetCache(fileName);
    }

    private static string? LocateInNuGetCache(string fileName)
    {
        var packagesRoot = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (string.IsNullOrEmpty(packagesRoot))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(home))
                return null;
            packagesRoot = Path.Combine(home, ".nuget", "packages");
        }

        var dataPackage = Path.Combine(packagesRoot, "hl7.fhir.specification.data.r4");
        if (!Directory.Exists(dataPackage))
            return null;

        // Prefer the newest restored version directory.
        foreach (var versionDir in Directory.GetDirectories(dataPackage)
                     .OrderByDescending(d => d, StringComparer.Ordinal))
        {
            var candidate = Path.Combine(versionDir, "contentFiles", "any", "any", fileName);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }
}
