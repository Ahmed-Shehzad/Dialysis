import { apiClient } from "@/lib/api/apiClient";
import { ADMIN_PREFIX, type CodeTemplateLibrary } from "./types";

const path = (id?: string) =>
  id
    ? `${ADMIN_PREFIX}/code-template-libraries/${id}`
    : `${ADMIN_PREFIX}/code-template-libraries`;

export const fetchCodeTemplateLibraries = async (): Promise<CodeTemplateLibrary[]> => {
  const res = await apiClient.get<CodeTemplateLibrary[]>(path());
  return res.data ?? [];
};

export const fetchCodeTemplateLibrary = async (
  id: string,
): Promise<CodeTemplateLibrary> => {
  const res = await apiClient.get<CodeTemplateLibrary>(path(id));
  return res.data;
};

export const createCodeTemplateLibrary = async (
  library: CodeTemplateLibrary,
): Promise<CodeTemplateLibrary> => {
  const res = await apiClient.post<CodeTemplateLibrary>(path(), library);
  return res.data;
};

export const updateCodeTemplateLibrary = async (
  library: CodeTemplateLibrary,
): Promise<CodeTemplateLibrary> => {
  const res = await apiClient.put<CodeTemplateLibrary>(path(library.id), library);
  return res.data;
};

export const deleteCodeTemplateLibrary = async (id: string): Promise<void> => {
  await apiClient.delete(path(id));
};

export const importCodeTemplateLibraries = async (
  libraries: CodeTemplateLibrary[],
): Promise<{ imported: string[] }> => {
  const res = await apiClient.post<{ imported: string[] }>(
    `${ADMIN_PREFIX}/code-template-libraries/import`,
    libraries,
  );
  return res.data;
};

export const importCodeTemplateLibrariesMirthXml = async (
  xml: string,
): Promise<{ imported: string[] }> => {
  const res = await apiClient.post<{ imported: string[] }>(
    `${ADMIN_PREFIX}/code-template-libraries/import-mirth-xml`,
    xml,
    { headers: { "Content-Type": "text/plain" } },
  );
  return res.data;
};
