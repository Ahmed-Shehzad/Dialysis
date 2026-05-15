import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import {
  searchEhrPatientsPage,
  type PatientSearchFilters,
} from "@/features/ehr/api/ehrApi";

const PAGE_SIZES = [10, 25, 50, 100];

type FormFilters = {
  q: string;
  familyName: string;
  givenName: string;
  mrn: string;
  dobFrom: string;
  dobTo: string;
  sex: "" | "male" | "female" | "other" | "unknown";
  status: "" | "Active" | "Inactive" | "Deceased" | "Merged";
};

const emptyFilters: FormFilters = {
  q: "",
  familyName: "",
  givenName: "",
  mrn: "",
  dobFrom: "",
  dobTo: "",
  sex: "",
  status: "",
};

const useDebouncedValue = <T,>(value: T, delayMs = 300): T => {
  const [v, setV] = useState(value);
  useEffect(() => {
    const id = globalThis.setTimeout(() => setV(value), delayMs);
    return () => globalThis.clearTimeout(id);
  }, [value, delayMs]);
  return v;
};

const toApiFilters = (form: FormFilters, skip: number, take: number): PatientSearchFilters => ({
  q: form.q || undefined,
  familyName: form.familyName || undefined,
  givenName: form.givenName || undefined,
  mrn: form.mrn || undefined,
  dobFrom: form.dobFrom || undefined,
  dobTo: form.dobTo || undefined,
  sex: form.sex || undefined,
  status: form.status || undefined,
  skip,
  take,
});

const STATUS_CLASSES: Record<string, string> = {
  Active: "bg-emerald-700 text-white",
  Deceased: "bg-slate-700 text-slate-200",
  Merged: "bg-amber-700 text-white",
};

