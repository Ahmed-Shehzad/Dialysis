using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.ReportGenerator;
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

    [Parameter("Container registry + repository prefix PushImages publishes to — e.g. mycompany.jfrog.io/dialysis-docker " +
               "(a Docker repository on JFrog Artifactory / JFrog Container Registry) or ghcr.io/myorg. " +
               "Authenticate first: `docker login <registry host>` (JFrog: username + identity token).")]
    readonly string Registry;

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
        .Description("Runs the full test suite with TRX logs + per-project Cobertura coverage into artifacts/test-results.")
        .DependsOn(Compile)
        .Produces(TestResultsDirectory / "*.trx")
        .Executes(() => DotNetTest(s => s
            .SetProjectFile(SolutionFile)
            .SetConfiguration(Configuration)
            .EnableNoBuild()
            .SetResultsDirectory(TestResultsDirectory)
            // coverlet.collector rides on every *Tests* project via Directory.Build.props.
            .SetDataCollector("XPlat Code Coverage")
            .AddLoggers("trx")));

    [Parameter("Minimum line-coverage percentage CoverageReport enforces (0–100). Default 0 = report-only; " +
               "ratchet upward once a baseline is established.")]
    readonly double MinCoverage;

    AbsolutePath CoverageReportDirectory => ArtifactsDirectory / "coverage";

    Target CoverageReport => _ => _
        .Description("Aggregates the Cobertura files Test produced into artifacts/coverage (HTML + JSON summary + " +
                     "GitHub-flavoured markdown summary) and fails if line coverage is below --min-coverage.")
        .After(Test)
        .Produces(CoverageReportDirectory / "Summary.json")
        .Executes(() =>
        {
            var coverageFiles = TestResultsDirectory.GlobFiles("**/coverage.cobertura.xml");
            if (coverageFiles.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No coverage.cobertura.xml found under {TestResultsDirectory} — run the Test target first.");
            }
            CoverageReportDirectory.CreateOrCleanDirectory();
            ReportGeneratorTasks.ReportGenerator(s => s
                .SetFramework("net9.0")
                .SetReports(coverageFiles.Select(f => f.ToString()))
                .SetTargetDirectory(CoverageReportDirectory)
                .SetReportTypes(ReportTypes.HtmlInline, ReportTypes.JsonSummary, ReportTypes.MarkdownSummaryGithub));

            // ReportGenerator's JsonSummary carries {"summary":{"linecoverage": <percent>}}.
            var summaryJson = (CoverageReportDirectory / "Summary.json").ReadAllText();
            using var document = System.Text.Json.JsonDocument.Parse(summaryJson);
            var lineCoverage = document.RootElement.GetProperty("summary").GetProperty("linecoverage").GetDouble();
            Log.Information("Line coverage: {Coverage:F1}% (gate: {Gate:F1}%)", lineCoverage, MinCoverage);
            if (lineCoverage < MinCoverage)
            {
                throw new InvalidOperationException(
                    $"Line coverage {lineCoverage:F1}% is below the --min-coverage gate of {MinCoverage:F1}%.");
            }
        });

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
    void RunAspirePublisher(string publisher, string environment, AbsolutePath output, string sentinel,
        IReadOnlyDictionary<string, string> extraEnvironment = null)
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
            if (TryPublishOnce(publisher, environment, output, sentinel, attempt, maxAttempts, extraEnvironment))
            {
                Log.Information("Generated {Files} file(s) under {Output}", output.GlobFiles("**/*").Count, output);
                return;
            }
        }

        throw new InvalidOperationException(
            $"Aspire {publisher} publisher failed to write {sentinel} for environment '{environment}' after {maxAttempts} attempts.");
    }

    bool TryPublishOnce(string publisher, string environment, AbsolutePath output, string sentinel, int attempt, int maxAttempts,
        IReadOnlyDictionary<string, string> extraEnvironment = null)
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
        // per-env switch that drives the AppHost's HSTS / replicas / OTEL shaping (plus any
        // caller-specific extras, e.g. PushImages' registry/tag qualification).
        startInfo.Environment["DIALYSIS_DEPLOY_ENV"] = environment;
        foreach (var (key, value) in extraEnvironment ?? new Dictionary<string, string>())
        {
            startInfo.Environment[key] = value;
        }

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

    Target PushImages => _ => _
        .Description("Builds every repo-built deployment image (module APIs, BFFs, gateway, SPAs) and pushes it to --registry " +
                     "(e.g. a JFrog Artifactory / JCR Docker repository), tagged with the GitVersion-derived SemVer (--version overrides). " +
                     "Also writes artifacts/images/values-images-<environment>.yaml — a Helm values override that points the committed " +
                     "deploy/charts/dialysis-<environment> chart at the pushed images (`helm install ... -f <file>`). " +
                     "Requires a running Docker daemon and a prior `docker login`. See docs/operations/container-registry.md.")
        .Requires(() => Registry)
        .Executes(() =>
        {
            var tag = PackageVersion ?? "latest";
            // Publish a scratch compose project with registry-qualified image names next to the
            // build stanzas (DIALYSIS_IMAGE_REGISTRY/_TAG → ComposePublishExtensions). The
            // committed deploy/compose/<env> folders stay registry-free; this throwaway copy is
            // the single source for what to build, what to push, and what the chart should pull.
            var scratch = TemporaryDirectory / ("push-images-" + Environment);
            RunAspirePublisher("compose", Environment, scratch, sentinel: "docker-compose.yaml",
                extraEnvironment: new Dictionary<string, string>
                {
                    ["DIALYSIS_IMAGE_REGISTRY"] = Registry,
                    ["DIALYSIS_IMAGE_TAG"] = tag,
                });

            var composeFile = scratch / "docker-compose.yaml";
            var services = ParseRegistryBuiltServices(composeFile);
            if (services.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No registry-qualified buildable services found in {composeFile} — expected image names starting with '{Registry}'.");
            }
            Log.Information("Building + pushing {Count} image(s) to {Registry} with tag {Tag}", services.Count, Registry, tag);

            // NUKE's ArgumentStringHandler double-quotes interpolated values containing spaces,
            // so a joined service list would arrive at compose as ONE argument. A bare `build`
            // builds exactly the services that carry a build stanza (our 22); pushes go one
            // service per invocation for the same quoting reason (and per-image progress).
            StartProcess("docker", $"compose -f {composeFile} build", RootDirectory)
                .AssertWaitForExit().AssertZeroExitCode();
            foreach (var (name, image) in services)
            {
                Log.Information("Pushing {Image}", image);
                StartProcess("docker", $"compose -f {composeFile} push {name}", RootDirectory)
                    .AssertWaitForExit().AssertZeroExitCode();
            }

            // Helm values override: parameters.<name>.<name>_image keys mirror the generated
            // chart's values.yaml (resource name with '-' → '_').
            var valuesFile = ArtifactsDirectory / "images" / $"values-images-{Environment}.yaml";
            valuesFile.Parent.CreateDirectory();
            var lines = new List<string>
            {
                $"# Generated by `./build.sh PushImages --registry {Registry} --environment {Environment}`.",
                $"# Points deploy/charts/dialysis-{Environment} at the pushed images:",
                $"#   helm install dialysis deploy/charts/dialysis-{Environment} -f {valuesFile}",
                "parameters:",
            };
            foreach (var (name, image) in services.OrderBy(service => service.Name))
            {
                var parameter = name.Replace('-', '_');
                lines.Add($"  {parameter}:");
                lines.Add($"    {parameter}_image: \"{image}\"");
            }
            valuesFile.WriteAllLines(lines);
            Log.Information("Wrote Helm image override → {File}", valuesFile);
        });

    // Reads the scratch compose file PushImages generated and returns every service that both
    // builds from a repo Dockerfile and is named for the target registry. The file is
    // machine-generated with stable two/four-space indentation, so a line scan is reliable —
    // and it keeps the build free of a YAML-parser dependency.
    List<(string Name, string Image)> ParseRegistryBuiltServices(AbsolutePath composeFile)
    {
        var services = new List<(string Name, string Image)>();
        string currentService = null;
        string currentImage = null;
        var currentHasBuild = false;
        void FlushCurrent()
        {
            if (currentService is not null && currentHasBuild && currentImage is not null
                && currentImage.StartsWith(Registry.TrimEnd('/') + "/", System.StringComparison.Ordinal))
            {
                services.Add((currentService, currentImage));
            }
        }
        foreach (var line in composeFile.ReadAllLines())
        {
            var serviceMatch = System.Text.RegularExpressions.Regex.Match(line, "^  ([A-Za-z0-9_.-]+):\\s*$");
            if (serviceMatch.Success)
            {
                FlushCurrent();
                currentService = serviceMatch.Groups[1].Value;
                currentImage = null;
                currentHasBuild = false;
                continue;
            }
            if (!line.StartsWith("    ", System.StringComparison.Ordinal) && line.Trim().Length > 0 && !line.StartsWith("  ", System.StringComparison.Ordinal))
            {
                // Left the `services:` block (e.g. top-level `networks:`).
                FlushCurrent();
                currentService = null;
                continue;
            }
            var imageMatch = System.Text.RegularExpressions.Regex.Match(line, "^    image: \"(.+)\"\\s*$");
            if (imageMatch.Success)
            {
                currentImage = imageMatch.Groups[1].Value;
            }
            else if (System.Text.RegularExpressions.Regex.IsMatch(line, "^    build:\\s*$"))
            {
                currentHasBuild = true;
            }
        }
        FlushCurrent();
        return services;
    }

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
