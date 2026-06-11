import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { FormField, TextInput, WorkflowCard } from "@/components/ui/FormField";
import { admitPatient, bookAppointment, placeMedicationOrder } from "@/features/his/api/hisApi";

const errorMessage = (err: unknown): string => {
  const status = (err as { response?: { status?: number } })?.response?.status;
  return status ? `Failed (HTTP ${status})` : "Request failed";
};

const AdmitPatientCard = () => {
  const [patientId, setPatientId] = useState("");
  const [wardCode, setWardCode] = useState("3W-NEPH");
  const [lastId, setLastId] = useState<string | null>(null);
  const queryClient = useQueryClient();

  const m = useMutation({
    mutationFn: () => admitPatient({ patientId, wardCode }),
    onSuccess: (id) => {
      setLastId(id);
      queryClient.invalidateQueries({ queryKey: ["his", "manager-dashboard"] });
      queryClient.invalidateQueries({ queryKey: ["his", "outbox-metadata"] });
    },
  });

  return (
    <WorkflowCard
      title="Admit patient"
      description="HIS PatientFlow slice — creates an inpatient admission."
      onSubmit={() => m.mutate()}
      isPending={m.isPending}
      errorMessage={m.error ? errorMessage(m.error) : undefined}
      successMessage={lastId ? `Admitted ✓ (id ${lastId.slice(0, 8)}…)` : undefined}
      submitLabel="Admit"
    >
      <FormField label="Patient id (Guid)">
        <TextInput
          required
          value={patientId}
          onChange={(e) => setPatientId(e.target.value)}
          placeholder="00000000-0000-0000-0000-000000000001"
        />
      </FormField>
      <FormField label="Ward code">
        <TextInput required value={wardCode} onChange={(e) => setWardCode(e.target.value)} />
      </FormField>
    </WorkflowCard>
  );
};

const BookAppointmentCard = () => {
  const [patientId, setPatientId] = useState("");
  const [providerId, setProviderId] = useState("");
  // Lazy initializer: Date.now() is impure, so it must not run on every render
  // (react-hooks/purity) — only once when the state is first created.
  const [start, setStart] = useState(() =>
    new Date(Date.now() + 24 * 3600 * 1000).toISOString().slice(0, 16),
  );
  const [duration, setDuration] = useState(30);
  const [lastId, setLastId] = useState<string | null>(null);

  const m = useMutation({
    mutationFn: () => {
      const startUtc = new Date(start).toISOString();
      const endUtc = new Date(new Date(start).getTime() + duration * 60_000).toISOString();
      return bookAppointment({ patientId, providerId, slotStartUtc: startUtc, slotEndUtc: endUtc });
    },
    onSuccess: setLastId,
  });

  return (
    <WorkflowCard
      title="Book appointment"
      description="HIS Scheduling slice — books a future slot for a patient with a provider."
      onSubmit={() => m.mutate()}
      isPending={m.isPending}
      errorMessage={m.error ? errorMessage(m.error) : undefined}
      successMessage={lastId ? `Booked ✓ (id ${lastId.slice(0, 8)}…)` : undefined}
      submitLabel="Book"
    >
      <FormField label="Patient id (Guid)">
        <TextInput required value={patientId} onChange={(e) => setPatientId(e.target.value)} />
      </FormField>
      <FormField label="Provider id (Guid)">
        <TextInput required value={providerId} onChange={(e) => setProviderId(e.target.value)} />
      </FormField>
      <FormField label="Slot start (local)">
        <TextInput
          type="datetime-local"
          required
          value={start}
          onChange={(e) => setStart(e.target.value)}
        />
      </FormField>
      <FormField label="Duration (minutes)">
        <TextInput
          type="number"
          min={5}
          max={240}
          value={duration}
          onChange={(e) => setDuration(Number(e.target.value))}
        />
      </FormField>
    </WorkflowCard>
  );
};

const PlaceMedicationCard = () => {
  const [patientId, setPatientId] = useState("");
  const [drugCode, setDrugCode] = useState("29046");
  const [dosage, setDosage] = useState("10 mg PO daily");
  const [lastId, setLastId] = useState<string | null>(null);

  const m = useMutation({
    mutationFn: () => placeMedicationOrder({ patientId, drugCode, dosage }),
    onSuccess: setLastId,
  });

  return (
    <WorkflowCard
      title="Place medication order"
      description="HIS Medication slice — issues a medication order for the patient."
      onSubmit={() => m.mutate()}
      isPending={m.isPending}
      errorMessage={m.error ? errorMessage(m.error) : undefined}
      successMessage={lastId ? `Ordered ✓ (id ${lastId.slice(0, 8)}…)` : undefined}
      submitLabel="Order"
    >
      <FormField label="Patient id (Guid)">
        <TextInput required value={patientId} onChange={(e) => setPatientId(e.target.value)} />
      </FormField>
      <FormField label="Drug code (RxNorm)">
        <TextInput required value={drugCode} onChange={(e) => setDrugCode(e.target.value)} />
      </FormField>
      <FormField label="Dosage">
        <TextInput required value={dosage} onChange={(e) => setDosage(e.target.value)} />
      </FormField>
    </WorkflowCard>
  );
};

export const HisWorkflowsPage = () => (
  <div className="space-y-4">
    <header>
      <h2 className="text-xl font-semibold text-clinic-50">HIS workflows</h2>
      <p className="text-sm text-slate-400">
        Demonstrates HIS PatientFlow, Scheduling, and Medication slices end-to-end.
      </p>
    </header>
    <div className="grid gap-4 lg:grid-cols-3">
      <AdmitPatientCard />
      <BookAppointmentCard />
      <PlaceMedicationCard />
    </div>
  </div>
);
