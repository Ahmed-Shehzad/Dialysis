import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { fetchConsentsForPatient, revokeConsent, type ConsentDto } from "@/features/hie/api/hieApi";
import { humanizeError } from "@/lib/api/humanizeError";

const formatDate = (iso: string): string => {
  try {
    return new Date(iso).toLocaleDateString();
  } catch {
    return iso;
  }
};

const directionLabel = (direction: ConsentDto["direction"]): string => {
  if (typeof direction === "string") return direction;
  // Backend serialises the enum as int 0/1/2 — match the convention used in EhrChartPage.
  if (direction === 1) return "Inbound";
  if (direction === 2) return "Outbound";
  return String(direction);
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
 * Operator-facing consent listing for the HIE Exchange page. Look up a patient by id,
 * see every consent grant on file across partners, and revoke an active one in place.
 * Reuses the existing `fetchConsentsForPatient` and `revokeConsent` API surface — both
 * were previously only invoked by the EHR chart's read-only consent panel.
 */
export const ConsentAdminPanel = () => {
  const [query, setQuery] = useState("");
  const [lookupId, setLookupId] = useState<string | null>(null);
  const queryClient = useQueryClient();

  const consents = useQuery({
    queryKey: ["hie", "consents", lookupId],
    queryFn: () => fetchConsentsForPatient(lookupId as string),
    enabled: Boolean(lookupId),
  });

  const revoke = useMutation({
    mutationFn: (consentId: string) => revokeConsent(consentId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["hie", "consents", lookupId] });
    },
  });

  const handleLookup = (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = query.trim();
    if (trimmed.length === 0) return;
    setLookupId(trimmed);
  };

  return (
    <section className="space-y-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <header>
        <h3 className="text-sm font-medium text-slate-200">Consent administration</h3>
        <p className="text-xs text-slate-400">
          Cross-organisation disclosure grants. Revoke takes effect immediately on outbound
          dispatch.
        </p>
      </header>

      <form onSubmit={handleLookup} className="flex items-end gap-2">
        <label className="flex-1 text-sm">
          <span className="mb-1 block text-slate-300">Patient id</span>
          <input
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Guid…"
            className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 font-mono text-xs text-slate-100 focus:border-clinic-500 focus:outline-none"
          />
        </label>
        <button
          type="submit"
          disabled={query.trim().length === 0}
          className="rounded-md bg-clinic-600 px-3 py-1.5 text-sm font-medium text-white transition hover:bg-clinic-500 disabled:cursor-not-allowed disabled:opacity-50"
        >
          Look up
        </button>
      </form>

      {consents.isLoading && <div className="text-xs text-slate-400">Loading consents…</div>}
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
          No consent grants recorded for this patient.
        </div>
      )}

      {consents.data && consents.data.length > 0 && (
        <ul className="divide-y divide-slate-800 text-sm">
          {consents.data.map((c) => (
            <li key={c.id} className="grid grid-cols-12 items-center gap-2 py-2">
              <span className="col-span-3 truncate font-mono text-xs text-slate-300">
                {c.partnerId}
              </span>
              <span className="col-span-3 text-slate-300">{c.scope}</span>
              <span className="col-span-2 text-xs uppercase text-slate-400">
                {directionLabel(c.direction)}
              </span>
              <span className="col-span-2 text-xs text-slate-400">
                {formatDate(c.effectiveFromUtc)}
              </span>
              <span className="col-span-1">
                <StatusBadge consent={c} />
              </span>
              <span className="col-span-1 text-right">
                {isActive(c) && (
                  <button
                    type="button"
                    onClick={() => {
                      if (globalThis.confirm(`Revoke consent ${c.partnerId} / ${c.scope}?`)) {
                        revoke.mutate(c.id);
                      }
                    }}
                    disabled={revoke.isPending}
                    className="rounded-md border border-rose-700/60 px-2 py-0.5 text-xs text-rose-200 transition hover:border-rose-500 disabled:opacity-50"
                  >
                    Revoke
                  </button>
                )}
              </span>
            </li>
          ))}
        </ul>
      )}

      {revoke.error && (
        <div role="alert" className="text-xs text-rose-300">
          {humanizeError(revoke.error)}
        </div>
      )}
    </section>
  );
};
