using Aspire.Hosting.Kubernetes.Resources;
using YamlDotNet.Serialization;

namespace Dialysis.AppHost;

/// <summary>
/// Per-resource decoration for the Kubernetes publisher — the k8s sibling of
/// <see cref="ComposePublishExtensions"/>. Aspire's <c>KubernetesEnvironment</c> renders one
/// Deployment/StatefulSet per resource but models neither pod resource sizing, replica counts,
/// PodDisruptionBudgets, nor NetworkPolicies; those production-hardening concerns are applied
/// here so the committed <c>deploy/charts/dialysis-&lt;env&gt;/</c> charts carry them and the
/// <c>deploy-artifacts.yml</c> drift gate keeps them in lockstep with the AppHost.
///
/// Every mutation is a no-op outside the k8s publish step: Aspire only invokes the
/// <c>PublishAsKubernetesService</c> callbacks while <c>--publisher k8s</c> is active.
/// </summary>
public static class KubernetesPublishExtensions
{
    /// <summary>Workload tiers with distinct CPU/memory envelopes in the published charts.</summary>
    public enum WorkloadTier
    {
        /// <summary>Module API host (HIS/EHR/PDMS/SmartConnect/HIE/Lab) — scaled horizontally.</summary>
        ModuleApi,

        /// <summary>Per-context or identity BFF — cookie/OIDC + YARP forward, light CPU.</summary>
        Bff,

        /// <summary>nginx serving a static SPA bundle.</summary>
        Web,

        /// <summary>The YARP edge gateway — scaled horizontally.</summary>
        Gateway,

        /// <summary>In-chart per-module Postgres StatefulSet (operators run CNPG in prod, but the chart must stand alone).</summary>
        Postgres,

        /// <summary>Shared infrastructure containers: RabbitMQ, Valkey, Keycloak.</summary>
        Infra,
    }

    /// <summary>
    /// Applies the production-hardening trio to the resource's generated workload:
    /// CPU/memory requests + limits on every (init) container, an explicit replica count for the
    /// horizontally-scaled tiers (module APIs + gateway, mirroring the compose publisher's
    /// <c>deploy.replicas</c>), and — whenever the replica count exceeds one — a
    /// PodDisruptionBudget (<c>minAvailable: 1</c>) so a node drain can never evict every replica
    /// of a clinical service at once.
    /// </summary>
    public static IResourceBuilder<T> WithKubernetesWorkloadHardening<T>(
        this IResourceBuilder<T> builder,
        WorkloadTier tier,
        string environment)
        where T : IComputeResource =>
        builder.PublishAsKubernetesService(resource =>
        {
            var hardened = DeploymentEnvironment.RequiresProductionHardening(environment);
            var (requests, limits) = Sizing(tier, hardened);
            foreach (var container in EnumerateContainers(resource.Workload))
            {
                container.Resources ??= new ResourceRequirementsV1();
                container.Resources.Requests["cpu"] = requests.Cpu;
                container.Resources.Requests["memory"] = requests.Memory;
                container.Resources.Limits["cpu"] = limits.Cpu;
                container.Resources.Limits["memory"] = limits.Memory;
            }

            if (tier is WorkloadTier.ModuleApi or WorkloadTier.Gateway
                && resource.Workload is Deployment deployment)
            {
                var replicas = DeploymentEnvironment.Replicas(environment);
                deployment.Spec ??= new DeploymentSpecV1();
                deployment.Spec.Replicas = replicas;
                if (replicas > 1)
                {
                    resource.AdditionalResources.Add(BuildPodDisruptionBudget(resource.Name));
                }
            }
        });

    /// <summary>
    /// Emits the chart's namespace-wide network segmentation (attached to the gateway resource so
    /// the policies land once per chart): a default policy that drops all ingress originating
    /// outside the release namespace (pods inside the namespace keep talking freely — the module
    /// APIs, BFFs, brokers and databases form one trust zone), plus an explicit allow that opens
    /// the gateway — the only browser-facing surface, fronted by the Ingress controller in its own
    /// namespace — to traffic from anywhere.
    /// </summary>
    public static IResourceBuilder<ProjectResource> WithKubernetesNetworkPolicies(
        this IResourceBuilder<ProjectResource> builder) =>
        builder.PublishAsKubernetesService(resource =>
        {
            var sameNamespaceOnly = new NetworkPolicy();
            sameNamespaceOnly.Metadata.Name = "deny-cross-namespace-ingress";
            sameNamespaceOnly.Spec.PolicyTypes.Add("Ingress");
            sameNamespaceOnly.Spec.Ingress.Add(new NetworkPolicyIngressRule
            {
                From = [new NetworkPolicyPeer { PodSelector = new LabelSelectorV1() }],
            });
            resource.AdditionalResources.Add(sameNamespaceOnly);

            var gatewayFromAnywhere = new NetworkPolicy();
            gatewayFromAnywhere.Metadata.Name = "allow-external-ingress-to-gateway";
            gatewayFromAnywhere.Spec.PodSelector.MatchLabels = new Dictionary<string, string>
            {
                [ComponentLabel] = resource.Name,
            };
            gatewayFromAnywhere.Spec.PolicyTypes.Add("Ingress");
            gatewayFromAnywhere.Spec.Ingress.Add(new NetworkPolicyIngressRule
            {
                From = [new NetworkPolicyPeer { NamespaceSelector = new LabelSelectorV1() }],
            });
            resource.AdditionalResources.Add(gatewayFromAnywhere);
        });

