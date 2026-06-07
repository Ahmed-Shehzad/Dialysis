import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { FormField, TextInput } from "@/components/ui/FormField";
import {
  type AuthoringResult,
  type FhirElementConstraint,
  type FhirProfileSpec,
  authorImplementationGuide,
  authorProfile,
  listGuides,
  listProfiles,
  loadPackage,
} from "@/features/fhir-authoring/api/fhirAuthoringApi";

const BLANK_CONSTRAINT: FhirElementConstraint = { path: "" };

const severityClass = (sev: string): string => {
  switch (sev) {
    case "fatal":
    case "error":
      return "text-rose-300";
    case "warning":
      return "text-amber-300";
    case "information":
      return "text-slate-400";
    default:
      return "text-slate-300";
  }
};

const VerificationPanel = ({ result }: { result: AuthoringResult }) => (
  <div className="space-y-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
    <div className="flex items-center gap-3">
      <span
        className={
          "rounded px-2 py-0.5 text-xs font-semibold " +
          (result.published ? "bg-emerald-700/40 text-emerald-200" : "bg-rose-700/40 text-rose-200")
        }
      >
        {result.published ? "Verified · Published" : "Verification failed"}
      </span>
      <span className="text-xs text-slate-500">HTTP {result.httpStatus}</span>
    </div>

    {result.issues.length > 0 && (
      <ul className="space-y-1 text-xs">
        {result.issues.map((issue, i) => (
          <li key={i} className={severityClass(issue.severity)}>
            <span className="uppercase">[{issue.severity}]</span> {issue.diagnostics}
          </li>
        ))}
      </ul>
    )}

    {result.artifact && (
      <details className="text-xs">
        <summary className="cursor-pointer text-slate-400 hover:text-slate-200">
          {String(result.artifact.resourceType)} JSON
        </summary>
        <pre className="mt-2 max-h-96 overflow-auto rounded-md border border-slate-800 bg-slate-950 p-3 font-mono text-[11px] text-slate-200">
          {JSON.stringify(result.artifact, null, 2)}
        </pre>
      </details>
    )}
  </div>
);

const ConstraintEditor = ({
  constraints,
  onChange,
}: {
  constraints: FhirElementConstraint[];
  onChange: (next: FhirElementConstraint[]) => void;
}) => {
  const update = (i: number, patch: Partial<FhirElementConstraint>) =>
    onChange(constraints.map((c, idx) => (idx === i ? { ...c, ...patch } : c)));
  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between">
        <span className="text-xs uppercase tracking-wide text-slate-400">Element constraints</span>
        <button
          type="button"
          onClick={() => onChange([...constraints, { ...BLANK_CONSTRAINT }])}
          className="rounded-md border border-slate-700 px-2 py-0.5 text-xs text-slate-300 hover:bg-slate-800"
        >
          + Add element
        </button>
      </div>
      {constraints.length === 0 && (
        <p className="text-xs text-slate-500">
          No constraints — the profile derives the base unchanged.
        </p>
      )}
      {constraints.map((c, i) => (
        <div
          key={i}
          className="grid grid-cols-12 items-center gap-2 rounded-md border border-slate-800 bg-slate-900/40 p-2"
        >
          <input
            aria-label="Element path"
            placeholder="Element path (e.g. Patient.identifier)"
            value={c.path}
            onChange={(e) => update(i, { path: e.target.value })}
            className="col-span-5 rounded border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-100 placeholder-slate-500"
          />
          <input
            aria-label="Minimum cardinality"
            placeholder="min"
            inputMode="numeric"
            value={c.min ?? ""}
            onChange={(e) =>
              update(i, { min: e.target.value === "" ? null : Number(e.target.value) })
            }
            className="col-span-1 rounded border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-100 placeholder-slate-500"
          />
          <input
            aria-label="Maximum cardinality"
            placeholder="max"
            value={c.max ?? ""}
            onChange={(e) => update(i, { max: e.target.value || null })}
            className="col-span-1 rounded border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-100 placeholder-slate-500"
          />
          <label className="col-span-3 flex items-center gap-1 text-xs text-slate-300">
            <input
              type="checkbox"
              checked={Boolean(c.mustSupport)}
              onChange={(e) => update(i, { mustSupport: e.target.checked })}
            />
            mustSupport
          </label>
          <button
            type="button"
            onClick={() => onChange(constraints.filter((_, idx) => idx !== i))}
            className="col-span-2 rounded border border-rose-800 px-2 py-1 text-xs text-rose-300 hover:bg-rose-900/40"
          >
            Remove
          </button>
          <input
            aria-label="Binding value set"
            placeholder="Binding value set (optional)"
            value={c.bindingValueSet ?? ""}
            onChange={(e) => update(i, { bindingValueSet: e.target.value || null })}
            className="col-span-9 rounded border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-100 placeholder-slate-500"
          />
          <select
            aria-label="Binding strength"
            value={c.bindingStrength ?? "required"}
            onChange={(e) => update(i, { bindingStrength: e.target.value })}
            className="col-span-3 rounded border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-200"
          >
            <option value="required">required</option>
            <option value="extensible">extensible</option>
            <option value="preferred">preferred</option>
            <option value="example">example</option>
          </select>
        </div>
      ))}
    </div>
  );
};

