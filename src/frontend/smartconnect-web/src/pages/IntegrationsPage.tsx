import {
  IntegrationsTabs,
  useIntegrationsTab,
  type IntegrationsTabKey,
} from "@/features/smartconnect/components/IntegrationsTabs";
import { IntegrationsSummary } from "@/features/smartconnect/components/IntegrationsSummary";
import { FlowsTab } from "@/features/smartconnect/tabs/FlowsTab";
import { MessagesTab } from "@/features/smartconnect/tabs/MessagesTab";
import { ConfigurationMapTab } from "@/features/smartconnect/tabs/ConfigurationMapTab";
import { CodeTemplatesTab } from "@/features/smartconnect/tabs/CodeTemplatesTab";
import { AlertsTab } from "@/features/smartconnect/tabs/AlertsTab";
import { AuditEventsTab } from "@/features/smartconnect/tabs/AuditEventsTab";
import { RetentionTab } from "@/features/smartconnect/tabs/RetentionTab";
import { DependencyGraphTab } from "@/features/smartconnect/tabs/DependencyGraphTab";
import { Hl7WorkbenchTab } from "@/features/smartconnect/tabs/Hl7WorkbenchTab";
import { ModuleHeader } from "@/shell/ModuleHeader";

const renderTab = (key: IntegrationsTabKey) => {
  switch (key) {
    case "flows":
      return <FlowsTab />;
    case "dependency-graph":
      return <DependencyGraphTab />;
    case "hl7-workbench":
      return <Hl7WorkbenchTab />;
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
      <ModuleHeader
        moduleSlug="smartconnect"
        quickActions={[
          {
            label: "+ New channel",
            to: "/integrations?tab=flows&action=new",
            hint: "Create a new HL7 v2 / FHIR channel via the multi-step dialog",
            variant: "primary",
          },
          {
            label: "HL7 Workbench",
            to: "/integrations?tab=hl7-workbench",
            hint: "Paste a real HL7 v2 message, parse / validate / dispatch it through any channel",
          },
          {
            label: "Dependency graph",
            to: "/integrations?tab=dependency-graph",
            hint: "Visualise which channels depend on which",
          },
        ]}
        tour={[
          { title: "Flows", body: "channels, lifecycle, statistics" },
          { title: "Messages", body: "ledger search by flow / sender / batch" },
          {
            title: "HL7 Workbench",
            body: "bring your own message and walk it through the pipeline",
          },
          { title: "Alerts", body: "rules + fired alert events" },
        ]}
      />
      <IntegrationsSummary />
      <IntegrationsTabs current={tab} onChange={setTab} />
      <div>{renderTab(tab)}</div>
    </div>
  );
};
