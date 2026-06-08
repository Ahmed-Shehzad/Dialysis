using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tooling.ProcessTasks;

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

    [Parameter("Override the package version emitted by Pack (e.g. 1.4.0 or 1.4.0-preview.1). " +
               "When unset, the version is derived from git history by GitVersion (see GitVersion.yml).")]
    readonly string Version;

    // GitVersion is resolved lazily — only when a target actually asks for a version — and
    // tolerantly: Clean/Compile/Test never touch git history, and an environment where the
    // tool can't run (no .git directory, or a shallow CI clone) degrades to null so an explicit
    // --version still works. The GitVersion.Tool itself is pulled into the NuGet cache by the
    // PackageDownload in _build.csproj; NUKE resolves its net10.0 build from there.
    readonly Lazy<GitVersion> _gitVersion = new(ResolveGitVersionOrNull);

    GitVersion GitVersionInfo => _gitVersion.Value;

    static GitVersion ResolveGitVersionOrNull()
    {
        try
        {
            return GitVersionTasks.GitVersion(s => s
                .SetFramework("net10.0")
                .SetNoFetch(true)
                .SetProcessWorkingDirectory(RootDirectory)).Result;
        }
        catch (System.Exception exception)
        {
            Log.Warning(exception,
                "GitVersion could not derive a version (no git history, or a shallow clone?). " +
                "Falling back to --version or each project's own <Version>.");
            return null;
        }
    }

    // The version Pack stamps onto the packages: an explicit --version wins; otherwise
    // GitVersion's NuGet-compatible SemVer (e.g. 1.4.0 on a release tag, 1.4.1-ci.3 on the
    // trunk between tags, 1.4.1-<branch>.5 on a feature/claude branch). Null only when neither
    // is available, in which case Pack falls back to each project's own <Version>.
    string PackageVersion =>
        !string.IsNullOrWhiteSpace(Version) ? Version : GitVersionInfo?.SemVer;

    [Parameter("NuGet feed Publish pushes to.")]
    readonly string NuGetSource = "https://api.nuget.org/v3/index.json";

    [Parameter("NuGet API key for Publish."), Secret]
    readonly string NuGetApiKey;

    [Parameter("Deployment environment for PublishCompose — one of dev / staging / prod. Default: prod.")]
    readonly string Environment = "prod";

    static readonly string[] AllComposeEnvironments = ["dev", "staging", "prod"];

    AbsolutePath SolutionFile => RootDirectory / "Dialysis.slnx";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath TestResultsDirectory => ArtifactsDirectory / "test-results";
    AbsolutePath PackagesDirectory => ArtifactsDirectory / "packages";
    AbsolutePath AspireAppHost => RootDirectory / "src" / "aspire" / "Dialysis.AppHost" / "Dialysis.AppHost.csproj";
    AbsolutePath ComposeRootDirectory => RootDirectory / "deploy" / "compose";
    AbsolutePath ComposeOutputDirectoryFor(string env) => ComposeRootDirectory / env;
    AbsolutePath HelmChartsRootDirectory => RootDirectory / "deploy" / "charts";
    AbsolutePath HelmChartOutputDirectoryFor(string env) => HelmChartsRootDirectory / ("dialysis-" + env);

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

    Target ShowVersion => _ => _
        .Description("Prints the GitVersion-derived version for the current commit (SemVer, AssemblySemVer, InformationalVersion, branch, sha) — what Pack would stamp. Handy in CI logs and for sanity-checking a branch before tagging a release.")
        .Executes(() =>
        {
            // Fail-fast gate: unlike Pack (which can fall back to --version / per-project <Version>),
            // ShowVersion's whole job is to surface the git-derived version, so a null here is an
            // error worth stopping on. Running this target first in CI (`./build.sh ShowVersion Test
            // Pack`) makes a broken git state — e.g. a shallow clone without fetch-depth: 0 — fail
            // loudly up front instead of silently shipping mis-versioned packages.
            var gitVersion = Assert.NotNull(GitVersionInfo,
                "GitVersion could not derive a version. Ensure this is a git checkout with full history " +
                "(CI must check out with fetch-depth: 0), or pass --version to set one explicitly.");

            Log.Information("SemVer                {Value}", gitVersion.SemVer);
            Log.Information("FullSemVer            {Value}", gitVersion.FullSemVer);
            Log.Information("MajorMinorPatch       {Value}", gitVersion.MajorMinorPatch);
            Log.Information("AssemblySemVer        {Value}", gitVersion.AssemblySemVer);
            Log.Information("InformationalVersion  {Value}", gitVersion.InformationalVersion);
            Log.Information("BranchName            {Value}", gitVersion.BranchName);
            Log.Information("Sha                   {Value}", gitVersion.Sha);
        });

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

            var packageVersion = PackageVersion;
            Log.Information("Packing {Count} NuGet-packable project(s) at version {Version}.",
                packableProjects.Count, packageVersion ?? "(per-project <Version>)");
            PackagesDirectory.CreateOrCleanDirectory();

            DotNetPack(s =>
            {
                s = s
                    .SetConfiguration(Configuration)
                    .EnableNoBuild()
                    .EnableIncludeSymbols()
                    .SetSymbolPackageFormat(DotNetSymbolPackageFormat.snupkg)
                    .SetOutputDirectory(PackagesDirectory)
                    // NU5123 is a MAX_PATH advisory: GitVersion gives feature/claude branches a long
                    // pre-release label (1.0.0-<branch>.n) which, on top of this repo's deliberately
                    // descriptive assembly names, pushes a package's eventual restore path over the
                    // ~200-char threshold. It is non-actionable for net10 consumers (long-path
                    // support is universal) and the codes are warnings-as-errors repo-wide — relax it
                    // for packaging only. Pack runs --no-build, so no compiler warnings are affected.
                    .SetProperty("NoWarn", "NU5123");
                if (!string.IsNullOrWhiteSpace(packageVersion)) s = s.SetVersion(packageVersion);
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

    Target PublishCompose => _ => _
        .Description("Regenerates one deployment-environment compose project from the Aspire AppHost. Default is prod; pick the shape with --environment dev|staging|prod. Output: deploy/compose/<environment>/docker-compose.yaml + .env + aspire-manifest.json — self-contained, overlay-free.")
        .Produces(ComposeOutputDirectoryFor("{environment}") / "docker-compose.yaml")
        .Produces(ComposeOutputDirectoryFor("{environment}") / ".env")
        .Executes(() => PublishComposeForEnvironment(Environment));

    Target PublishAllCompose => _ => _
        .Description("Fans PublishCompose out across every supported environment (dev / staging / prod). Use this before committing any AppHost change so the three folders stay in lockstep.")
        .Executes(() =>
        {
            foreach (var env in AllComposeEnvironments)
            {
                PublishComposeForEnvironment(env);
            }
        });

    void PublishComposeForEnvironment(string environment)
        => RunAspirePublisher("compose", environment, ComposeOutputDirectoryFor(environment),
            sentinel: "docker-compose.yaml");

    // Regenerates one environment's artifact, with retries (see below). Aspire 13.4(-preview) does
    // not exit cleanly: --deploy false fails to suppress a post-publish step, so the k8s publisher
    // crashes (duplicate DeploymentTargetAnnotation — see the AppHost workaround) and the
    // production-hardened compose envs HANG. In the common case the artifact is fully written before
    // that, so TryPublishOnce treats "sentinel on disk" as success; when the race crashes the
    // pipeline before the chart is written, the retry loop runs it again.
    void RunAspirePublisher(string publisher, string environment, AbsolutePath output, string sentinel)
    {
        if (Array.IndexOf(AllComposeEnvironments, environment) < 0)
        {
            throw new ArgumentException(
                $"Unsupported --environment '{environment}'. Expected one of: {string.Join(", ", AllComposeEnvironments)}.");
        }

        // The Aspire 13.4-preview k8s publisher fails non-deterministically: concurrent pipeline
        // steps (push-prereq / validate-build-only-container-references) race on the shared resource
        // model ("Sequence contains more than one matching element" / "Collection was modified") and
        // sometimes crash BEFORE the chart is written. The race is timing-dependent, so a fresh
        // attempt usually succeeds. Retry a few times; compose takes the same path for uniformity.
        const int maxAttempts = 4;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (TryPublishOnce(publisher, environment, output, sentinel, attempt, maxAttempts))
            {
                Log.Information("Generated {Files} file(s) under {Output}", output.GlobFiles("**/*").Count, output);
                return;
            }
        }

        throw new InvalidOperationException(
            $"Aspire {publisher} publisher failed to write {sentinel} for environment '{environment}' after {maxAttempts} attempts.");
    }

    bool TryPublishOnce(string publisher, string environment, AbsolutePath output, string sentinel, int attempt, int maxAttempts)
    {
        output.CreateOrCleanDirectory();
        var sentinelPath = output / sentinel;
        Log.Information("Running Aspire {Publisher} publisher (environment={Environment}, attempt {Attempt}/{Max}) → {Output}",
            publisher, environment, attempt, maxAttempts, output);

        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = RootDirectory,
            UseShellExecute = false,
        };
        foreach (var argument in new string[]
                 {
                     "run", "--project", AspireAppHost, "--no-launch-profile",
                     "--configuration", Configuration.ToString(), "--",
                     "--publisher", publisher, "--output-path", output, "--deploy", "false",
                 })
        {
            startInfo.ArgumentList.Add(argument);
        }
        // ProcessStartInfo seeds Environment from the parent (complete PATH); just overlay the
        // per-env switch that drives the AppHost's HSTS / replicas / OTEL shaping.
        startInfo.Environment["DIALYSIS_DEPLOY_ENV"] = environment;

        using var process = System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the Aspire publisher process.");

        // Both publishers hang or crash AFTER fully writing their output, so we watch the output
        // directory's aggregate signature (file count + total bytes) and stop the process once it
        // has stopped growing — generation is done. This covers compose (one file) and the k8s
        // chart tree (~hundreds of files) identically: stablePolls only reaches the threshold once
        // no file has been added or grown for that many seconds. The 10-minute deadline is a
        // backstop for the build + generate; the sentinel-exists check below is the success gate.
        var deadline = System.DateTime.UtcNow.AddMinutes(10);
        (int Count, long Bytes) lastSignature = (-1, -1);
        var stablePolls = 0;
        while (!process.HasExited)
        {
            if (sentinelPath.FileExists())
            {
                var files = output.GlobFiles("**/*");
                (int Count, long Bytes) signature = (files.Count, files.Sum(file => new System.IO.FileInfo(file).Length));
                stablePolls = signature == lastSignature ? stablePolls + 1 : 0;
                lastSignature = signature;
                if (stablePolls >= 4)
                {
                    Log.Information("{Output} settled at {Count} file(s); stopping the publisher (it would otherwise hang).",
                        output, signature.Count);
                    TryKill(process);
                    break;
                }
            }
            if (System.DateTime.UtcNow > deadline)
            {
                Log.Warning("Aspire {Publisher} publisher exceeded the 10-minute deadline; stopping it.", publisher);
                TryKill(process);
                break;
            }
            System.Threading.Thread.Sleep(1000);
        }

        // The artifact on disk — not the exit code — is the success criterion: the publisher
        // routinely exits non-zero (k8s crash) or is killed (compose hang) after writing it. A
        // missing sentinel means this attempt lost the pipeline race; the caller retries.
        if (!sentinelPath.FileExists())
        {
            Log.Warning("Aspire {Publisher} publisher attempt {Attempt}/{Max} did not produce {Sentinel} (Aspire 13.4-preview pipeline race); retrying.",
                publisher, attempt, maxAttempts, sentinel);
            return false;
        }

        return true;
    }

    static void TryKill(System.Diagnostics.Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (System.InvalidOperationException)
        {
            // Raced with the process exiting on its own — nothing to kill.
        }
    }

    Target PublishKubernetes => _ => _
        .Description("Regenerates one deployment-environment Helm chart from the Aspire AppHost via `--publisher k8s`. Default is prod; pick the shape with --environment dev|staging|prod. Output: deploy/charts/dialysis-<environment>/ — a complete Helm chart (Chart.yaml + values.yaml + templates/) renderable on any Kubernetes cluster.")
        .Produces(HelmChartOutputDirectoryFor("{environment}") / "Chart.yaml")
        .Produces(HelmChartOutputDirectoryFor("{environment}") / "values.yaml")
        .Executes(() => PublishKubernetesForEnvironment(Environment));

    Target PublishAllKubernetes => _ => _
        .Description("Fans PublishKubernetes out across every supported environment (dev / staging / prod). Run before committing any AppHost change so the three charts stay in lockstep.")
        .Executes(() =>
        {
            foreach (var env in AllComposeEnvironments)
            {
                PublishKubernetesForEnvironment(env);
            }
        });

    Target PublishDeployArtifacts => _ => _
        .Description("Regenerates EVERY deployment artifact in one shot — compose projects and Helm charts for all three environments (dev / staging / prod) — from the Aspire AppHost. Run this after any AppHost change, then commit deploy/. The deploy-artifacts CI gate fails the PR if these drift from the AppHost.")
        .DependsOn(PublishAllCompose, PublishAllKubernetes);

    Target InstallGitHooks => _ => _
        .Description("Points git's core.hooksPath at the tracked .githooks/ directory so the repo's pre-commit guard runs. Run once after cloning.")
        .Executes(() =>
        {
            StartProcess("git", "config core.hooksPath .githooks", RootDirectory).AssertZeroExitCode();
            Log.Information("core.hooksPath set to .githooks — the pre-commit deploy-artifact guard is now active.");
        });

    void PublishKubernetesForEnvironment(string environment)
        => RunAspirePublisher("k8s", environment, HelmChartOutputDirectoryFor(environment),
            sentinel: "Chart.yaml");
}
