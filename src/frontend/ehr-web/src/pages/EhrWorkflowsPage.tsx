import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { FormField, TextInput, WorkflowCard } from "@/components/ui/FormField";
import {
  orderLabTest,
  registerPatient,
  signClinicalNote,
  startEncounter,
} from "@/features/ehr/api/ehrApi";

const errorMessage = (err: unknown): string => {
  const status = (err as { response?: { status?: number } })?.response?.status;
  return status ? `Failed (HTTP ${status})` : "Request failed";
};

const RegisterPatientCard = () => {
  const queryClient = useQueryClient();
  const [mrn, setMrn] = useState("");
  const [family, setFamily] = useState("");
  const [given, setGiven] = useState("");
  const [dob, setDob] = useState("1980-01-01");
  const [sex, setSex] = useState<"male" | "female" | "">("");
  const [lastId, setLastId] = useState<string | null>(null);

  const m = useMutation({
    mutationFn: () =>
      registerPatient({
        medicalRecordNumber: mrn,
        familyName: family,
        givenName: given,
        dateOfBirth: dob,
        sexAtBirthCode: sex || undefined,
        preferredLanguageCode: "en-US",
      }),
    onSuccess: (id) => {
      setLastId(id);
      queryClient.invalidateQueries({ queryKey: ["ehr", "patients"] });
    },
  });

  return (
    <WorkflowCard
      title="Register patient"
      description="EHR Registration slice — creates the patient master record + emits a registration event consumed by HIS."
      onSubmit={() => m.mutate()}
      isPending={m.isPending}
      errorMessage={m.error ? errorMessage(m.error) : undefined}
      successMessage={lastId ? `Registered ✓ (id ${lastId.slice(0, 8)}…)` : undefined}
      submitLabel="Register"
    >
      <FormField label="MRN">
        <TextInput required value={mrn} onChange={(e) => setMrn(e.target.value)} />
      </FormField>
      <div className="grid grid-cols-2 gap-3">
        <FormField label="Family name">
          <TextInput required value={family} onChange={(e) => setFamily(e.target.value)} />
        </FormField>
        <FormField label="Given name">
          <TextInput required value={given} onChange={(e) => setGiven(e.target.value)} />
        </FormField>
      </div>
      <FormField label="Date of birth">
        <TextInput type="date" required value={dob} onChange={(e) => setDob(e.target.value)} />
      </FormField>
      <FormField label="Sex at birth">
        <select
          value={sex}
          onChange={(e) => setSex(e.target.value as "male" | "female" | "")}
          className="rounded-md border border-slate-700 bg-slate-900 px-3 py-1.5 text-sm text-slate-100"
        >
          <option value="">—</option>
          <option value="male">Male</option>
          <option value="female">Female</option>
        </select>
      </FormField>
    </WorkflowCard>
  );
};

const StartEncounterCard = () => {
  const [patientId, setPatientId] = useState("");
  const [providerId, setProviderId] = useState("");
  const [klass, setKlass] = useState("AMB");
  const [lastId, setLastId] = useState<string | null>(null);

  const m = useMutation({
    mutationFn: () =>
      startEncounter({ patientId, providerId, encounterClassCode: klass, appointmentId: null }),
    onSuccess: setLastId,
  });

  return (
    <WorkflowCard
      title="Start encounter"
      description="EHR ClinicalNotes slice — opens a clinical encounter for documentation."
      onSubmit={() => m.mutate()}
      isPending={m.isPending}
      errorMessage={m.error ? errorMessage(m.error) : undefined}
      successMessage={lastId ? `Opened ✓ (id ${lastId.slice(0, 8)}…)` : undefined}
      submitLabel="Open"
    >
      <FormField label="Patient id (Guid)">
        <TextInput required value={patientId} onChange={(e) => setPatientId(e.target.value)} />
      </FormField>
      <FormField label="Provider id (Guid)">
        <TextInput required value={providerId} onChange={(e) => setProviderId(e.target.value)} />
      </FormField>
      <FormField label="Class code" hint="AMB / IMP / EMER / HH">
        <TextInput required value={klass} onChange={(e) => setKlass(e.target.value)} />
      </FormField>
    </WorkflowCard>
  );
};

