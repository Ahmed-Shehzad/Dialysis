import { useQuery } from "@tanstack/react-query";
import { fetchPartners, type PartnerStatusDto } from "@/features/hie/api/hieApi";
import { humanizeError } from "@/lib/api/humanizeError";

/**
 * Operator pre-flight panel — every configured partner endpoint with a coarse "ready
 * to dispatch?" flag. The IsConfigured signal is intentionally narrow (valid absolute
 * URL on file); full TEFCA trust-anchor / IAS-JWT readiness will land here once the
 * trust-store status surfaces an inspectable interface — today
 * `TefcaTrustAnchorValidator` only exposes `Validate(cert)` which is per-request, not a
 * standing health probe.
 */
export const PartnerStatusPanel = () => {
  const partners = useQuery({
    queryKey: ["hie", "ops", "partners"],
    queryFn: fetchPartners,
    staleTime: 30_000,
  });

  return (
    <section className="space-y-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <header>
        <h3 className="text-sm font-medium text-slate-200">Partner endpoints</h3>
        <p className="text-xs text-slate-400">
          Configured outbound destinations. <span className="font-mono">isConfigured</span> is true
          when the base URL parses as an absolute URI — a quick pre-flight before going live.
        </p>
      </header>

      {partners.isLoading && <div className="text-xs text-slate-400">Loading partners…</div>}

      {partners.error && (
        <div
          role="alert"
          className="rounded-md border border-rose-700 bg-rose-900/40 p-2 text-xs text-rose-100"
        >
          {humanizeError(partners.error)}
        </div>
      )}

      {partners.data && partners.data.length === 0 && (
        <div className="rounded-md border border-dashed border-slate-700 p-3 text-xs text-slate-500">
          No partners configured. Add{" "}
          <span className="font-mono">Hie:Partners:&lt;id&gt;:BaseUrl</span> to the API host
          configuration.
        </div>
      )}

      {partners.data && partners.data.length > 0 && (
        <ul className="grid gap-2 sm:grid-cols-2">
          {partners.data.map((p) => (
            <PartnerCard key={p.partnerId} partner={p} />
          ))}
        </ul>
      )}
    </section>
  );
};

const PartnerCard = ({ partner }: { partner: PartnerStatusDto }) => {
  const tone = partner.isConfigured
    ? "border-emerald-700/60 bg-emerald-950/40"
    : "border-amber-700/60 bg-amber-950/30";
  return (
    <li className={`rounded-lg border p-3 ${tone}`}>
      <header className="flex items-baseline justify-between gap-2">
        <h4 className="font-mono text-sm font-semibold text-slate-100">{partner.partnerId}</h4>
        <span className="text-xs text-slate-300">
          {partner.isConfigured ? "Configured" : "Pending wiring"}
        </span>
      </header>
      <dl className="mt-2 space-y-0.5 text-xs">
        <div className="flex justify-between gap-2">
          <dt className="text-slate-400">Base URL</dt>
          <dd className="truncate font-mono text-slate-200" title={partner.baseUrl}>
            {partner.baseUrl || "(not set)"}
          </dd>
        </div>
        <div className="flex justify-between gap-2">
          <dt className="text-slate-400">Bearer token</dt>
          <dd className={partner.hasBearerToken ? "text-emerald-200" : "text-amber-200"}>
            {partner.hasBearerToken ? "on file" : "missing"}
          </dd>
        </div>
        <div className="flex justify-between gap-2">
          <dt className="text-slate-400">Timeout</dt>
          <dd className="text-slate-200">{partner.timeoutSeconds}s</dd>
        </div>
      </dl>
    </li>
  );
};
