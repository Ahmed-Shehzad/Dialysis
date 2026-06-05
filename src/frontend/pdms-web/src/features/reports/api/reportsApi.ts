import { apiClient } from "@/lib/api/apiClient";

export type SessionReport = {
  id: string;
  sessionId: string;
  patientId: string;
  kind: "DischargeLetter" | "ShiftReport" | "BillingDocument";
  status: "Pending" | "Generated" | "Delivered" | "Archived" | "Failed";
  format: string;
  contentHash: string | null;
  storageRef: string | null;
  generatedAtUtc: string | null;
  deliveredAtUtc: string | null;
  failureReason: string | null;
};

export type ReportTemplate = {
  id: string;
  slug: string;
  kind: "DischargeLetter" | "ShiftReport" | "BillingDocument";
  title: string;
  languageCode: string | null;
  publishedVersionNumber: number | null;
  versions: ReportTemplateVersion[];
};

export type ReportTemplateVersion = {
  versionNumber: number;
  bodyMarkdown: string;
  authoredBySub: string;
  authoredAtUtc: string;
};

export const fetchSessionReports = async (sessionId: string): Promise<SessionReport[]> => {
  const response = await apiClient.get<SessionReport[]>(
    `/pdms/api/v1.0/sessions/${sessionId}/reports`,
  );
  return response.data ?? [];
};

export const reportDownloadUrl = (reportId: string): string =>
  `/pdms/api/v1.0/reports/${reportId}/content`;

/**
 * Downloads a rendered report's PDF bytes through the authenticated apiClient (so the Bearer
 * token is attached) and saves them via a transient object URL. A plain `<a href>` to the
 * /content endpoint can't carry the Authorization header and 401s against the PDMS API.
 */
export const downloadReportBinary = async (reportId: string, filename: string): Promise<void> => {
  const response = await apiClient.get<Blob>(reportDownloadUrl(reportId), { responseType: "blob" });
  const url = URL.createObjectURL(response.data);
  try {
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = filename;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
  } finally {
    URL.revokeObjectURL(url);
  }
};

export const fetchTemplates = async (kind?: ReportTemplate["kind"]): Promise<ReportTemplate[]> => {
  const response = await apiClient.get<ReportTemplate[]>(`/pdms/api/v1.0/reporting/templates`, {
    params: kind ? { kind } : undefined,
  });
  return response.data ?? [];
};

export type AppendTemplateVersionRequest = {
  slug: string;
  kind: ReportTemplate["kind"];
  title: string;
  bodyMarkdown: string;
  authoredBySub: string;
  // BCP-47 tag (e.g. "de", "en-US"); omit / empty for the language-neutral default.
  languageCode?: string | null;
};

export const appendTemplateVersion = async (
  request: AppendTemplateVersionRequest,
): Promise<ReportTemplate> => {
  const response = await apiClient.post<ReportTemplate>(
    `/pdms/api/v1.0/reporting/templates`,
    request,
  );
  return response.data;
};

export const publishTemplate = async (
  slug: string,
  versionNumber: number,
  languageCode?: string | null,
): Promise<ReportTemplate> => {
  const response = await apiClient.post<ReportTemplate>(
    `/pdms/api/v1.0/reporting/templates/${encodeURIComponent(slug)}/publish`,
    { versionNumber, languageCode: languageCode ?? null },
  );
  return response.data;
};
