import { describe, it, expect, vi, beforeEach } from "vitest";
import type { PatientLabel } from "@/features/ehr/api/ehrApi";

// Mock the HTTP layer so we can assert how many times it is actually called.
vi.mock("@/features/ehr/api/ehrApi", () => ({
  fetchPatientsByIds: vi.fn(
    async (ids: string[]): Promise<PatientLabel[]> =>
      ids.map((id) => ({
        id,
        medicalRecordNumber: `MRN-${id}`,
        givenName: "Given",
        familyName: "Family",
        dateOfBirth: "2000-01-01",
      })),
  ),
}));

import { loadPatientLabel } from "./patientLoader";
import { fetchPatientsByIds } from "@/features/ehr/api/ehrApi";

describe("patientLoader (N+1 guard)", () => {
  beforeEach(() => vi.clearAllMocks());

  it("coalesces many concurrent loads issued in one tick into a SINGLE batched fetch", async () => {
    const ids = Array.from({ length: 50 }, (_, i) => `patient-${i}`);

    const results = await Promise.all(ids.map((id) => loadPatientLabel(id)));

    // The whole point: 50 rows -> 1 request, never 50.
    expect(fetchPatientsByIds).toHaveBeenCalledTimes(1);
    const firstBatch = vi.mocked(fetchPatientsByIds).mock.calls.at(0)?.[0] ?? [];
    expect(firstBatch).toHaveLength(50);
    expect(results).toHaveLength(50);
    expect(results[0]?.id).toBe("patient-0");
    expect(results[0]?.medicalRecordNumber).toBe("MRN-patient-0");
  });

  it("resolves an unknown id to null (UI then shows the placeholder, not an error)", async () => {
    vi.mocked(fetchPatientsByIds).mockResolvedValueOnce([]);
    const value = await loadPatientLabel("missing");
    expect(value).toBeNull();
  });

  it("degrades a failed batch to null for every waiter (a label must never break the page)", async () => {
    vi.mocked(fetchPatientsByIds).mockRejectedValueOnce(new Error("403"));
    const [a, b] = await Promise.all([loadPatientLabel("x"), loadPatientLabel("y")]);
    expect(a).toBeNull();
    expect(b).toBeNull();
  });
});
