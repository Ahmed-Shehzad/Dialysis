import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { fetchPatientChart, type ChartItem, type PatientChartView } from "@/features/ehr/api/ehrApi";
import { fetchConsentsForPatient, type ConsentDto } from "@/features/hie/api/hieApi";

const Section = ({ title, items }: { title: string; items: ChartItem[] }) => (
  <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
    <h3 className="mb-2 text-sm font-medium text-slate-200">
      {title} <span className="text-slate-500">({items.length})</span>
    </h3>
    {items.length === 0 ? (
      <div className="text-xs text-slate-500">None recorded.</div>
    ) : (
      <ul className="divide-y divide-slate-800 text-sm">
        {items.map((item) => (
          <li key={item.id} className="grid grid-cols-12 gap-3 py-2">
            <div className="col-span-5 text-slate-200">{item.display}</div>
            <div className="col-span-3 font-mono text-xs text-slate-400">{item.code}</div>
            <div className="col-span-3 text-xs text-slate-400">{item.value ?? "—"}</div>
            <div className="col-span-1 text-right text-xs text-slate-500">{item.status ?? ""}</div>
          </li>
        ))}
      </ul>
    )}
  </section>
);

export const PatientChartPage = () => {
  const { patientId } = useParams<{ patientId: string }>();

  const chart = useQuery<PatientChartView>({
    queryKey: ["ehr", "chart", patientId],
    queryFn: () => fetchPatientChart(patientId as string),
    enabled: Boolean(patientId),
  });

  const consents = useQuery<ConsentDto[]>({
    queryKey: ["hie", "consents", patientId],
    queryFn: () => fetchConsentsForPatient(patientId as string),
    enabled: Boolean(patientId),
  });

  if (!patientId) return <div className="text-slate-400">Missing patient id.</div>;

  return (
    <div className="space-y-4">
      <header>
        <h2 className="text-xl font-semibold text-clinic-50">Patient chart</h2>
        <p className="font-mono text-xs text-slate-400">{patientId}</p>
      </header>

      {chart.isLoading && <div className="text-slate-400">Loading chart…</div>}
      {chart.error && (
        <div className="rounded-md border border-rose-700 bg-rose-900/40 p-3 text-rose-100">
          Chart unavailable.
        </div>
      )}

      {chart.data && (
        <div className="grid gap-4 lg:grid-cols-2">
          <Section title="Problems" items={chart.data.problems} />
          <Section title="Allergies" items={chart.data.allergies} />
          <Section title="Medications" items={chart.data.medications} />
          <Section title="Vital signs" items={chart.data.vitals} />
          <Section title="Immunizations" items={chart.data.immunizations} />
        </div>
      )}

      <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
        <h3 className="mb-2 text-sm font-medium text-slate-200">
          HIE consents <span className="text-slate-500">(cross-organisation disclosure)</span>
        </h3>
        {consents.isLoading && <div className="text-xs text-slate-400">Loading…</div>}
        {consents.data && consents.data.length === 0 && (
          <div className="text-xs text-slate-500">No consent grants recorded.</div>
        )}
        {consents.data && consents.data.length > 0 && (
          <ul className="divide-y divide-slate-800 text-sm">
            {consents.data.map((c) => (
              <li key={c.id} className="grid grid-cols-12 gap-3 py-2">
                <div className="col-span-4 font-mono text-xs text-slate-300">{c.partnerId}</div>
                <div className="col-span-3 text-slate-300">{c.scope}</div>
                <div className="col-span-2 text-xs uppercase text-slate-400">
                  {typeof c.direction === "number" ? (c.direction === 1 ? "Inbound" : "Outbound") : c.direction}
                </div>
                <div className="col-span-3 text-xs text-slate-400">
                  effective {new Date(c.effectiveFromUtc).toLocaleDateString()}
                  {c.revokedAtUtc ? " (revoked)" : ""}
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
};
