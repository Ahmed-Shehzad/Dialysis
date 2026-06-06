import { apiClient } from "@/lib/api/apiClient";

export type Charge = {
  chargeId: string;
  patientId: string;
  encounterId: string;
  cptCode: string;
  billedAmount: number;
  currencyCode: string;
  status: string;
  assignedClaimId: string | null;
  diagnosisPointerIcd10Codes: string[];
};

export type Claim = {
  claimId: string;
  patientId: string;
  payerId: string;
  payerCode: string;
  claimFormatCode: string;
  billedTotal: number;
  currencyCode: string;
  status: string;
  externalControlNumber: string | null;
  payerClaimControlNumber: string | null;
  submittedAtUtc: string | null;
  acknowledgedAtUtc: string | null;
  chargeCount: number;
  acknowledgementCount: number;
};

export type ClaimAck = {
  acknowledgementId: string;
  kind: string;
  verdict: string;
  payerClaimControlNumber: string | null;
  reasonCodes: string[];
  receivedAtUtc: string;
};

export type ClaimAcks = {
  claimId: string;
  status: string;
  externalControlNumber: string | null;
  payerClaimControlNumber: string | null;
  acknowledgedAtUtc: string | null;
  acknowledgements: ClaimAck[];
};

export type BillingExportJob = {
  id: string;
  payerCode: string;
  statusCode: string;
  periodStart: string;
  periodEnd: string;
  submittedAtUtc: string;
  completedAtUtc: string | null;
  notes: string | null;
};

const ehrPrefix = "/ehr/api/v1.0/billing";
const hisPrefix = "/api/his/api/v1.0/operations/billing";

export const fetchCharges = async (
  params: { status?: string; take?: number } = {},
): Promise<Charge[]> => {
  const response = await apiClient.get<Charge[]>(`${ehrPrefix}/charges`, { params });
  return response.data ?? [];
};

export const fetchClaims = async (
  params: { status?: string; take?: number } = {},
): Promise<Claim[]> => {
  const response = await apiClient.get<Claim[]>(`${ehrPrefix}/claims`, { params });
  return response.data ?? [];
};

export const fetchClaimAcks = async (claimId: string): Promise<ClaimAcks> => {
  const response = await apiClient.get<ClaimAcks>(`${ehrPrefix}/claims/${claimId}/acks`);
  return response.data;
};

export type LostCharge = {
  encounterId: string;
  patientId: string;
  providerId: string;
  closedAtUtc: string;
};

/** Closed encounters with no captured charge (lost-charge worklist). */
export const fetchLostCharges = async (
  params: { olderThanDays?: number; take?: number } = {},
): Promise<LostCharge[]> => {
  const response = await apiClient.get<LostCharge[]>(`${ehrPrefix}/worklist/lost-charges`, {
    params,
  });
  return response.data ?? [];
};

/** Captured charges aging without a claim (charge-lag / late-filing worklist). */
export const fetchChargeLag = async (
  params: { olderThanDays?: number; take?: number } = {},
): Promise<Charge[]> => {
  const response = await apiClient.get<Charge[]>(`${ehrPrefix}/worklist/charge-lag`, { params });
  return response.data ?? [];
};

/** Denied claims (appeal / resubmit worklist). */
export const fetchDenials = async (params: { take?: number } = {}): Promise<Claim[]> => {
  const response = await apiClient.get<Claim[]>(`${ehrPrefix}/worklist/denials`, { params });
  return response.data ?? [];
};

export const fetchBillingExportJobs = async (
  params: { status?: string; take?: number } = {},
): Promise<BillingExportJob[]> => {
  const response = await apiClient.get<BillingExportJob[]>(`${hisPrefix}/export-jobs`, { params });
  return response.data ?? [];
};