const SignNoteCard = () => {
  const [noteId, setNoteId] = useState("");
  const [providerId, setProviderId] = useState("");
  const [success, setSuccess] = useState(false);

  const m = useMutation({
    mutationFn: () => signClinicalNote({ noteId, signingProviderId: providerId }),
    onSuccess: () => setSuccess(true),
  });

  return (
    <WorkflowCard
      title="Sign clinical note"
      description="EHR ClinicalNotes slice — finalizes a draft note; emits ClinicalNoteSigned which HIE picks up."
      onSubmit={() => m.mutate()}
      isPending={m.isPending}
      errorMessage={m.error ? errorMessage(m.error) : undefined}
      successMessage={success ? "Signed ✓" : undefined}
      submitLabel="Sign"
    >
      <FormField label="Note id (Guid)">
        <TextInput required value={noteId} onChange={(e) => setNoteId(e.target.value)} />
      </FormField>
      <FormField label="Signing provider (Guid)">
        <TextInput required value={providerId} onChange={(e) => setProviderId(e.target.value)} />
      </FormField>
    </WorkflowCard>
  );
};

const OrderLabCard = () => {
  const [patientId, setPatientId] = useState("");
  const [encounterId, setEncounterId] = useState("");
  const [providerId, setProviderId] = useState("");
  const [facility, setFacility] = useState("LAB-MAIN");
  const [panels, setPanels] = useState("718-7,2160-0");
  const [lastId, setLastId] = useState<string | null>(null);

  const m = useMutation({
    mutationFn: () =>
      orderLabTest({
        patientId,
        encounterId,
        orderingProviderId: providerId,
        labFacilityCode: facility,
        loincPanelCodes: panels
          .split(",")
          .map((p) => p.trim())
          .filter(Boolean),
      }),
    onSuccess: (result) => setLastId(result.id),
  });

  return (
    <WorkflowCard
      title="Order lab test"
      description="EHR ClinicalNotes slice — places a lab order; downstream consumers receive a LabOrderPlaced event."
      onSubmit={() => m.mutate()}
      isPending={m.isPending}
      errorMessage={m.error ? errorMessage(m.error) : undefined}
      successMessage={lastId ? `Ordered ✓ (id ${lastId.slice(0, 8)}…)` : undefined}
      submitLabel="Order"
    >
      <FormField label="Patient id (Guid)">
        <TextInput required value={patientId} onChange={(e) => setPatientId(e.target.value)} />
      </FormField>
      <FormField label="Encounter id (Guid)">
        <TextInput required value={encounterId} onChange={(e) => setEncounterId(e.target.value)} />
      </FormField>
      <FormField label="Ordering provider (Guid)">
        <TextInput required value={providerId} onChange={(e) => setProviderId(e.target.value)} />
      </FormField>
      <FormField label="Lab facility">
        <TextInput required value={facility} onChange={(e) => setFacility(e.target.value)} />
      </FormField>
      <FormField label="LOINC panels (comma-separated)">
        <TextInput required value={panels} onChange={(e) => setPanels(e.target.value)} />
      </FormField>
    </WorkflowCard>
  );
};

export const EhrWorkflowsPage = () => (
  <div className="space-y-4">
    <header>
      <h2 className="text-xl font-semibold text-clinic-50">EHR workflows</h2>
      <p className="text-sm text-slate-400">
        Clinical actions: register a patient, start an encounter, sign a note, order labs.
      </p>
    </header>
    <div className="grid gap-4 lg:grid-cols-2">
      <RegisterPatientCard />
      <StartEncounterCard />
      <SignNoteCard />
      <OrderLabCard />
    </div>
  </div>
);
