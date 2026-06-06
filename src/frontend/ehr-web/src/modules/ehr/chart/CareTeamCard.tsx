import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  addCareTeamMember,
  CARE_TEAM_ROLES,
  type CareTeamRole,
  careTeamRoleLabel,
  DEMO_PROVIDERS,
  fetchCareTeam,
  removeCareTeamMember,
  setPrimaryCareTeamMember,
} from "@/features/care-coordination/api/careTeamApi";
import { humanizeError } from "@/lib/api/humanizeError";

const providerLabel = (id: string): string =>
  DEMO_PROVIDERS.find((p) => p.id === id)?.display ?? `provider ${id.slice(0, 8)}`;

/**
 * Care-team roster on the chart — the team-based, less-fragmented view. Lists members + roles with one
 * designated primary, and lets a coordinator add/remove members and set the primary.
 */
export const CareTeamCard = ({ patientId }: { patientId: string }) => {
  const queryClient = useQueryClient();
  const [providerId, setProviderId] = useState(DEMO_PROVIDERS[0]?.id ?? "");
  const [role, setRole] = useState<CareTeamRole>("PrimaryNephrologist");

  const team = useQuery({
    queryKey: ["ehr", "care-team", patientId],
    queryFn: () => fetchCareTeam(patientId),
    enabled: Boolean(patientId),
  });

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["ehr", "care-team", patientId] });

  const memberOnTeam = (team.data?.members ?? []).some((m) => m.providerId === providerId);

  const add = useMutation({
    mutationFn: () =>
      addCareTeamMember(patientId, {
        providerId,
        role,
        isPrimary: (team.data?.members.length ?? 0) === 0,
      }),
    onSuccess: () => void invalidate(),
  });
  const remove = useMutation({
    mutationFn: (id: string) => removeCareTeamMember(patientId, id),
    onSuccess: () => void invalidate(),
  });
  const setPrimary = useMutation({
    mutationFn: (id: string) => setPrimaryCareTeamMember(patientId, id),
    onSuccess: () => void invalidate(),
  });

  const anyError = team.error ?? add.error ?? remove.error ?? setPrimary.error;

  return (
    <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <h3 className="mb-2 text-sm font-medium text-slate-200">Care team</h3>

      {anyError && <p className="text-xs text-rose-300">{humanizeError(anyError)}</p>}

      {(team.data?.members.length ?? 0) === 0 ? (
        <p className="text-xs text-slate-500">No care team yet.</p>
      ) : (
        <ul className="divide-y divide-slate-800 text-sm">
          {team.data?.members.map((m) => (
            <li key={m.providerId} className="grid grid-cols-12 items-center gap-2 py-2">
              <span className="col-span-5 text-slate-200">{providerLabel(m.providerId)}</span>
              <span className="col-span-3 text-xs text-slate-400">{careTeamRoleLabel(m.role)}</span>
              <span className="col-span-2 text-xs">
                {m.isPrimary ? (
                  <span className="text-emerald-300">Primary</span>
                ) : (
                  <button
                    type="button"
                    onClick={() => setPrimary.mutate(m.providerId)}
                    className="text-slate-400 underline-offset-2 hover:underline"
                  >
                    Make primary
                  </button>
                )}
              </span>
              <span className="col-span-2 text-right">
                <button
                  type="button"
                  onClick={() => remove.mutate(m.providerId)}
                  className="text-xs text-rose-300 hover:underline"
                >
                  Remove
                </button>
              </span>
            </li>
          ))}
        </ul>
      )}

      <form
        onSubmit={(e) => {
          e.preventDefault();
          if (providerId && !memberOnTeam) add.mutate();
        }}
        className="mt-3 flex flex-wrap items-center gap-2"
      >
        <select
          value={providerId}
          onChange={(e) => setProviderId(e.target.value)}
          className="flex-1 rounded-md border border-slate-700 bg-slate-950 p-2 text-xs text-slate-100"
        >
          {DEMO_PROVIDERS.map((p) => (
            <option key={p.id} value={p.id}>
              {p.display}
            </option>
          ))}
        </select>
        <select
          value={role}
          onChange={(e) => setRole(e.target.value as CareTeamRole)}
          className="rounded-md border border-slate-700 bg-slate-950 p-2 text-xs text-slate-100"
        >
          {CARE_TEAM_ROLES.map((r) => (
            <option key={r} value={r}>
              {careTeamRoleLabel(r)}
            </option>
          ))}
        </select>
        <button
          type="submit"
          disabled={add.isPending || !providerId || memberOnTeam}
          className="rounded-md border border-slate-700 px-3 py-1.5 text-xs text-slate-200 transition hover:border-slate-500 disabled:opacity-50"
        >
          Add
        </button>
      </form>
    </section>
  );
};
