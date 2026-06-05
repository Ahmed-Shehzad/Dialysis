import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  bindDevice,
  changeDeviceStatus,
  fetchDeviceTypes,
  fetchDevices,
  registerDevice,
  type DeviceStatus,
  type DeviceStatusAction,
  type DeviceSummary,
  type RegisterDeviceInput,
} from "@/features/devices/api/devicesApi";
import { humanizeError } from "@/lib/api/humanizeError";

const STATUS_TONE: Record<string, string> = {
  Registered: "border-slate-600 bg-slate-800/60 text-slate-300",
  Active: "border-emerald-700/70 bg-emerald-950/40 text-emerald-100",
  Suspended: "border-amber-700/70 bg-amber-950/30 text-amber-100",
  Retired: "border-rose-700/70 bg-rose-950/40 text-rose-100",
};

const statusTone = (status: DeviceStatus): string =>
  STATUS_TONE[status] ?? "border-slate-600 bg-slate-800/60 text-slate-300";

const formatTime = (iso?: string | null): string =>
  iso ? new Date(iso).toLocaleString() : "never";

/**
 * Steward console for the RPM device registry. Register a device against the configured
 * device-type catalog, watch the fleet (status + last-seen), bind a device to a patient, and
 * apply lifecycle transitions (suspend / activate / retire). Ingestion governs readings against
 * what is registered here.
 */
export const DeviceRegistryPage = () => {
  const queryClient = useQueryClient();
  const devices = useQuery({
    queryKey: ["his", "devices"],
    queryFn: () => fetchDevices(200),
    refetchInterval: 30_000,
  });
  const types = useQuery({
    queryKey: ["his", "device-types"],
    queryFn: fetchDeviceTypes,
    staleTime: 5 * 60_000,
  });

  const invalidate = () => void queryClient.invalidateQueries({ queryKey: ["his", "devices"] });

  const register = useMutation({ mutationFn: registerDevice, onSuccess: invalidate });
  const status = useMutation({
    mutationFn: ({ id, action }: { id: string; action: DeviceStatusAction }) =>
      changeDeviceStatus(id, action),
    onSuccess: invalidate,
  });
  const bind = useMutation({
    mutationFn: ({ id, patientId }: { id: string; patientId: string }) => bindDevice(id, patientId),
    onSuccess: invalidate,
  });

  const rows = devices.data ?? [];

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-lg font-semibold text-slate-100">Device registry</h1>
        <p className="text-sm text-slate-400">
          Remote-patient-monitoring devices. Readings are governed against what is registered here —
          a suspended or retired device is rejected, and a bound device only accepts its patient.
        </p>
      </header>

      <RegisterDeviceForm
        typeOptions={types.data ?? []}
        onSubmit={(input) => register.mutate(input)}
        pending={register.isPending}
        error={register.error ? humanizeError(register.error) : null}
      />

      <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
        <h2 className="mb-2 text-sm font-medium text-slate-200">
          Registered devices <span className="text-slate-500">({rows.length})</span>
        </h2>

        {devices.isLoading && <p className="text-sm text-slate-400">Loading devices…</p>}
        {devices.error && <p className="text-sm text-rose-300">{humanizeError(devices.error)}</p>}
        {!devices.isLoading && rows.length === 0 && (
          <p className="text-sm text-slate-500">No devices registered yet.</p>
        )}

        {rows.length > 0 && (
          <ul className="divide-y divide-slate-800 text-sm">
            {rows.map((d) => (
              <DeviceRow
                key={d.id}
                device={d}
                onStatus={(action) => status.mutate({ id: d.id, action })}
                onBind={(patientId) => bind.mutate({ id: d.id, patientId })}
                busy={status.isPending || bind.isPending}
              />
            ))}
          </ul>
        )}

        {(status.error || bind.error) && (
          <p role="alert" className="mt-2 text-xs text-rose-300">
            {humanizeError(status.error ?? bind.error)}
          </p>
        )}
      </section>
    </div>
  );
};

