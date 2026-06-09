import { useQuery } from "@tanstack/react-query";
import {
  type Charge,
  type Claim,
  fetchChargeLag,
  fetchDenials,
  fetchLostCharges,
  type LostCharge,
} from "@/features/billing/api/billingApi";
import { humanizeError } from "@/lib/api/humanizeError";
import { PatientLabel } from "@/features/patients/PatientLabel";

const shortId = (id: string): string => id.slice(0, 8);
const money = (amount: number, currency: string): string => `${amount.toFixed(2)} ${currency}`;
const daysAgo = (iso: string): string => {
  const days = Math.floor((Date.now() - new Date(iso).getTime()) / 86_400_000);
  return days <= 0 ? "today" : `${days}d ago`;
};

const Section = ({
  title,
  subtitle,
  isLoading,
  error,
  empty,
  children,
}: {
  title: string;
  subtitle: string;
  isLoading: boolean;
  error: unknown;
  empty: boolean;
  children: React.ReactNode;
}) => (
  <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
    <header className="mb-2">
      <h3 className="text-sm font-medium text-slate-200">{title}</h3>
      <p className="text-xs text-slate-400">{subtitle}</p>
    </header>
    {isLoading && <p className="text-xs text-slate-400">Loading…</p>}
    {error != null && <p className="text-xs text-rose-300">{humanizeError(error)}</p>}
    {!isLoading && !error && empty && (
      <p className="rounded-md border border-dashed border-slate-700 p-3 text-xs text-slate-500">
        Nothing to work — clear.
      </p>
    )}
    {!empty && children}
  </section>
);

/**
 * Revenue-cycle worklist: the three queues a biller works to stop revenue leaking — encounters that
 * never produced a charge (lost charges), charges aging without a claim (charge lag / late filing),
 * and denied claims (appeals). Read-only views over the EHR Billing slice.
 */
export const BillingWorklistPage = () => {
  const lostCharges = useQuery({
    queryKey: ["ehr", "billing", "worklist", "lost-charges"],
    queryFn: () => fetchLostCharges({ olderThanDays: 2 }),
  });
  const chargeLag = useQuery({
    queryKey: ["ehr", "billing", "worklist", "charge-lag"],
    queryFn: () => fetchChargeLag({ olderThanDays: 7 }),
  });
  const denials = useQuery({
    queryKey: ["ehr", "billing", "worklist", "denials"],
    queryFn: () => fetchDenials(),
  });

  return (
    <div className="space-y-6">
      <header>
        <p className="text-xs uppercase tracking-wide text-slate-400">Billing</p>
        <h2 className="text-2xl font-semibold text-clinic-50">Revenue-cycle worklist</h2>
        <p className="text-xs text-slate-400">
          Lost charges, charge lag, and denials — work these to stop revenue leaking.
        </p>
      </header>

      <Section
        title={`Lost charges (${lostCharges.data?.length ?? 0})`}
        subtitle="Encounters closed > 2 days ago with no captured charge."
        isLoading={lostCharges.isLoading}
        error={lostCharges.error}
        empty={(lostCharges.data?.length ?? 0) === 0}
      >
        <ul className="divide-y divide-slate-800 text-sm">
          {(lostCharges.data ?? []).map((e: LostCharge) => (
            <li key={e.encounterId} className="grid grid-cols-12 items-center gap-2 py-2">
              <span className="col-span-4 font-mono text-xs text-slate-300" title={e.encounterId}>
                encounter {shortId(e.encounterId)}
              </span>
              <span className="col-span-5 truncate text-xs text-slate-300">
                <PatientLabel patientId={e.patientId} showMrn={false} />
              </span>
              <span className="col-span-3 text-right text-xs text-amber-300">
                closed {daysAgo(e.closedAtUtc)}
              </span>
            </li>
          ))}
        </ul>
      </Section>

      <Section
        title={`Charge lag (${chargeLag.data?.length ?? 0})`}
        subtitle="Captured charges > 7 days old not yet on a claim — late-filing risk."
        isLoading={chargeLag.isLoading}
        error={chargeLag.error}
        empty={(chargeLag.data?.length ?? 0) === 0}
      >
        <ul className="divide-y divide-slate-800 text-sm">
          {(chargeLag.data ?? []).map((c: Charge) => (
            <li key={c.chargeId} className="grid grid-cols-12 items-center gap-2 py-2">
              <span className="col-span-3 text-slate-200">CPT {c.cptCode}</span>
              <span className="col-span-4 truncate text-xs text-slate-300">
                <PatientLabel patientId={c.patientId} showMrn={false} />
              </span>
              <span className="col-span-3 text-slate-300">
                {money(c.billedAmount, c.currencyCode)}
              </span>
              <span className="col-span-2 text-right text-xs uppercase text-slate-500">
                {c.status}
              </span>
            </li>
          ))}
        </ul>
      </Section>

      <Section
        title={`Denials (${denials.data?.length ?? 0})`}
        subtitle="Claims a payer rejected (999 / 277CA) — appeal or resubmit."
        isLoading={denials.isLoading}
        error={denials.error}
        empty={(denials.data?.length ?? 0) === 0}
      >
        <ul className="divide-y divide-slate-800 text-sm">
          {(denials.data ?? []).map((c: Claim) => (
            <li key={c.claimId} className="grid grid-cols-12 items-center gap-2 py-2">
              <span className="col-span-3 text-slate-200">{c.payerCode}</span>
              <span className="col-span-3 text-slate-300">
                {money(c.billedTotal, c.currencyCode)}
              </span>
              <span className="col-span-3 font-mono text-xs text-slate-400" title={c.claimId}>
                claim {shortId(c.claimId)}
              </span>
              <span className="col-span-3 text-right text-xs uppercase text-rose-300">
                {c.status}
              </span>
            </li>
          ))}
        </ul>
      </Section>
    </div>
  );
};
