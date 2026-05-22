import { useQuery } from "@tanstack/react-query";
import { fetchConsentsForPatient, type ConsentDto } from "@/features/hie/api/hieApi";
import { humanizeError } from "@/lib/api/humanizeError";

const directionLabel = (direction: ConsentDto["direction"]): string => {
  if (typeof direction === "string") return direction;
  // Backend serialises the enum as int 0/1/2 — match the convention used elsewhere.
  if (direction === 1) return "Inbound";
  if (direction === 2) return "Outbound";
  return "All";
};

const isActive = (consent: ConsentDto): boolean => {
  if (consent.revokedAtUtc) return false;
  if (consent.effectiveToUtc && new Date(consent.effectiveToUtc) < new Date()) return false;
  return true;
};

const StatusBadge = ({ consent }: { consent: ConsentDto }) => {
  if (consent.revokedAtUtc) {
    return (
      <span className="rounded-full border border-rose-700/70 bg-rose-950/40 px-2 py-0.5 text-xs text-rose-200">
        Revoked
      </span>
    );
  }
  if (consent.effectiveToUtc && new Date(consent.effectiveToUtc) < new Date()) {
    return (
      <span className="rounded-full border border-slate-700 bg-slate-900/40 px-2 py-0.5 text-xs text-slate-300">
        Expired
      </span>
    );
  }
  return (
    <span className="rounded-full border border-emerald-700/70 bg-emerald-950/40 px-2 py-0.5 text-xs text-emerald-200">
      Active
    </span>
  );
};

/**
 * Patient-side consent visibility panel. Surfaces every cross-organisation disclosure
 * grant on file for the signed-in patient, so they can see "who can see what". Read-only
 * here by design — revoking a consent has clinical and legal weight and routes through
 * the operator-facing ConsentAdminPanel (#38). A future iteration may add a
 * patient-initiated "request revocation" flow, which is a different aggregate (an
 * intent that triggers an operator review) rather than direct revocation.
 *
 * Uses the existing `fetchConsentsForPatient` endpoint — same data the EHR chart and
 * the operator's ConsentAdminPanel read. No new backend.
 */
export const PatientConsentsPanel = ({ patientId }: { patientId: string }) => {
  const consents = useQuery({
    queryKey: ["patient-portal", "consents", patientId],
    queryFn: () => fetchConsentsForPatient(patientId),
    staleTime: 60_000,
  });

  return (
    <section className="space-y-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <header>
        <h3 className="text-sm font-medium text-slate-200">Your data-sharing consents</h3>
        <p className="text-xs text-slate-400">
          Who the clinic shares your record with, and for what. Talk to staff to grant or revoke a
          consent.
        </p>
      </header>

      {consents.isLoading && <div className="text-xs text-slate-400">Loading your consents…</div>}

      {consents.error && (
        <div
          role="alert"
          className="rounded-md border border-rose-700 bg-rose-900/40 p-2 text-xs text-rose-100"
        >
          {humanizeError(consents.error)}
        </div>
      )}

      {consents.data && consents.data.length === 0 && (
        <div className="rounded-md border border-dashed border-slate-700 p-3 text-xs text-slate-500">
          No consents on file. Your record is not shared with any external organisation.
        </div>
      )}

      {consents.data && consents.data.length > 0 && (
        <ul className="divide-y divide-slate-800 text-sm">
          {consents.data.map((c) => (
            <li key={c.id} className="grid grid-cols-12 items-center gap-2 py-2">
              <span className="col-span-4 truncate text-slate-200" title={c.partnerId}>
                {c.partnerId}
              </span>
              <span className="col-span-3 truncate text-slate-300">{c.scope}</span>
              <span className="col-span-2 text-xs uppercase text-slate-400">
                {directionLabel(c.direction)}
              </span>
              <span className="col-span-3 text-right">
                <StatusBadge consent={c} />
                {!isActive(c) && c.revokedAtUtc && (
                  <span
                    className="ml-2 text-xs text-slate-500"
                    title={new Date(c.revokedAtUtc).toLocaleString()}
                  >
                    {new Date(c.revokedAtUtc).toLocaleDateString()}
                  </span>
                )}
              </span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
};
