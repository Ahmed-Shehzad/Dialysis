import { useSearchParams } from "react-router";

export type IntegrationsTabKey =
  | "flows"
  | "messages"
  | "config-map"
  | "code-templates"
  | "alerts"
  | "events"
  | "retention"
  | "dependency-graph"
  | "hl7-workbench";

const TAB_DEFS: Array<{ key: IntegrationsTabKey; label: string; hint: string }> = [
  { key: "flows", label: "Flows", hint: "Channels, lifecycle, statistics" },
  { key: "dependency-graph", label: "Dependencies", hint: "Channel start-order graph" },
  {
    key: "hl7-workbench",
    label: "HL7 Workbench",
    hint: "Parse / validate / dispatch your own HL7",
  },
  { key: "messages", label: "Messages", hint: "Message ledger with drill-down" },
  { key: "config-map", label: "Config Map", hint: "Global / per-flow variables" },
  { key: "code-templates", label: "Code Templates", hint: "Shared script libraries" },
  { key: "alerts", label: "Alerts", hint: "Rules + fired events" },
  { key: "events", label: "Audit Events", hint: "Lifecycle + user actions" },
  { key: "retention", label: "Retention", hint: "Pruner settings" },
];

const DEFAULT_TAB: IntegrationsTabKey = "flows";

export const useIntegrationsTab = (): [IntegrationsTabKey, (next: IntegrationsTabKey) => void] => {
  const [params, setParams] = useSearchParams();
  const raw = params.get("tab");
  const current = (TAB_DEFS.find((t) => t.key === raw)?.key ?? DEFAULT_TAB) as IntegrationsTabKey;
  const setTab = (next: IntegrationsTabKey) => {
    const updated = new URLSearchParams(params);
    updated.set("tab", next);
    setParams(updated, { replace: true });
  };
  return [current, setTab];
};

export const IntegrationsTabs = ({
  current,
  onChange,
}: {
  current: IntegrationsTabKey;
  onChange: (next: IntegrationsTabKey) => void;
}) => (
  <nav className="border-b border-slate-800">
    <ul className="-mb-px flex flex-wrap gap-1">
      {TAB_DEFS.map((t) => {
        const active = t.key === current;
        return (
          <li key={t.key}>
            <button
              type="button"
              onClick={() => onChange(t.key)}
              title={t.hint}
              className={
                "rounded-t-md px-3 py-2 text-sm transition " +
                (active
                  ? "border border-slate-800 border-b-transparent bg-slate-900/60 text-clinic-100"
                  : "text-slate-400 hover:text-slate-200")
              }
            >
              {t.label}
            </button>
          </li>
        );
      })}
    </ul>
  </nav>
);
