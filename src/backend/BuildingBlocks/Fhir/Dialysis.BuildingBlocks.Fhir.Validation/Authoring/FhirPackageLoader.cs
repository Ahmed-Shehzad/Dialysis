using System.Formats.Tar;
using System.IO.Compression;
using System.Text.Json;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace Dialysis.BuildingBlocks.Fhir.Validation.Authoring;

/// <summary>Summary of loading an external FHIR (NPM-style) package into the registry.</summary>
public sealed record FhirPackageLoadResult(
    string? PackageName,
    string? PackageVersion,
    int Loaded,
    int Skipped,
    IReadOnlyList<string> Canonicals);

/// <summary>
/// Loads an external FHIR package (the <c>.tgz</c> NPM tarball published by HL7 — US Core,
/// CH Core, …) into the conformance registry so declared IG dependencies resolve. The tarball is
/// gzip + tar; conformance resources live under <c>package/*.json</c>. Read with the in-box
/// <see cref="TarReader"/> / <see cref="GZipStream"/> — no package-manager dependency.
/// </summary>
public interface IFhirPackageLoader
{
    Task<FhirPackageLoadResult> LoadAsync(Stream tarball, CancellationToken cancellationToken);

    Task<FhirPackageLoadResult> LoadFileAsync(string path, CancellationToken cancellationToken);
}

/// <inheritdoc cref="IFhirPackageLoader" />
public sealed class FhirPackageLoader(IFhirConformanceRegistry registry) : IFhirPackageLoader
{
    // Modern System.Text.Json FHIR options: synchronous (no VSTHRD103) and not obsolete.
    private static readonly JsonSerializerOptions _fhirJson =
        new JsonSerializerOptions().ForFhir(ModelInfo.ModelInspector);

    public async Task<FhirPackageLoadResult> LoadFileAsync(string path, CancellationToken cancellationToken)
    {
        await using var file = File.OpenRead(path);
        return await LoadAsync(file, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FhirPackageLoadResult> LoadAsync(Stream tarball, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tarball);

        var loaded = new List<string>();
        var skipped = 0;
        string? packageName = null;
        string? packageVersion = null;

        await using var gzip = new GZipStream(tarball, CompressionMode.Decompress);
        await using var tar = new TarReader(gzip, leaveOpen: true);

        while (await tar.GetNextEntryAsync(cancellationToken: cancellationToken).ConfigureAwait(false)
               is { } entry)
        {
            if (entry.EntryType is not (TarEntryType.RegularFile or TarEntryType.V7RegularFile))
                continue;

            var name = entry.Name.Replace('\\', '/');
            if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                continue;
            if (name.EndsWith("/.index.json", StringComparison.OrdinalIgnoreCase))
                continue;

            var json = await ReadEntryAsync(entry, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json))
                continue;

            // package/package.json is the NPM manifest, not a FHIR resource.
            if (name.EndsWith("/package.json", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("package.json", StringComparison.OrdinalIgnoreCase))
            {
                (packageName, packageVersion) = TryReadManifest(json);
                continue;
            }

            // A conformance resource has a top-level "resourceType" and canonical "url"; peek
            // first (cheap) then deserialize that same element into a POCO.
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (!TryGetCanonical(doc.RootElement, out var canonical))
                {
                    skipped++;
                    continue;
                }

                var resource = JsonSerializer.Deserialize<Resource>(json, _fhirJson);
                if (resource is null)
                {
                    skipped++;
                    continue;
                }

                registry.Register(canonical, resource);
                loaded.Add(canonical);
            }
            catch (Exception ex) when (
                ex is JsonException or DeserializationFailedException or InvalidOperationException)
            {
                skipped++;
            }
        }

        return new FhirPackageLoadResult(packageName, packageVersion, loaded.Count, skipped, loaded);
    }

    private static async Task<string> ReadEntryAsync(TarEntry entry, CancellationToken ct)
    {
        if (entry.DataStream is null)
            return string.Empty;
        using var reader = new StreamReader(entry.DataStream, leaveOpen: true);
        return await reader.ReadToEndAsync(ct).ConfigureAwait(false);
    }

    private static bool TryGetCanonical(JsonElement root, out string canonical)
    {
        canonical = string.Empty;
        if (root.ValueKind != JsonValueKind.Object)
            return false;
        if (!root.TryGetProperty("resourceType", out var rt) || rt.ValueKind != JsonValueKind.String)
            return false;
        if (!root.TryGetProperty("url", out var url) || url.ValueKind != JsonValueKind.String)
            return false;
        var value = url.GetString();
        if (string.IsNullOrWhiteSpace(value))
            return false;
        canonical = value;
        return true;
    }

    private static (string? Name, string? Version) TryReadManifest(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var name = root.TryGetProperty("name", out var n) ? n.GetString() : null;
            var version = root.TryGetProperty("version", out var v) ? v.GetString() : null;
            return (name, version);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }
}
