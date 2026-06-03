import { apiClient } from "@/lib/api/apiClient";

export type QhinPartnerStatus = "Onboarding" | "Active" | "Suspended";
export type TrustAnchorStatus = "Active" | "Revoked";

export type QhinPartnerRow = {
  id: string;
  name: string;
  fhirBaseUrl: string;
  iasEndpoint: string;
  status: QhinPartnerStatus;
  mtlsCertThumbprint?: string | null;
  trustAnchorCount: number;
  updatedAtUtc: string;
  updatedBy: string;
};

export type QhinTrustAnchorRow = {
  id: string;
  subject: string;
  thumbprint: string;
  notBefore: string;
  notAfter: string;
  status: TrustAnchorStatus;
  attachedAtUtc: string;
  attachedBy: string;
};

export type QhinPartnerDetail = QhinPartnerRow & {
  createdAtUtc: string;
  trustAnchors: QhinTrustAnchorRow[];
};

type Envelope<T> = { data: T };

const base = "/api/hie/api/v1.0/tefca/partners";

export const fetchQhinPartners = async (): Promise<QhinPartnerRow[]> => {
  const response = await apiClient.get<Envelope<QhinPartnerRow[]>>(base);
  return response.data?.data ?? [];
};

export const fetchQhinPartner = async (id: string): Promise<QhinPartnerDetail | null> => {
  try {
    const response = await apiClient.get<Envelope<QhinPartnerDetail>>(`${base}/${id}`);
    return response.data?.data ?? null;
  } catch (error) {
    if ((error as { response?: { status?: number } }).response?.status === 404) return null;
    throw error;
  }
};

export type OnboardQhinInput = {
  name: string;
  fhirBaseUrl: string;
  iasEndpoint: string;
};

export const onboardQhinPartner = async (input: OnboardQhinInput): Promise<string> => {
  const response = await apiClient.post<Envelope<{ id: string }>>(base, input);
  return response.data.data.id;
};

export const reviseQhinPartner = async (id: string, input: OnboardQhinInput): Promise<void> => {
  await apiClient.put(`${base}/${id}`, input);
};

export const transitionQhinPartnerStatus = async (
  id: string,
  next: QhinPartnerStatus,
): Promise<void> => {
  await apiClient.post(`${base}/${id}/status`, { next });
};

export const attachTrustAnchor = async (id: string, certificatePem: string): Promise<string> => {
  const response = await apiClient.post<Envelope<{ anchorId: string }>>(
    `${base}/${id}/trust-anchors`,
    { certificatePem },
  );
  return response.data.data.anchorId;
};

export const revokeTrustAnchor = async (id: string, anchorId: string): Promise<void> => {
  await apiClient.delete(`${base}/${id}/trust-anchors/${anchorId}`);
};

export const rotateMtlsCertificate = async (
  id: string,
  base64Pfx: string,
  pfxPassword: string,
): Promise<string> => {
  const response = await apiClient.post<Envelope<{ thumbprint: string }>>(`${base}/${id}/mtls`, {
    base64Pfx,
    pfxPassword,
  });
  return response.data.data.thumbprint;
};

export const issueIasJwt = async (
  id: string,
  subjectPatientId: string,
  scope: string,
  lifetimeSeconds: number,
): Promise<string> => {
  const response = await apiClient.post<Envelope<{ token: string }>>(`${base}/${id}/ias-jwt`, {
    subjectPatientId,
    scope,
    lifetimeSeconds,
  });
  return response.data.data.token;
};

export const arrayBufferToBase64 = (buffer: ArrayBuffer): string => {
  let binary = "";
  const bytes = new Uint8Array(buffer);
  const chunk = 0x8000;
  for (let i = 0; i < bytes.length; i += chunk) {
    binary += String.fromCharCode.apply(null, Array.from(bytes.subarray(i, i + chunk)));
  }
  return btoa(binary);
};