const PackageLoadCard = () => {
  const queryClient = useQueryClient();
  const [file, setFile] = useState<File | null>(null);
  const mut = useMutation({
    mutationFn: (f: File) => loadPackage(f),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["fhir-authoring"] }),
  });
  return (
    <div className="space-y-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <h3 className="text-sm font-medium text-slate-100">Load external FHIR package</h3>
      <p className="text-xs text-slate-500">
        Upload an HL7 package tarball (<code>.tgz</code> — US Core, CH Core, …). Its conformance
        resources are registered so IGs that declare a dependency on it resolve and stop warning.
      </p>
      <div className="flex flex-wrap items-center gap-2">
        <input
          type="file"
          accept=".tgz,.tar.gz,application/gzip"
          aria-label="FHIR package tarball"
          onChange={(e) => setFile(e.target.files?.[0] ?? null)}
          className="text-xs text-slate-300 file:mr-2 file:rounded-md file:border-0 file:bg-slate-800 file:px-3 file:py-1.5 file:text-slate-200"
        />
        <button
          type="button"
          onClick={() => file && mut.mutate(file)}
          disabled={!file || mut.isPending}
          className="rounded-md bg-clinic-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-clinic-700 disabled:opacity-40"
        >
          {mut.isPending ? "Loading…" : "Load package"}
        </button>
      </div>
      {mut.data && (
        <ul className="space-y-1 text-xs">
          {mut.data.issues.map((issue, i) => (
            <li key={i} className={severityClass(issue.severity)}>
              {issue.diagnostics}
            </li>
          ))}
        </ul>
      )}
      {mut.error && (
        <p className="text-xs text-rose-300">Upload failed — is the HIS host running?</p>
      )}
    </div>
  );
};

const PublishedList = () => {
  const profiles = useQuery({ queryKey: ["fhir-authoring", "profiles"], queryFn: listProfiles });
  const guides = useQuery({ queryKey: ["fhir-authoring", "guides"], queryFn: listGuides });
  const row = (r: Record<string, unknown>) => (
    <li key={String(r.url)} className="text-xs text-slate-300">
      <span className="text-clinic-200">{String(r.name ?? r.id)}</span>{" "}
      <span className="text-slate-500">{String(r.url)}</span>
    </li>
  );
  return (
    <div className="space-y-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <h3 className="text-sm font-medium text-slate-100">Published artifacts</h3>
      <div>
        <div className="mb-1 text-xs uppercase text-slate-500">
          StructureDefinitions ({profiles.data?.length ?? 0})
        </div>
        <ul className="space-y-0.5">{(profiles.data ?? []).map(row)}</ul>
      </div>
      <div>
        <div className="mb-1 text-xs uppercase text-slate-500">
          ImplementationGuides ({guides.data?.length ?? 0})
        </div>
        <ul className="space-y-0.5">{(guides.data ?? []).map(row)}</ul>
      </div>
    </div>
  );
};