const statusBadge = (status: string) => {
  const cls = STATUS_CLASSES[status] ?? "bg-slate-600 text-slate-200";
  return <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${cls}`}>{status}</span>;
};

const inputClass =
  "w-full rounded-md border border-slate-700 bg-slate-900 px-2 py-1.5 text-sm text-slate-100 focus:border-clinic-500 focus:outline-none";
const labelClass = "block text-[10px] uppercase tracking-wide text-slate-400";

export const PatientsPage = () => {
  const [form, setForm] = useState<FormFilters>(emptyFilters);
  const [pageSize, setPageSize] = useState(25);
  const [pageIndex, setPageIndex] = useState(0);

  const debounced = useDebouncedValue(form, 300);

  // Reset to first page whenever filters change.
  useEffect(() => {
    setPageIndex(0);
  }, [debounced]);

  const skip = pageIndex * pageSize;
  const apiFilters = useMemo(() => toApiFilters(debounced, skip, pageSize), [debounced, skip, pageSize]);

  const { data, isLoading, isFetching, error } = useQuery({
    queryKey: ["ehr", "patients", "page", apiFilters],
    queryFn: () => searchEhrPatientsPage(apiFilters),
    placeholderData: keepPreviousData,
  });

  const update = <K extends keyof FormFilters>(key: K, value: FormFilters[K]) =>
    setForm((f) => ({ ...f, [key]: value }));

  const total = data?.totalCount ?? 0;
  const pageCount = Math.max(1, Math.ceil(total / pageSize));
  const showingFrom = total === 0 ? 0 : skip + 1;
  const showingTo = Math.min(total, skip + (data?.items.length ?? 0));
  const hasFilters = Object.values(form).some((v) => v !== "");
  const matchLabel = total === 1 ? "match" : "matches";

  return (
    <div className="space-y-4">
      <header>
        <h2 className="text-xl font-semibold text-clinic-50">Patients</h2>
        <p className="text-sm text-slate-400">
          Search and paginate the EHR registry. Combine filters to narrow results.
        </p>
      </header>

      <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-3">
        <div className="grid grid-cols-1 gap-3 md:grid-cols-4">
          <div className="md:col-span-2">
            <label htmlFor="filter-q" className={labelClass}>Free-text (name or MRN)</label>
            <input
              id="filter-q"
              type="search"
              autoFocus
              value={form.q}
              onChange={(e) => update("q", e.target.value)}
              placeholder="e.g. Müller or MRN-SIM-…"
              className={inputClass}
            />
          </div>
          <div>
            <label htmlFor="filter-mrn" className={labelClass}>MRN</label>
            <input
              id="filter-mrn"
              value={form.mrn}
              onChange={(e) => update("mrn", e.target.value)}
              className={`${inputClass} font-mono text-xs`}
            />
          </div>
          <div>
            <label htmlFor="filter-status" className={labelClass}>Status</label>
            <select
              id="filter-status"
              value={form.status}
              onChange={(e) => update("status", e.target.value as FormFilters["status"])}
              className={inputClass}
            >
              <option value="">Any</option>
              <option value="Active">Active</option>
              <option value="Inactive">Inactive</option>
              <option value="Deceased">Deceased</option>
              <option value="Merged">Merged</option>
            </select>
          </div>

          <div>
            <label htmlFor="filter-family" className={labelClass}>Family name</label>
            <input
              id="filter-family"
              value={form.familyName}
              onChange={(e) => update("familyName", e.target.value)}
              className={inputClass}
            />
          </div>
          <div>
            <label htmlFor="filter-given" className={labelClass}>Given name</label>
            <input
              id="filter-given"
              value={form.givenName}
              onChange={(e) => update("givenName", e.target.value)}
              className={inputClass}
            />
          </div>
          <div>
            <label htmlFor="filter-sex" className={labelClass}>Sex at birth</label>
            <select
              id="filter-sex"
              value={form.sex}
              onChange={(e) => update("sex", e.target.value as FormFilters["sex"])}
              className={inputClass}
            >
              <option value="">Any</option>
              <option value="male">Male</option>
              <option value="female">Female</option>
              <option value="other">Other</option>
              <option value="unknown">Unknown</option>
            </select>
          </div>
          <div className="grid grid-cols-2 gap-2">
            <div>
              <label htmlFor="filter-dob-from" className={labelClass}>DOB from</label>
              <input
                id="filter-dob-from"
                type="date"
                value={form.dobFrom}
                onChange={(e) => update("dobFrom", e.target.value)}
                className={inputClass}
              />
            </div>
            <div>
              <label htmlFor="filter-dob-to" className={labelClass}>DOB to</label>
              <input
                id="filter-dob-to"
                type="date"
                value={form.dobTo}
                onChange={(e) => update("dobTo", e.target.value)}
                className={inputClass}
              />
            </div>
          </div>
        </div>

        <div className="mt-3 flex items-center justify-between text-xs text-slate-400">
          <span>
            {isFetching ? "Searching…" : `${total} ${matchLabel}`}
            {hasFilters && (
              <button
                type="button"
                onClick={() => setForm(emptyFilters)}
                className="ml-3 underline hover:text-slate-200"
              >
                Clear filters
              </button>
            )}
          </span>
          <span className="flex items-center gap-2">
            <label htmlFor="page-size" className="text-slate-500">Page size</label>
            <select
              id="page-size"
              value={pageSize}
              onChange={(e) => {
                setPageSize(Number(e.target.value));
                setPageIndex(0);
              }}
              className="rounded-md border border-slate-700 bg-slate-900 px-2 py-0.5 text-xs text-slate-100"
            >
              {PAGE_SIZES.map((n) => (
                <option key={n} value={n}>{n}</option>
              ))}
            </select>
          </span>
        </div>
      </section>

      {error && (
        <div className="rounded-md border border-rose-700 bg-rose-900/40 p-3 text-rose-100">
          EHR patient registry unavailable.
        </div>
      )}

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
            {isLoading && (
              <tr>
                <td colSpan={5} className="px-3 py-6 text-center text-slate-400">Loading…</td>
              </tr>
            )}
            {!isLoading && data?.items.length === 0 && (
              <tr>
                <td colSpan={5} className="px-3 py-6 text-center text-slate-400">
                  No patients match the current filters.
                </td>
              </tr>
            )}
            {data?.items.map((p) => (
              <tr key={p.id} className="hover:bg-slate-800/40">
                <td className="px-3 py-2 font-mono text-xs text-slate-300">{p.medicalRecordNumber}</td>
                <td className="px-3 py-2">
                  <Link to={`/patients/${p.id}`} className="text-clinic-50 hover:underline">
                    {p.familyName}, {p.givenName}
                  </Link>
                </td>
                <td className="px-3 py-2 text-slate-400">{p.dateOfBirth}</td>
                <td className="px-3 py-2 text-slate-400">{p.sexAtBirthCode ?? "—"}</td>
                <td className="px-3 py-2">{statusBadge(p.status)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <nav className="flex items-center justify-between text-xs text-slate-400">
        <span>
          {total === 0
            ? "No results"
            : `Showing ${showingFrom}–${showingTo} of ${total}`}
        </span>
        <span className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => setPageIndex(0)}
            disabled={pageIndex === 0}
            className="rounded-md border border-slate-700 px-2 py-0.5 disabled:opacity-40"
          >
            « First
          </button>
          <button
            type="button"
            onClick={() => setPageIndex((i) => Math.max(0, i - 1))}
            disabled={pageIndex === 0}
            className="rounded-md border border-slate-700 px-2 py-0.5 disabled:opacity-40"
          >
            ‹ Prev
          </button>
          <span className="text-slate-300">
            Page {pageIndex + 1} of {pageCount}
          </span>
          <button
            type="button"
            onClick={() => setPageIndex((i) => Math.min(pageCount - 1, i + 1))}
            disabled={pageIndex >= pageCount - 1}
            className="rounded-md border border-slate-700 px-2 py-0.5 disabled:opacity-40"
          >
            Next ›
          </button>
          <button
            type="button"
            onClick={() => setPageIndex(pageCount - 1)}
            disabled={pageIndex >= pageCount - 1}
            className="rounded-md border border-slate-700 px-2 py-0.5 disabled:opacity-40"
          >
            Last »
          </button>
        </span>
      </nav>
    </div>
  );
};
