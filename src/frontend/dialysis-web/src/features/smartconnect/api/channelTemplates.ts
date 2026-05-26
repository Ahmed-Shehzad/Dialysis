import type { IntegrationFlowPipelineDefinition, OutboundRouteSlot } from "./types";

/**
 * Generates a fresh `IntegrationFlowPipelineDefinition` for a known channel shape. Operators can
 * edit the generated definition in the dialog's confirm step before posting, or they can pick
 * "Custom blank pipeline" to start from an empty definition and write everything by hand.
 *
 * Each preset answers a specific question — "what's the minimum pipeline that makes this work?".
 * The dialog renders the preview JSON so the operator can verify (and adjust) before submit.
 */
export type ChannelTemplateId = "hl7-mllp" | "hl7-file" | "custom";

export type ChannelTemplate = {
  id: ChannelTemplateId;
  label: string;
  description: string;
  build: () => IntegrationFlowPipelineDefinition;
};

const allowAllFilter = { kind: "allow-all" };

const emptyPipeline = (): IntegrationFlowPipelineDefinition => ({
  routeFilters: [],
  attachmentHandler: null,
  sourceTransformStages: [],
  outboundRoutesSequential: false,
  outboundRoutes: [],
  scripts: null,
  linkedLibraryIds: [],
});

const hl7MllpRoute = (): OutboundRouteSlot => ({
  ordinal: 0,
  kind: "transponder-bus",
  propertiesJson: JSON.stringify(
    {
      routingHint: "ORU^R01",
      headers: { sourceModule: "smartconnect" },
    },
    null,
    2,
  ),
});

export const CHANNEL_TEMPLATES: ChannelTemplate[] = [
  {
    id: "hl7-mllp",
    label: "HL7 v2 over MLLP",
    description:
      "Accepts MLLP-framed HL7 v2 messages from the mllp source connector (default port 2575); publishes the parsed payload onto the Transponder bus so downstream modules can subscribe via SmartConnectRoutedPayloadIntegrationEvent.",
    build: () => ({
      ...emptyPipeline(),
      routeFilters: [allowAllFilter],
      outboundRoutes: [hl7MllpRoute()],
    }),
  },
  {
    id: "hl7-file",
    label: "HL7 v2 over File-drop",
    description:
      "Watches a directory (file-reader source connector) for *.hl7 files and routes the same way as MLLP. Useful when the upstream is a batch dump rather than a live socket.",
    build: () => ({
      ...emptyPipeline(),
      routeFilters: [allowAllFilter],
      outboundRoutes: [hl7MllpRoute()],
    }),
  },
  {
    id: "custom",
    label: "Custom blank pipeline",
    description:
      "Start from an empty pipeline. Add route filters, source transforms, and outbound routes by hand from the form below.",
    build: emptyPipeline,
  },
];

export const findTemplate = (id: ChannelTemplateId): ChannelTemplate =>
  CHANNEL_TEMPLATES.find((t) => t.id === id) ?? CHANNEL_TEMPLATES[0]!;
