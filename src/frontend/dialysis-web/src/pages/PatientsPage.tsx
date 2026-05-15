import { useState } from "react";
import { Link } from "react-router-dom";
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { searchEhrPatients, type EhrPatient } from "@/features/ehr/api/ehrApi";

export const PatientsPage = () => {
  const [query, setQuery] = useState("");

  const { data, isLoading, error } = useQuery<EhrPatient[]>({
    queryKey: ["ehr", "patients", query],
    queryFn: () => searchEhrPatients(query || undefined, 50),
    placeholderData: keepPreviousData,
  });

  return (
    <div className="space-y-4">
      <header>
        <h2 className="text-xl font-semibold text-clinic-50">Patients</h2>
        <p className="text-sm text-slate-400">
          Search the EHR patient registry by name or medical record number.
        </p>
      </header>

      <input
        type="search"
        autoFocus
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        placeholder="Name or MRN…"
        className="w-full max-w-md rounded-md border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-100 placeholder-slate-500 focus:border-clinic-500 focus:outline-none"
      />

      {error && (
        <div className="rounded-md border border-rose-700 bg-rose-900/40 p-3 text-rose-100">
          EHR unavailable.
        </div>
      )}
      {isLoading && <div className="text-sm text-slate-400">Searching…</div>}

      {data && (
        <div className="overflow-hidden rounded-lg border border-slate-800">
          <table className="min-w-full divide-y divide-slate-800 text-sm">
            <thead className="bg-slate-900/80 text-xs uppercase text-slate-400">
              <tr>
                <th className="px-3 py-2 text-left">MRN</th>
                <th className="px-3 py-2 text-left">Name</th>
                <th className="px-3 py-2 text-left">DOB</th>
                <th className="px-3 py-2 text-left">Sex</th>
                <th className="px-3 py-2 text-left">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/70 bg-slate-900/40">
              {data.map((p) => (
                <tr key={p.id} className="hover:bg-slate-800/40">
                  <td className="px-3 py-2 font-mono text-xs text-slate-300">{p.medicalRecordNumber}</td>
                  <td className="px-3 py-2">
                    <Link to={`/patients/${p.id}`} className="text-clinic-50 hover:underline">
                      {p.familyName}, {p.givenName}
                    </Link>
                  </td>
                  <td className="px-3 py-2 text-slate-400">{p.dateOfBirth}</td>
                  <td className="px-3 py-2 text-slate-400">{p.sexAtBirthCode ?? "—"}</td>
                  <td className="px-3 py-2 text-slate-400">{p.status}</td>
                </tr>
              ))}
              {data.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-3 py-6 text-center text-slate-400">
                    No patients matched.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};
