namespace Dialysis.AppHost;

/// <summary>
/// Opt-in container-registry qualification for the published compose artifacts (JFrog
/// Artifactory / JFrog Container Registry, or any OCI registry). When <see cref="RegistryEnvVar"/>
/// is set on the publish invocation, every service the deployment stack builds from a repo
/// Dockerfile (module APIs, BFFs, gateway, SPAs) additionally gets an
/// <c>image: &lt;registry&gt;/&lt;service&gt;:&lt;tag&gt;</c> name next to its <c>build:</c> stanza — so
/// <c>docker compose build</c> tags the full image set for the registry and
/// <c>docker compose push</c> publishes it. The committed <c>deploy/compose/&lt;env&gt;/</c> folders
/// are always generated <b>without</b> these variables (the <c>deploy-artifacts.yml</c> drift gate
/// regenerates with a bare environment), so they stay registry-free; registry-qualified output is
/// produced on demand by the NUKE <c>PushImages</c> target into a scratch folder. That target also
/// derives a Helm values override from the scratch compose file so <c>helm install -f</c> points
/// the committed charts at the pushed images.
/// </summary>
public static class ContainerRegistryPublishExtensions
{
    /// <summary>
    /// Registry + repository prefix, e.g. <c>mycompany.jfrog.io/dialysis-docker</c> (a Docker
    /// repository on Artifactory / JFrog Container Registry) or <c>ghcr.io/myorg</c>.
    /// </summary>
    public const string RegistryEnvVar = "DIALYSIS_IMAGE_REGISTRY";

    /// <summary>Image tag; defaults to <c>latest</c>. NUKE passes the GitVersion-derived SemVer.</summary>
    public const string TagEnvVar = "DIALYSIS_IMAGE_TAG";

    /// <summary>
    /// The registry-qualified image name for a repo-built compose service, or <c>null</c> when
    /// <see cref="RegistryEnvVar"/> is unset (the committed, registry-free artifact shape).
    /// </summary>
    public static string? QualifiedImageName(string serviceName)
    {
        var registry = Environment.GetEnvironmentVariable(RegistryEnvVar);
        if (string.IsNullOrWhiteSpace(registry))
        {
            return null;
        }
        var tag = Environment.GetEnvironmentVariable(TagEnvVar);
        return $"{registry.TrimEnd('/')}/{serviceName}:{(string.IsNullOrWhiteSpace(tag) ? "latest" : tag)}";
    }
}