const RegisterDeviceForm = ({
  typeOptions,
  onSubmit,
  pending,
  error,
}: {
  typeOptions: { code: string; display: string }[];
  onSubmit: (input: RegisterDeviceInput) => void;
  pending: boolean;
  error: string | null;
}) => {
  const [deviceId, setDeviceId] = useState("");
  const [deviceTypeCode, setDeviceTypeCode] = useState("");
  const [manufacturer, setManufacturer] = useState("");
  const [model, setModel] = useState("");

  const canSubmit = deviceId.trim().length > 0 && deviceTypeCode.length > 0 && !pending;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!canSubmit) return;
    onSubmit({
      deviceId: deviceId.trim(),
      deviceTypeCode,
      manufacturer: manufacturer.trim() || null,
      model: model.trim() || null,
    });
    setDeviceId("");
    setManufacturer("");
    setModel("");
  };

  return (
    <form
      onSubmit={handleSubmit}
      className="grid gap-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4 sm:grid-cols-2 lg:grid-cols-5"
    >
      <label className="text-xs text-slate-300">
        <span className="mb-1 block">Device id</span>
        <input
          value={deviceId}
          onChange={(e) => setDeviceId(e.target.value)}
          placeholder="serial / gateway id"
          className="w-full rounded border border-slate-700 bg-slate-950 px-2 py-1.5 font-mono text-xs text-slate-100 focus:border-clinic-500 focus:outline-none"
        />
      </label>
      <label className="text-xs text-slate-300">
        <span className="mb-1 block">Type</span>
        <select
          value={deviceTypeCode}
          onChange={(e) => setDeviceTypeCode(e.target.value)}
          className="w-full rounded border border-slate-700 bg-slate-950 px-2 py-1.5 text-xs text-slate-100 focus:border-clinic-500 focus:outline-none"
        >
          <option value="">Select…</option>
          {typeOptions.map((t) => (
            <option key={t.code} value={t.code}>
              {t.display}
            </option>
          ))}
        </select>
      </label>
      <label className="text-xs text-slate-300">
        <span className="mb-1 block">Manufacturer</span>
        <input
          value={manufacturer}
          onChange={(e) => setManufacturer(e.target.value)}
          className="w-full rounded border border-slate-700 bg-slate-950 px-2 py-1.5 text-xs text-slate-100 focus:border-clinic-500 focus:outline-none"
        />
      </label>
      <label className="text-xs text-slate-300">
        <span className="mb-1 block">Model</span>
        <input
          value={model}
          onChange={(e) => setModel(e.target.value)}
          className="w-full rounded border border-slate-700 bg-slate-950 px-2 py-1.5 text-xs text-slate-100 focus:border-clinic-500 focus:outline-none"
        />
      </label>
      <div className="flex items-end">
        <button
          type="submit"
          disabled={!canSubmit}
          className="w-full rounded-md bg-clinic-600 px-3 py-2 text-sm font-medium text-white transition hover:bg-clinic-500 disabled:cursor-not-allowed disabled:opacity-50"
        >
          Register
        </button>
      </div>
      {error && (
        <p role="alert" className="text-xs text-rose-300 sm:col-span-2 lg:col-span-5">
          {error}
        </p>
      )}
    </form>
  );
};

const DeviceRow = ({
  device,
  onStatus,
  onBind,
  busy,
}: {
  device: DeviceSummary;
  onStatus: (action: DeviceStatusAction) => void;
  onBind: (patientId: string) => void;
  busy: boolean;
}) => {
  const retired = device.status === "Retired";
  const suspended = device.status === "Suspended";

  const handleBind = () => {
    const patientId = globalThis.prompt(`Bind ${device.deviceId} to patient id:`)?.trim();
    if (patientId) onBind(patientId);
  };

  return (
    <li className="grid grid-cols-12 items-center gap-2 py-2">
      <span
        className="col-span-3 truncate font-mono text-xs text-slate-200"
        title={device.deviceId}
      >
        {device.deviceId}
      </span>
      <span className="col-span-2 text-xs text-slate-400">{device.deviceTypeCode}</span>
      <span className="col-span-2">
        <span className={`rounded-full border px-2 py-0.5 text-xs ${statusTone(device.status)}`}>
          {device.status}
        </span>
      </span>
      <span
        className="col-span-2 truncate text-xs text-slate-400"
        title={device.patientId ?? undefined}
      >
        {device.patientId ? `patient ${device.patientId.slice(0, 8)}…` : "unbound"}
      </span>
      <span className="col-span-1 text-xs text-slate-500" title={device.lastSeenAtUtc ?? undefined}>
        {formatTime(device.lastSeenAtUtc)}
      </span>
      <span className="col-span-2 flex justify-end gap-1">
        {!retired && (
          <button
            type="button"
            onClick={handleBind}
            disabled={busy}
            className="rounded border border-slate-700 px-2 py-0.5 text-xs text-slate-200 transition hover:border-slate-500 disabled:opacity-50"
          >
            Bind
          </button>
        )}
        {!retired && suspended && (
          <button
            type="button"
            onClick={() => onStatus("Activate")}
            disabled={busy}
            className="rounded border border-emerald-700/60 px-2 py-0.5 text-xs text-emerald-200 transition hover:border-emerald-500 disabled:opacity-50"
          >
            Activate
          </button>
        )}
        {!retired && !suspended && (
          <button
            type="button"
            onClick={() => onStatus("Suspend")}
            disabled={busy}
            className="rounded border border-amber-700/60 px-2 py-0.5 text-xs text-amber-200 transition hover:border-amber-500 disabled:opacity-50"
          >
            Suspend
          </button>
        )}
        {!retired && (
          <button
            type="button"
            onClick={() => {
              if (globalThis.confirm(`Retire ${device.deviceId}? This is permanent.`))
                onStatus("Retire");
            }}
            disabled={busy}
            className="rounded border border-rose-700/60 px-2 py-0.5 text-xs text-rose-200 transition hover:border-rose-500 disabled:opacity-50"
          >
            Retire
          </button>
        )}
      </span>
    </li>
  );
};
