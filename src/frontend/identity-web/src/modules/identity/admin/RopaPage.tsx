import { useQuery } from "@tanstack/react-query";
import { fetchRopa } from "@/features/data-protection/api/dataProtectionApi";

/**
 * GDPR Art. 30 Records of Processing Activities viewer. Renders the document the
 * <c>IRopaGenerator</c> assembles from every module's lawful-basis registry plus the
 * platform retention schedule. The DPO files this with the supervisory authority on
 * request — the operator's view here is the "what's the system declaring today"
 * truth.
 */
export const RopaPage = () => {
  const query = useQuery({
    queryKey: ["identity", "data-protection", "ropa"],
    queryFn: fetchRopa,
    refetchInterval: 5 * 60_000,
  });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-lg font-semibold text-slate-100">
          Records of Processing Activities (RoPA)
        </h1>
        <p className="text-sm text-slate-400">
          GDPR Art. 30 — every processing activity the platform performs, declared by module.
        </p>
      </div>

      {query.isLoading && <div className="text-sm text-slate-400">Generating RoPA…</div>}
      {query.isError && (
        <div className="text-sm text-rose-300">
          Could not load RoPA. Check the data-protection endpoint mount.
        </div>
      )}

      {query.data && (
        <>
          <div className="rounded border border-slate-800 bg-slate-900/60 p-4 text-sm">
            <div className="grid grid-cols-2 gap-2 text-slate-300">
              <div>
                <span className="text-slate-500">Controller:</span> {query.data.controllerName}
              </div>
              <div>
                <span className="text-slate-500">Contact:</span> {query.data.controllerContact}
              </div>
              <div className="col-span-2">
                <span className="text-slate-500">Generated:</span>{" "}
                {new Date(query.data.generatedAtUtc).toISOString().replace("T", " ").slice(0, 19)} Z
              </div>
            </div>
          </div>

          {query.data.modules.map((section) => (
            <section key={section.moduleSlug} className="space-y-2">
              <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-300">
                {section.moduleSlug}
              </h2>
              {section.activities.length === 0 ? (
                <div className="text-xs text-slate-500">
                  No processing activities declared in this module.
                </div>
              ) : (
                <table className="w-full table-fixed border-collapse text-sm">
                  <thead className="text-left text-slate-400">
                    <tr>
                      <th className="w-44 pb-2 font-medium">Activity</th>
                      <th className="pb-2 font-medium">Purpose</th>
                      <th className="w-36 pb-2 font-medium">Lawful basis</th>
                      <th className="pb-2 font-medium">Data categories</th>
                      <th className="pb-2 font-medium">Recipients</th>
                      <th className="w-32 pb-2 font-medium">Retention</th>
                    </tr>
                  </thead>
                  <tbody className="text-slate-200">
                    {section.activities.map((a) => (
                      <tr key={a.name} className="border-t border-slate-800/60 align-top">
                        <td className="py-2">{a.name}</td>
                        <td className="py-2 text-slate-300">{a.purpose}</td>
                        <td className="py-2 text-xs">{a.lawfulBasis}</td>
                        <td className="py-2 text-xs text-slate-400">
                          {a.dataCategories.join(", ")}
                        </td>
                        <td className="py-2 text-xs text-slate-400">{a.recipients.join(", ")}</td>
                        <td className="py-2 text-xs">{a.retentionWindow ?? "—"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </section>
          ))}

          <section className="space-y-2">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-300">
              Retention schedule
            </h2>
            <table className="w-full table-fixed border-collapse text-sm">
              <thead className="text-left text-slate-400">
                <tr>
                  <th className="pb-2 font-medium">Data category</th>
                  <th className="pb-2 font-medium">Window</th>
                  <th className="pb-2 font-medium">Legal basis</th>
                </tr>
              </thead>
              <tbody className="text-slate-200">
                {query.data.retention.map((r) => (
                  <tr key={r.dataCategory} className="border-t border-slate-800/60">
                    <td className="py-2">{r.dataCategory}</td>
                    <td className="py-2">{r.windowLabel}</td>
                    <td className="py-2 text-xs text-slate-400">{r.legalBasis}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </section>
        </>
      )}
    </div>
  );
};

export default RopaPage;
