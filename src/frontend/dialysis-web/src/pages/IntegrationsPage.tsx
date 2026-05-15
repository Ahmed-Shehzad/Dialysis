import {
  IntegrationsTabs,
  useIntegrationsTab,
  type IntegrationsTabKey,
} from "@/features/smartconnect/components/IntegrationsTabs";
import { FlowsTab } from "@/features/smartconnect/tabs/FlowsTab";
import { MessagesTab } from "@/features/smartconnect/tabs/MessagesTab";
import { ConfigurationMapTab } from "@/features/smartconnect/tabs/ConfigurationMapTab";
import { CodeTemplatesTab } from "@/features/smartconnect/tabs/CodeTemplatesTab";
import { AlertsTab } from "@/features/smartconnect/tabs/AlertsTab";
import { AuditEventsTab } from "@/features/smartconnect/tabs/AuditEventsTab";
import { RetentionTab } from "@/features/smartconnect/tabs/RetentionTab";

const renderTab = (key: IntegrationsTabKey) => {
  switch (key) {
    case "flows":
      return <FlowsTab />;
    case "messages":
      return <MessagesTab />;
    case "config-map":
      return <ConfigurationMapTab />;
    case "code-templates":
      return <CodeTemplatesTab />;
    case "alerts":
      return <AlertsTab />;
    case "events":
      return <AuditEventsTab />;
    case "retention":
      return <RetentionTab />;
  }
};

export const IntegrationsPage = () => {
  const [tab, setTab] = useIntegrationsTab();
  return (
    <div className="space-y-4">
      <header>
        <h2 className="text-xl font-semibold text-clinic-50">Integrations (SmartConnect)</h2>
        <p className="text-sm text-slate-400">
          Operator console — flows, message ledger, configuration map, code templates, alerts,
          audit events, and retention. Modelled on the Mirth Connect Administrator left rail.
        </p>
      </header>
      <IntegrationsTabs current={tab} onChange={setTab} />
      <div>{renderTab(tab)}</div>
    </div>
  );
};
