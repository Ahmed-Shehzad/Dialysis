import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  fetchRotations,
  replaceRotation,
  type OnCallRotation,
  type ChainLink,
} from "@/features/oncall/api/oncallApi";

/**
 * Operator-facing on-call rotation editor. Each row is one chair × shift assignment with
 * the primary / backup / supervisor chain. Clicking a row opens the editor drawer which
 * lets the operator edit channel targets + addresses for each chain link. Save round-trips
 * through `PUT /oncall/rotations/{id}` and invalidates the list.
 */
export const OnCallRotationPage = () => {
  const queryClient = useQueryClient();
  const [selected, setSelected] = useState<OnCallRotation | null>(null);
  const query = useQuery({
    queryKey: ["pdms", "oncall", "rotations"],
    queryFn: () => fetchRotations(),
    refetchInterval: 60_000,
  });

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["pdms", "oncall", "rotations"] });
  const rows = query.data ?? [];

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-lg font-semibold text-slate-100">On-call rotation</h1>
        <p className="text-sm text-slate-400">
          Per chair × shift assignments. Primary is paged first; backup at the policy window;
          supervisor at the next.
        </p>
      </div>

      {query.isLoading && <div className="text-sm text-slate-400">Loading rotations…</div>}
      {query.isError && (
        <div className="text-sm text-rose-300">Could not load rotations. Retry shortly.</div>
      )}
      {!query.isLoading && rows.length === 0 && (
        <div className="rounded border border-dashed border-slate-700 p-6 text-sm text-slate-400">
          No rotations configured. Alarms route to the operator's out-of-band paging tree until at
          least one is on file.
        </div>
      )}
      {rows.length > 0 && (
        <table className="w-full table-fixed border-collapse text-sm">
          <thead className="text-left text-slate-400">
            <tr>
              <th className="w-24 pb-2 font-medium">Chair</th>
              <th className="w-24 pb-2 font-medium">Shift</th>
              <th className="pb-2 font-medium">Primary</th>
              <th className="pb-2 font-medium">Backup</th>
              <th className="pb-2 font-medium">Supervisor</th>
              <th className="w-24 pb-2 font-medium">Effective</th>
            </tr>
          </thead>
          <tbody className="text-slate-200">
            {rows.map((row) => (
              <tr
                key={row.id}
                onClick={() => setSelected(row)}
                className="cursor-pointer border-t border-slate-800/60 hover:bg-slate-800/30"
              >
                <td className="py-2 align-top font-mono text-xs">{row.chairId.slice(0, 8)}</td>
                <td className="py-2 align-top">{row.shiftCode}</td>
                <td className="py-2 align-top">{row.primary.displayName}</td>
                <td className="py-2 align-top">{row.backup.displayName}</td>
                <td className="py-2 align-top">{row.supervisor.displayName}</td>
                <td className="py-2 align-top font-mono text-xs">
                  {row.effectiveFromUtc} → {row.effectiveUntilUtc}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {selected && (
        <RotationDrawer
          rotation={selected}
          onClose={() => setSelected(null)}
          onApplied={() => {
            invalidate();
            setSelected(null);
          }}
        />
      )}
    </div>
  );
};

const RotationDrawer = ({
  rotation,
  onClose,
  onApplied,
}: {
  rotation: OnCallRotation;
  onClose: () => void;
  onApplied: () => void;
}) => {
  const [primary, setPrimary] = useState<ChainLink>(rotation.primary);
  const [backup, setBackup] = useState<ChainLink>(rotation.backup);
  const [supervisor, setSupervisor] = useState<ChainLink>(rotation.supervisor);

  const mutation = useMutation({
    mutationFn: () =>
      replaceRotation(rotation.id, {
        chairId: rotation.chairId,
        shiftCode: rotation.shiftCode,
        effectiveFromUtc: rotation.effectiveFromUtc,
        effectiveUntilUtc: rotation.effectiveUntilUtc,
        primary,
        backup,
        supervisor,
      }),
    onSuccess: onApplied,
  });

  return (
    <div className="fixed inset-0 z-40 flex items-center justify-end bg-slate-950/70" role="dialog">
      <div className="h-full w-full max-w-2xl overflow-y-auto border-l border-slate-800 bg-slate-900 p-5 shadow-xl">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-slate-100">
            Chair {rotation.chairId.slice(0, 8)} · {rotation.shiftCode}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="rounded border border-slate-700 px-3 py-1.5 text-sm text-slate-200 hover:border-slate-500"
          >
            Close
          </button>
        </div>

        <LinkEditor label="Primary" link={primary} onChange={setPrimary} />
        <LinkEditor label="Backup" link={backup} onChange={setBackup} />
        <LinkEditor label="Supervisor" link={supervisor} onChange={setSupervisor} />

        {mutation.isError && (
          <div className="mt-3 text-xs text-rose-300">Save failed — retry shortly.</div>
        )}

        <div className="mt-5 flex justify-end gap-2 text-sm">
          <button
            type="button"
            onClick={onClose}
            className="rounded border border-slate-700 px-3 py-1.5 text-slate-200 hover:border-slate-500"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={() => mutation.mutate()}
            disabled={mutation.isPending}
            className="rounded bg-emerald-600 px-3 py-1.5 text-slate-50 hover:bg-emerald-500 disabled:opacity-50"
          >
            {mutation.isPending ? "Saving…" : "Save"}
          </button>
        </div>
      </div>
    </div>
  );
};

const LinkEditor = ({
  label,
  link,
  onChange,
}: {
  label: string;
  link: ChainLink;
  onChange: (next: ChainLink) => void;
}) => (
  <div className="mb-4 rounded border border-slate-800 bg-slate-900/40 p-3">
    <div className="mb-2 text-sm font-medium text-slate-200">{label}</div>
    <div className="grid grid-cols-2 gap-2 text-sm">
      <label className="block">
        <span className="text-xs text-slate-400">Clinician sub</span>
        <input
          className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-1.5 text-slate-100 font-mono text-xs"
          value={link.clinicianSub}
          onChange={(e) => onChange({ ...link, clinicianSub: e.target.value })}
        />
      </label>
      <label className="block">
        <span className="text-xs text-slate-400">Display name</span>
        <input
          className="mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-1.5 text-slate-100"
          value={link.displayName}
          onChange={(e) => onChange({ ...link, displayName: e.target.value })}
        />
      </label>
    </div>
    <div className="mt-2 text-xs text-slate-500">
      Channels:{" "}
      {link.channels.length > 0
        ? link.channels.map((c) => `${c.channel}:${c.address}`).join(", ")
        : "(none)"}
    </div>
  </div>
);

export default OnCallRotationPage;