    // -------- private helpers --------

    /// <summary>Label key the Aspire k8s publisher stamps with the resource name on every pod.</summary>
    private const string ComponentLabel = "app.kubernetes.io/component";

    private static IEnumerable<ContainerV1> EnumerateContainers(Workload? workload)
    {
        if (workload?.PodTemplate?.Spec is not { } podSpec)
        {
            yield break;
        }
        foreach (var container in podSpec.Containers)
        {
            yield return container;
        }
        foreach (var container in podSpec.InitContainers)
        {
            yield return container;
        }
    }

    private static PodDisruptionBudget BuildPodDisruptionBudget(string resourceName)
    {
        var pdb = new PodDisruptionBudget();
        pdb.Metadata.Name = resourceName + "-pdb";
        pdb.Spec.MinAvailable = 1;
        pdb.Spec.Selector.MatchLabels = new Dictionary<string, string>
        {
            [ComponentLabel] = resourceName,
        };
        return pdb;
    }

    /// <summary>
    /// CPU/memory envelope per tier. Staging/prod (hardened) get clinical-grade guarantees;
    /// dev stays small so the whole chart fits a laptop-class cluster. Values are emitted as
    /// literals into the chart — operators needing different envelopes re-shape here and re-run
    /// the NUKE <c>PublishAllKubernetes</c> target (never hand-edit the chart).
    /// </summary>
    private static (ResourceEnvelope Requests, ResourceEnvelope Limits) Sizing(WorkloadTier tier, bool hardened) =>
        tier switch
        {
            WorkloadTier.ModuleApi when hardened => (new("250m", "512Mi"), new("1000m", "1Gi")),
            WorkloadTier.ModuleApi => (new("100m", "256Mi"), new("500m", "512Mi")),
            WorkloadTier.Gateway when hardened => (new("250m", "256Mi"), new("1000m", "512Mi")),
            WorkloadTier.Gateway => (new("100m", "128Mi"), new("500m", "256Mi")),
            WorkloadTier.Bff when hardened => (new("100m", "256Mi"), new("500m", "512Mi")),
            WorkloadTier.Bff => (new("50m", "128Mi"), new("250m", "256Mi")),
            WorkloadTier.Web => (new("25m", "64Mi"), new("100m", "128Mi")),
            WorkloadTier.Postgres when hardened => (new("250m", "512Mi"), new("1000m", "2Gi")),
            WorkloadTier.Postgres => (new("100m", "256Mi"), new("500m", "1Gi")),
            WorkloadTier.Infra when hardened => (new("250m", "512Mi"), new("1000m", "2Gi")),
            _ => (new("100m", "256Mi"), new("500m", "1Gi")),
        };

    private readonly record struct ResourceEnvelope(string Cpu, string Memory);

    // -------- minimal policy/v1 + networking.k8s.io/v1 models --------
    // Aspire.Hosting.Kubernetes (13.4 preview) ships no PodDisruptionBudget / NetworkPolicy
    // resource models; KubernetesResource.AdditionalResources accepts any BaseKubernetesResource,
    // so the two kinds are modeled here with exactly the fields the charts need.

    private sealed class PodDisruptionBudget : BaseKubernetesResource
    {
        public PodDisruptionBudget()
            : base("policy/v1", "PodDisruptionBudget")
        {
        }

        [YamlMember(Alias = "spec")]
        public PodDisruptionBudgetSpec Spec { get; set; } = new();
    }

    private sealed class PodDisruptionBudgetSpec
    {
        [YamlMember(Alias = "minAvailable")]
        public int? MinAvailable { get; set; }

        [YamlMember(Alias = "selector")]
        public LabelSelectorV1 Selector { get; set; } = new();
    }

    private sealed class NetworkPolicy : BaseKubernetesResource
    {
        public NetworkPolicy()
            : base("networking.k8s.io/v1", "NetworkPolicy")
        {
        }

        [YamlMember(Alias = "spec")]
        public NetworkPolicySpec Spec { get; set; } = new();
    }

    private sealed class NetworkPolicySpec
    {
        [YamlMember(Alias = "podSelector")]
        public LabelSelectorV1 PodSelector { get; set; } = new();

        [YamlMember(Alias = "policyTypes")]
        public List<string> PolicyTypes { get; } = [];

        [YamlMember(Alias = "ingress")]
        public List<NetworkPolicyIngressRule> Ingress { get; } = [];
    }

    private sealed class NetworkPolicyIngressRule
    {
        [YamlMember(Alias = "from")]
        public List<NetworkPolicyPeer>? From { get; set; }
    }

    private sealed class NetworkPolicyPeer
    {
        [YamlMember(Alias = "podSelector")]
        public LabelSelectorV1? PodSelector { get; set; }

        [YamlMember(Alias = "namespaceSelector")]
        public LabelSelectorV1? NamespaceSelector { get; set; }
    }
}
