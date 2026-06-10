import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  fetchCharges,
  fetchClaims,
  fetchClaimAcks,
  type Claim,
} from "@/features/billing/api/billingApi";

/**
 * Operator dashboard for revenue-cycle billing — recent charges + their parent-claim
 * status, with a per-claim drill-in panel that surfaces every 999 / 277CA
 * acknowledgement the clearinghouse has sent back. Charges still in <c>Captured</c>
 * status are unassigned; once <c>SubmitClaim</c> bundles them they show the parent
 * claim id and the claim's status appears alongside.
 */
export const BillingChargesPage = () => {
  const [chargeStatus, setChargeStatus] = useState<string>("Captured");
  const [claimStatus, setClaimStatus] = useState<string>("");
  const [drillClaimId, setDrillClaimId] = useState<string | null>(null);

  const chargesQuery = useQuery({
    queryKey: ["ehr", "billing", "charges", { status: chargeStatus }],
    queryFn: () => fetchCharges({ status: chargeStatus || undefined, take: 200 }),
    refetchInterval: 30_000,
  });

  const claimsQuery = useQuery({
    queryKey: ["ehr", "billing", "claims", { status: claimStatus }],
    queryFn: () => fetchClaims({ status: claimStatus || undefined, take: 200 }),
    refetchInterval: 30_000,
  });

  const charges = chargesQuery.data ?? [];
  const claims = claimsQuery.data ?? [];
  const claimsById = new Map<string, Claim>(claims.map((c) => [c.claimId, c]));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-lg font-semibold text-slate-100">Dialysis charges & claims</h1>
        <p className="text-sm text-slate-400">
          Captured charges flow into claims on submission. Track the claim's clearinghouse ack
          timeline in the drawer.
        </p>
      </div>

      <section className="space-y-3">
        <div className="flex items-center justify-between gap-4">
          <h2 className="text-sm font-semibold text-slate-200">Charges</h2>
          <label className="flex items-center gap-2 text-xs text-slate-300">
            Status
            <select
              value={chargeStatus}
              onChange={(e) => setChargeStatus(e.target.value)}
              className="rounded border border-slate-700 bg-slate-800/60 px-2 py-1 text-slate-100"
            >
              <option value="">All</option>
              <option value="Captured">Captured</option>
              <option value="OnClaim">OnClaim</option>
              <option value="Paid">Paid</option>
              <option value="Adjusted">Adjusted</option>
              <option value="Written">Written</option>
            </select>
          </label>
        </div>

        {chargesQuery.isLoading && <div className="text-sm text-slate-400">Loading charges…</div>}
        {chargesQuery.isError && (
          <div className="text-sm text-rose-300">Could not load charges. Retry shortly.</div>
        )}

        {!chargesQuery.isLoading && charges.length === 0 && (
          <div className="rounded border border-dashed border-slate-700 p-6 text-sm text-slate-400">
            No charges match the current filter.
          </div>
        )}

        {charges.length > 0 && (
          <table className="w-full table-fixed border-collapse text-sm">
            <thead className="text-left text-slate-400">
              <tr>
                <th className="w-44 pb-2 font-medium">Charge id</th>
                <th className="w-24 pb-2 font-medium">CPT</th>
                <th className="w-28 pb-2 font-medium">Amount</th>
                <th className="w-28 pb-2 font-medium">Status</th>
                <th className="pb-2 font-medium">Claim</th>
                <th className="pb-2 font-medium">Dx pointers</th>
              </tr>
            </thead>
            <tbody className="text-slate-200">
              {charges.map((row) => {
                const parent = row.assignedClaimId ? claimsById.get(row.assignedClaimId) : null;
                return (
                  <tr key={row.chargeId} className="border-t border-slate-800/60">
                    <td className="py-2 align-top font-mono text-xs">
                      {row.chargeId.slice(0, 8)}…
                    </td>
                    <td className="py-2 align-top font-mono text-xs">{row.cptCode}</td>
                    <td className="py-2 align-top">
                      {row.billedAmount.toFixed(2)} {row.currencyCode}
                    </td>
                    <td className="py-2 align-top">{row.status}</td>
                    <td className="py-2 align-top">
                      {row.assignedClaimId ? (
                        <button
                          type="button"
                          onClick={() => setDrillClaimId(row.assignedClaimId)}
                          className="text-emerald-300 underline-offset-2 hover:underline"
                        >
                          {parent ? parent.status : "View"}
                        </button>
                      ) : (
                        <span className="text-slate-500">—</span>
                      )}
                    </td>
                    <td className="py-2 align-top font-mono text-xs text-slate-400">
                      {row.diagnosisPointerIcd10Codes.join(", ")}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </section>

      <section className="space-y-3">
        <div className="flex items-center justify-between gap-4">
          <h2 className="text-sm font-semibold text-slate-200">Claims</h2>
          <label className="flex items-center gap-2 text-xs text-slate-300">
            Status
            <select
              value={claimStatus}
              onChange={(e) => setClaimStatus(e.target.value)}
              className="rounded border border-slate-700 bg-slate-800/60 px-2 py-1 text-slate-100"
            >
              <option value="">All</option>
              <option value="Assembled">Assembled</option>
              <option value="Submitted">Submitted</option>
              <option value="Acknowledged">Acknowledged</option>
              <option value="PartiallyPaid">PartiallyPaid</option>
              <option value="Paid">Paid</option>
              <option value="Denied">Denied</option>
              <option value="Cancelled">Cancelled</option>
            </select>
          </label>
        </div>

        {claimsQuery.isLoading && <div className="text-sm text-slate-400">Loading claims…</div>}

        {claims.length > 0 && (
          <table className="w-full table-fixed border-collapse text-sm">
            <thead className="text-left text-slate-400">
              <tr>
                <th className="w-44 pb-2 font-medium">Claim id</th>
                <th className="w-24 pb-2 font-medium">Payer</th>
                <th className="w-28 pb-2 font-medium">Status</th>
                <th className="w-28 pb-2 font-medium">Total</th>
                <th className="w-16 pb-2 font-medium">Lines</th>
                <th className="w-16 pb-2 font-medium">Acks</th>
                <th className="pb-2 font-medium">Submitted</th>
                <th className="w-20 pb-2 font-medium">Actions</th>
              </tr>
            </thead>
            <tbody className="text-slate-200">
              {claims.map((row) => (
                <tr key={row.claimId} className="border-t border-slate-800/60">
                  <td className="py-2 align-top font-mono text-xs">{row.claimId.slice(0, 8)}…</td>
                  <td className="py-2 align-top font-mono text-xs">{row.payerCode}</td>
                  <td className="py-2 align-top">{row.status}</td>
                  <td className="py-2 align-top">
                    {row.billedTotal.toFixed(2)} {row.currencyCode}
                  </td>
                  <td className="py-2 align-top">{row.chargeCount}</td>
                  <td className="py-2 align-top">{row.acknowledgementCount}</td>
                  <td className="py-2 align-top text-xs text-slate-400">
                    {row.submittedAtUtc
                      ? new Date(row.submittedAtUtc).toISOString().slice(0, 19) + "Z"
                      : "—"}
                  </td>
                  <td className="py-2 align-top">
                    <button
                      type="button"
                      onClick={() => setDrillClaimId(row.claimId)}
                      className="rounded border border-slate-700 px-2 py-1 text-xs text-slate-200 hover:border-slate-500"
                    >
                      Acks
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      {drillClaimId && (
        <AckTimelineDrawer claimId={drillClaimId} onClose={() => setDrillClaimId(null)} />
      )}
    </div>
  );
};

const verdictTone = (verdict: string): string => {
  if (verdict === "Accepted") return "text-emerald-300";
  if (verdict === "Rejected") return "text-rose-300";
  return "text-amber-300";
};

const AckTimelineDrawer = ({ claimId, onClose }: { claimId: string; onClose: () => void }) => {
  const query = useQuery({
    queryKey: ["ehr", "billing", "claims", claimId, "acks"],
    queryFn: () => fetchClaimAcks(claimId),
  });

  const acks = query.data?.acknowledgements ?? [];

  return (
    <div className="fixed inset-0 z-40 flex items-center justify-end bg-slate-950/70" role="dialog">
      <div className="h-full w-full max-w-xl overflow-y-auto border-l border-slate-800 bg-slate-900 p-5 shadow-xl">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-slate-100">Ack timeline</h2>
          <button
            type="button"
            onClick={onClose}
            className="rounded border border-slate-700 px-2 py-1 text-xs text-slate-200 hover:border-slate-500"
          >
            Close
          </button>
        </div>
        <div className="mb-3 text-xs text-slate-400 font-mono">{claimId}</div>

        {query.data && (
          <div className="mb-4 grid grid-cols-2 gap-2 text-xs text-slate-300">
            <div>
              <span className="text-slate-500">Status:</span> {query.data.status}
            </div>
            <div>
              <span className="text-slate-500">External CN:</span>{" "}
              {query.data.externalControlNumber ?? "—"}
            </div>
            <div>
              <span className="text-slate-500">Payer claim CN:</span>{" "}
              {query.data.payerClaimControlNumber ?? "—"}
            </div>
            <div>
              <span className="text-slate-500">Acknowledged at:</span>{" "}
              {query.data.acknowledgedAtUtc
                ? new Date(query.data.acknowledgedAtUtc).toISOString().slice(0, 19) + "Z"
                : "—"}
            </div>
          </div>
        )}

        {query.isLoading && <div className="text-sm text-slate-400">Loading…</div>}
        {acks.length === 0 && !query.isLoading && (
          <div className="rounded border border-dashed border-slate-700 p-6 text-sm text-slate-400">
            No acknowledgements yet — the clearinghouse hasn't responded with a 999 / 277CA.
          </div>
        )}

        {acks.length > 0 && (
          <ol className="space-y-3">
            {acks.map((a) => (
              <li
                key={a.acknowledgementId}
                className="rounded border border-slate-800 bg-slate-900/60 p-3 text-sm"
              >
                <div className="flex items-center justify-between text-xs">
                  <span className="font-mono text-slate-400">
                    {new Date(a.receivedAtUtc).toISOString().replace("T", " ").slice(0, 19)} Z
                  </span>
                  <span className={verdictTone(a.verdict)}>{a.verdict}</span>
                </div>
                <div className="mt-1 text-slate-200">
                  {a.kind}
                  {a.payerClaimControlNumber && (
                    <span className="ml-2 text-xs text-slate-500">
                      payerCN: {a.payerClaimControlNumber}
                    </span>
                  )}
                </div>
                {a.reasonCodes.length > 0 && (
                  <div className="mt-1 text-xs text-slate-400">
                    Reasons: {a.reasonCodes.join(", ")}
                  </div>
                )}
              </li>
            ))}
          </ol>
        )}
      </div>
    </div>
  );
};

export default BillingChargesPage;
