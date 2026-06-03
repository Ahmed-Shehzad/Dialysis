using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

/// <summary>
/// NUKE build for the Dialysis platform. Drives the same <c>dotnet</c> steps the GitHub
/// workflows run, but as a single, locally-runnable, strongly-typed pipeline:
/// <c>Clean → Restore → Compile → Test → Pack → Publish</c>.
///
/// The solution is the XML-format <c>Dialysis.slnx</c>; every step passes its path straight to
/// the dotnet CLI (which understands <c>.slnx</c> natively) rather than NUKE's solution model,
/// so the build never depends on a third-party <c>.slnx</c> parser.
///
/// Usage: <c>./build.sh &lt;Target&gt;</c> (or <c>build.cmd</c> on Windows). Default target is
/// <see cref="Compile"/>. Run <c>./build.sh --help</c> for the full parameter list.
/// </summary>
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build — Debug locally, Release on CI by default.")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Override the package version emitted by Pack (e.g. 1.4.0 or 1.4.0-preview.1).")]
    readonly string Version;

    [Parameter("NuGet feed Publish pushes to.")]
    readonly string NuGetSource = "https://api.nuget.org/v3/index.json";

    [Parameter("NuGet API key for Publish."), Secret]
    readonly string NuGetApiKey;

    AbsolutePath SolutionFile => RootDirectory / "Dialysis.slnx";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath TestResultsDirectory => ArtifactsDirectory / "test-results";
    AbsolutePath PackagesDirectory => ArtifactsDirectory / "packages";

    Target Clean => _ => _
        .Description("Removes every bin/obj under src + tests and empties the artifacts directory.")
        .Executes(() =>
        {
            RootDirectory
                .GlobDirectories("src/**/bin", "src/**/obj", "tests/**/bin", "tests/**/obj")
                .ForEach(directory => directory.DeleteDirectory());
            ArtifactsDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Description("Restores NuGet packages for the whole solution.")
        .Executes(() => DotNetRestore(s => s.SetProjectFile(SolutionFile)));

    Target Compile => _ => _
        .Description("Builds Dialysis.slnx (warnings are errors via Directory.Build.props).")
        .DependsOn(Restore)
        .Executes(() => DotNetBuild(s => s
            .SetProjectFile(SolutionFile)
            .SetConfiguration(Configuration)
            .EnableNoRestore()));

    Target Test => _ => _
        .Description("Runs the full test suite with TRX logs into artifacts/test-results.")
        .DependsOn(Compile)
        .Produces(TestResultsDirectory / "*.trx")
        .Executes(() => DotNetTest(s => s
            .SetProjectFile(SolutionFile)
            .SetConfiguration(Configuration)
            .EnableNoBuild()
            .SetResultsDirectory(TestResultsDirectory)
            .AddLoggers("trx")));

    Target Pack => _ => _
        .Description("Packs the projects explicitly marked <IsPackable>true</IsPackable> into artifacts/packages.")
        .DependsOn(Compile)
        .Produces(PackagesDirectory / "*.nupkg")
        .Executes(() =>
        {
            // Only projects that deliberately opt in (<IsPackable>true</IsPackable>) are part of
            // the NuGet surface — packing the whole solution would also pack internal host /
            // app libraries, some of which legitimately depend on prerelease packages (NU5104).
            var packableProjects = RootDirectory
                .GlobFiles("src/**/*.csproj")
                .Where(project => File.ReadAllText(project)
                    .Contains("<IsPackable>true</IsPackable>", StringComparison.OrdinalIgnoreCase))
                .OrderBy(project => project.ToString())
                .ToList();

            Log.Information("Packing {Count} NuGet-packable project(s).", packableProjects.Count);
            PackagesDirectory.CreateOrCleanDirectory();

            DotNetPack(s =>
            {
                s = s
                    .SetConfiguration(Configuration)
                    .EnableNoBuild()
                    .EnableIncludeSymbols()
                    .SetSymbolPackageFormat(DotNetSymbolPackageFormat.snupkg)
                    .SetOutputDirectory(PackagesDirectory);
                if (!string.IsNullOrWhiteSpace(Version)) s = s.SetVersion(Version);
                return s.CombineWith(packableProjects, (settings, project) => settings.SetProject(project));
            });
        });

    Target Publish => _ => _
        .Description("Pushes the packed .nupkg files to the configured NuGet feed.")
        .DependsOn(Pack)
        .Requires(() => NuGetApiKey)
        .Executes(() =>
        {
            var packages = PackagesDirectory.GlobFiles("*.nupkg");
            if (packages.Count == 0)
            {
                Log.Warning("No packages found in {Directory} — nothing to publish.", PackagesDirectory);
                return;
            }
            DotNetNuGetPush(s => s
                .SetSource(NuGetSource)
                .SetApiKey(NuGetApiKey)
                .EnableSkipDuplicate()
                .CombineWith(packages, (settings, package) => settings.SetTargetPath(package)));
        });
}
