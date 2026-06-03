import { apiClient } from "@/lib/api/apiClient";

export type ProcessingActivity = {
  name: string;
  purpose: string;
  lawfulBasis: string;
  dataCategories: string[];
  recipients: string[];
  retentionWindow: string | null;
  internationalTransfers: string | null;
};

export type RopaModuleSection = {
  moduleSlug: string;
  activities: ProcessingActivity[];
};

export type RetentionWindowRegistration = {
  dataCategory: string;
  windowLabel: string;
  legalBasis: string;
};

export type RopaDocument = {
  controllerName: string;
  controllerContact: string;
  generatedAtUtc: string;
  modules: RopaModuleSection[];
  retention: RetentionWindowRegistration[];
};

export type DataSubjectResource = {
  resourceType: string;
  identifier: string;
  json: string;
};

export type DataSubjectExport = {
  patientId: string;
  generatedAtUtc: string;
  resources: DataSubjectResource[];
};

export type Consent = {
  consentId: string;
  patientId: string;
  partnerId: string;
  scope: string;
  direction: number;
  effectiveFromUtc: string;
  effectiveToUtc: string | null;
  status: string;
};

// The GDPR endpoints are mounted by every module that calls MapEuDataProtectionRoutes(); we
// hit them through the EHR host (where patient data canonically lives). Path is gateway-
// proxied: /api/ehr/admin/data-protection/ropa, etc.
const ehrAdminPrefix = "/api/ehr";

export const fetchRopa = async (): Promise<RopaDocument> => {
  const response = await apiClient.get<RopaDocument>(
    `${ehrAdminPrefix}/admin/data-protection/ropa`,
  );
  return response.data;
};

export const exportPatientData = async (patientId: string): Promise<DataSubjectExport> => {
  const response = await apiClient.get<DataSubjectExport>(
    `${ehrAdminPrefix}/api/v1.0/data-subject-rights/${patientId}/export`,
  );
  return response.data;
};

export const requestErasure = async (
  patientId: string,
  requestedBy: string,
  reason: string,
): Promise<{ requestId: string }> => {
  const response = await apiClient.post<{ requestId: string }>(
    `${ehrAdminPrefix}/api/v1.0/data-subject-rights/${patientId}/erasure-request`,
    { requestedBy, reason },
  );
  return response.data;
};

export const requestRestriction = async (
  patientId: string,
  requestedBy: string,
  reason: string,
): Promise<{ requestId: string }> => {
  const response = await apiClient.post<{ requestId: string }>(
    `${ehrAdminPrefix}/api/v1.0/data-subject-rights/${patientId}/restriction`,
    { requestedBy, reason },
  );
  return response.data;
};

const hiePrefix = "/api/hie/api/v1.0/hie/consents";

type ConsentEnvelope = { data: Consent[]; links: unknown[] };

export const fetchConsentsForPatient = async (patientId: string): Promise<Consent[]> => {
  const response = await apiClient.get<ConsentEnvelope>(`${hiePrefix}/patient/${patientId}`);
  return response.data?.data ?? [];
};

export const grantConsent = async (request: {
  patientId: string;
  partnerId: string;
  scope: string;
  direction: number;
  effectiveFromUtc: string;
  effectiveToUtc: string | null;
}): Promise<{ consentId: string }> => {
  const response = await apiClient.post<{ data: { consentId: string } }>(hiePrefix, request);
  return { consentId: response.data?.data?.consentId };
};

export const revokeConsent = async (consentId: string): Promise<void> => {
  await apiClient.delete(`${hiePrefix}/${consentId}`);
};
