import { apiClient } from "@/lib/api/apiClient";

/**
 * On-demand FHIR profile / Implementation Guide authoring. The HIS host maps
 * `MapFhirAuthoringEndpoints()` under `/fhir/_author`; the gateway exposes it through the
 * `fhir-his` route as `/fhir/his/_author/...`.
 */
const AUTHORING_BASE = "/fhir/his/_author";

export type FhirElementConstraint = {
  path: string;
  min?: number | null;
  max?: string | null;
  mustSupport?: boolean | null;
  short?: string | null;
  definition?: string | null;
  typeCode?: string | null;
  bindingValueSet?: string | null;
  bindingStrength?: string | null;
  fixedString?: string | null;
  fixedCode?: string | null;
  fixedUri?: string | null;
};

export type FhirProfileSpec = {
  baseResourceType: string;
  url: string;
  name: string;
  title?: string;
  version?: string;
  description?: string;
  baseDefinition?: string;
  constraints: FhirElementConstraint[];
};

export type FhirIgDependency = {
  uri: string;
  packageId?: string;
  version?: string;
};

export type FhirImplementationGuideSpec = {
  packageId: string;
  url: string;
  name: string;
  title?: string;
  version: string;
  profiles: FhirProfileSpec[];
  dependsOn: FhirIgDependency[];
};

export type VerificationIssue = {
  severity: string;
  code: string;
  diagnostics: string;
};

export type AuthoringResult = {
  /** True when verification passed and the artifact was published into the registry. */
  published: boolean;
  httpStatus: number;
  issues: VerificationIssue[];
  /** The produced StructureDefinition / ImplementationGuide (raw FHIR JSON). */
  artifact: Record<string, unknown> | null;
  /** Additional StructureDefinitions returned alongside an authored IG. */
  profiles: Record<string, unknown>[];
};

type FhirResource = { resourceType?: string; [k: string]: unknown };
type BundleEntry = { resource?: FhirResource };
type FhirBundleOrOutcome = {
  resourceType?: string;
  entry?: BundleEntry[];
  issue?: { severity?: string; code?: string; diagnostics?: string }[];
};

const parseIssues = (outcome: FhirBundleOrOutcome | undefined): VerificationIssue[] =>
  (outcome?.issue ?? []).map((i) => ({
    severity: i.severity ?? "information",
    code: i.code ?? "informational",
    diagnostics: i.diagnostics ?? "",
  }));

const parseResult = (status: number, body: FhirBundleOrOutcome): AuthoringResult => {
  // 400 → a bare OperationOutcome (malformed spec). Otherwise a collection Bundle whose
  // first entry is the verification OperationOutcome and the rest are the artifacts.
  if (body.resourceType === "OperationOutcome") {
    return {
      published: false,
      httpStatus: status,
      issues: parseIssues(body),
      artifact: null,
      profiles: [],
    };
  }
  const resources = (body.entry ?? []).map((e) => e.resource).filter(Boolean) as FhirResource[];
  const outcome = resources.find((r) => r.resourceType === "OperationOutcome");
  const artifact =
    resources.find(
      (r) => r.resourceType === "StructureDefinition" || r.resourceType === "ImplementationGuide",
    ) ?? null;
  const profiles = resources.filter(
    (r) => r.resourceType === "StructureDefinition" && r !== artifact,
  );
  return {
    published: status === 201,
    httpStatus: status,
    issues: parseIssues(outcome as FhirBundleOrOutcome),
    artifact,
    profiles,
  };
};

// 4xx carries a meaningful FHIR body (verification failures / bad spec) — surface it, don't throw.
const acceptClientErrors = { validateStatus: (s: number) => s < 500 } as const;

export const authorProfile = async (spec: FhirProfileSpec): Promise<AuthoringResult> => {
  const res = await apiClient.post<FhirBundleOrOutcome>(
    `${AUTHORING_BASE}/StructureDefinition`,
    spec,
    { headers: { "Content-Type": "application/json" }, ...acceptClientErrors },
  );
  return parseResult(res.status, res.data);
};

export const authorImplementationGuide = async (
  spec: FhirImplementationGuideSpec,
): Promise<AuthoringResult> => {
  const res = await apiClient.post<FhirBundleOrOutcome>(
    `${AUTHORING_BASE}/ImplementationGuide`,
    spec,
    { headers: { "Content-Type": "application/json" }, ...acceptClientErrors },
  );
  return parseResult(res.status, res.data);
};

export type PackageLoadResult = {
  ok: boolean;
  httpStatus: number;
  issues: VerificationIssue[];
};

/**
 * Uploads an external FHIR package tarball (`.tgz` — US Core, CH Core, …). The host registers
 * its conformance resources so IGs that declare a dependency on it resolve and stop warning.
 */
export const loadPackage = async (file: File): Promise<PackageLoadResult> => {
  const body = await file.arrayBuffer();
  const res = await apiClient.post<FhirBundleOrOutcome>(`${AUTHORING_BASE}/package`, body, {
    headers: { "Content-Type": "application/gzip" },
    ...acceptClientErrors,
  });
  return { ok: res.status === 200, httpStatus: res.status, issues: parseIssues(res.data) };
};

const parseSearchset = (body: FhirBundleOrOutcome): Record<string, unknown>[] =>
  (body.entry ?? []).map((e) => e.resource).filter(Boolean) as Record<string, unknown>[];

export const listProfiles = async (): Promise<Record<string, unknown>[]> => {
  const res = await apiClient.get<FhirBundleOrOutcome>(`${AUTHORING_BASE}/StructureDefinition`);
  return parseSearchset(res.data);
};

export const listGuides = async (): Promise<Record<string, unknown>[]> => {
  const res = await apiClient.get<FhirBundleOrOutcome>(`${AUTHORING_BASE}/ImplementationGuide`);
  return parseSearchset(res.data);
};
