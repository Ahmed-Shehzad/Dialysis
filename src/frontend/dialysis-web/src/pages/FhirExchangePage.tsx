import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { patientMatch, submitFhirBundle } from "@/features/hie/api/hieApi";
import { ConsentAdminPanel } from "@/features/hie/components/ConsentAdminPanel";
import { FormField, TextInput } from "@/components/ui/FormField";

const SAMPLE_BUNDLE = JSON.stringify(
  {
    resourceType: "Bundle",
    type: "collection",
    entry: [
      {
        resource: {
          resourceType: "Patient",
          identifier: [{ system: "urn:dialysis:mrn", value: "MRN-DEMO-9001" }],
          name: [{ family: "Doe", given: ["Jane"] }],
          gender: "female",
          birthDate: "1972-08-14",
        },
      },
    ],
  },
  null,
  2,
);

const errorMessage = (err: unknown): string => {
  const status = (err as { response?: { status?: number } })?.response?.status;
  return status ? `Failed (HTTP ${status})` : "Request failed";
};

const BundleIngestPanel = () => {
  const [partner, setPartner] = useState("partner.demo");
  const [bundle, setBundle] = useState(SAMPLE_BUNDLE);
  const [response, setResponse] = useState<unknown>(null);

  const m = useMutation({
    mutationFn: () => submitFhirBundle(bundle, partner),
    onSuccess: setResponse,
  });

  return (
    <section className="space-y-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <header>
        <h3 className="text-sm font-medium text-slate-100">Submit FHIR Bundle (inbound)</h3>
        <p className="text-xs text-slate-400">
          POST <span className="font-mono">/api/v1.0/fhir/Bundle</span> with header{" "}
          <span className="font-mono">X-HIE-Partner</span>. Validates + persists incoming clinical
          data.
        </p>
      </header>
      <FormField label="Partner id">
        <TextInput required value={partner} onChange={(e) => setPartner(e.target.value)} />
      </FormField>
      <FormField label="FHIR JSON (Bundle)">
        <textarea
          value={bundle}
          onChange={(e) => setBundle(e.target.value)}
          rows={12}
          className="rounded-md border border-slate-700 bg-slate-900 px-3 py-2 font-mono text-xs text-slate-100 placeholder-slate-500 focus:border-clinic-500 focus:outline-none"
        />
      </FormField>
      <div className="flex items-center gap-3">
        <button
          type="button"
          onClick={() => m.mutate()}
          disabled={m.isPending}
          className="rounded-md bg-clinic-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-clinic-700 disabled:opacity-40"
        >
          {m.isPending ? "Submitting…" : "Submit"}
        </button>
        {m.error && <span className="text-xs text-rose-300">{errorMessage(m.error)}</span>}
        {response !== null && <span className="text-xs text-emerald-300">Accepted ✓</span>}
      </div>
      {response !== null && (
        <pre className="overflow-auto rounded-md border border-slate-700 bg-slate-950 p-2 text-xs text-slate-300">
          {JSON.stringify(response, null, 2)}
        </pre>
      )}
    </section>
  );
};

const PatientMatchPanel = () => {
  const [mrn, setMrn] = useState("");
  const [family, setFamily] = useState("Khan");
  const [given, setGiven] = useState("");
  const [birthdate, setBirthdate] = useState("");
  const [bundle, setBundle] = useState<unknown>(null);

  const m = useMutation({
    mutationFn: () => patientMatch({ mrn: mrn || undefined, family, given, birthdate }),
    onSuccess: setBundle,
  });

  return (
    <section className="space-y-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <header>
        <h3 className="text-sm font-medium text-slate-100">Patient $match (outbound query)</h3>
        <p className="text-xs text-slate-400">
          GET <span className="font-mono">/api/v1.0/fhir/Patient/$match</span> against the HIE
          patient index. Returns a FHIR <span className="font-mono">Bundle.type=searchset</span>.
        </p>
      </header>
      <div className="grid gap-3 sm:grid-cols-2">
        <FormField label="MRN">
          <TextInput value={mrn} onChange={(e) => setMrn(e.target.value)} placeholder="optional" />
        </FormField>
        <FormField label="Birthdate">
          <TextInput type="date" value={birthdate} onChange={(e) => setBirthdate(e.target.value)} />
        </FormField>
        <FormField label="Family name">
          <TextInput value={family} onChange={(e) => setFamily(e.target.value)} />
        </FormField>
        <FormField label="Given name">
          <TextInput value={given} onChange={(e) => setGiven(e.target.value)} />
        </FormField>
      </div>
      <div className="flex items-center gap-3">
        <button
          type="button"
          onClick={() => m.mutate()}
          disabled={m.isPending}
          className="rounded-md bg-clinic-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-clinic-700 disabled:opacity-40"
        >
          {m.isPending ? "Matching…" : "Match"}
        </button>
        {m.error && <span className="text-xs text-rose-300">{errorMessage(m.error)}</span>}
      </div>
      {bundle !== null && (
        <pre className="max-h-96 overflow-auto rounded-md border border-slate-700 bg-slate-950 p-2 text-xs text-slate-300">
          {JSON.stringify(bundle, null, 2)}
        </pre>
      )}
    </section>
  );
};

export const FhirExchangePage = () => (
  <div className="space-y-4">
    <header>
      <h2 className="text-xl font-semibold text-clinic-50">FHIR exchange (HIE)</h2>
      <p className="text-sm text-slate-400">
        Inbound Bundle ingestion + outbound Patient $match across partner organizations.
      </p>
    </header>
    <div className="grid gap-4 lg:grid-cols-2">
      <BundleIngestPanel />
      <PatientMatchPanel />
    </div>
    <ConsentAdminPanel />
  </div>
);