export const FhirAuthoringPage = () => {
  const queryClient = useQueryClient();
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["fhir-authoring"] });

  // --- Profile form ---
  const [pBase, setPBase] = useState("Patient");
  const [pName, setPName] = useState("DialysisPatient");
  const [pUrl, setPUrl] = useState(
    "https://dialysis.local/fhir/StructureDefinition/dialysis-patient",
  );
  const [pTitle, setPTitle] = useState("Dialysis Patient");
  const [constraints, setConstraints] = useState<FhirElementConstraint[]>([
    { path: "Patient.identifier", min: 1, mustSupport: true },
    { path: "Patient.name", mustSupport: true },
  ]);

  const profileSpec = (): FhirProfileSpec => ({
    baseResourceType: pBase.trim(),
    url: pUrl.trim(),
    name: pName.trim(),
    title: pTitle.trim() || undefined,
    constraints: constraints
      .filter((c) => c.path.trim())
      .map((c) => ({ ...c, path: c.path.trim() })),
  });

  const profileMut = useMutation({
    mutationFn: () => authorProfile(profileSpec()),
    onSuccess: invalidate,
  });

  // --- IG form ---
  const [igPackage, setIgPackage] = useState("dialysis.fhir.core");
  const [igName, setIgName] = useState("DialysisCoreIG");
  const [igUrl, setIgUrl] = useState(
    "https://dialysis.local/fhir/ImplementationGuide/dialysis-core",
  );
  const [igVersion, setIgVersion] = useState("0.1.0");
  const [igDependsOn, setIgDependsOn] = useState(
    "http://hl7.org/fhir/us/core/ImplementationGuide/hl7.fhir.us.core",
  );

  const igMut = useMutation({
    mutationFn: () =>
      authorImplementationGuide({
        packageId: igPackage.trim(),
        url: igUrl.trim(),
        name: igName.trim(),
        version: igVersion.trim() || "0.1.0",
        profiles: [profileSpec()],
        dependsOn: igDependsOn.trim()
          ? [{ uri: igDependsOn.trim(), packageId: "hl7.fhir.us.core" }]
          : [],
      }),
    onSuccess: invalidate,
  });

  return (
    <div className="space-y-4">
      <header>
        <h2 className="text-xl font-semibold text-clinic-50">FHIR Authoring</h2>
        <p className="text-sm text-slate-400">
          Create FHIR R4 profiles and Implementation Guides on demand. Each artifact is built, its
          correctness verified (snapshot generation against the bundled R4 spec + round-trip), and
          only published into the conformance registry when valid.
        </p>
      </header>

      <div className="grid gap-4 lg:grid-cols-2">
        <div className="space-y-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
          <h3 className="text-sm font-medium text-slate-100">Author StructureDefinition</h3>
          <div className="grid grid-cols-2 gap-3">
            <FormField label="Base resource type">
              <TextInput value={pBase} onChange={(e) => setPBase(e.target.value)} />
            </FormField>
            <FormField label="Name">
              <TextInput value={pName} onChange={(e) => setPName(e.target.value)} />
            </FormField>
          </div>
          <FormField label="Canonical URL">
            <TextInput value={pUrl} onChange={(e) => setPUrl(e.target.value)} />
          </FormField>
          <FormField label="Title">
            <TextInput value={pTitle} onChange={(e) => setPTitle(e.target.value)} />
          </FormField>
          <ConstraintEditor constraints={constraints} onChange={setConstraints} />
          <button
            type="button"
            onClick={() => profileMut.mutate()}
            disabled={profileMut.isPending}
            className="rounded-md bg-clinic-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-clinic-700 disabled:opacity-40"
          >
            {profileMut.isPending ? "Building + verifying…" : "Author + verify profile"}
          </button>
          {profileMut.data && <VerificationPanel result={profileMut.data} />}
          {profileMut.error && (
            <p className="text-xs text-rose-300">Request failed — is the HIS host running?</p>
          )}
        </div>

        <div className="space-y-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
          <h3 className="text-sm font-medium text-slate-100">Author ImplementationGuide</h3>
          <p className="text-xs text-slate-500">
            Bundles the profile defined on the left, plus package metadata and an optional
            dependency (advisory — an unresolved external IG is a warning, not a failure).
          </p>
          <div className="grid grid-cols-2 gap-3">
            <FormField label="Package id">
              <TextInput value={igPackage} onChange={(e) => setIgPackage(e.target.value)} />
            </FormField>
            <FormField label="Name">
              <TextInput value={igName} onChange={(e) => setIgName(e.target.value)} />
            </FormField>
          </div>
          <FormField label="Canonical URL">
            <TextInput value={igUrl} onChange={(e) => setIgUrl(e.target.value)} />
          </FormField>
          <div className="grid grid-cols-2 gap-3">
            <FormField label="Version">
              <TextInput value={igVersion} onChange={(e) => setIgVersion(e.target.value)} />
            </FormField>
            <FormField label="Depends on (canonical, optional)">
              <TextInput value={igDependsOn} onChange={(e) => setIgDependsOn(e.target.value)} />
            </FormField>
          </div>
          <button
            type="button"
            onClick={() => igMut.mutate()}
            disabled={igMut.isPending}
            className="rounded-md bg-clinic-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-clinic-700 disabled:opacity-40"
          >
            {igMut.isPending ? "Building + verifying…" : "Author + verify IG"}
          </button>
          {igMut.data && <VerificationPanel result={igMut.data} />}
          {igMut.error && (
            <p className="text-xs text-rose-300">Request failed — is the HIS host running?</p>
          )}
        </div>
      </div>

      <PackageLoadCard />
      <PublishedList />
    </div>
  );
};
