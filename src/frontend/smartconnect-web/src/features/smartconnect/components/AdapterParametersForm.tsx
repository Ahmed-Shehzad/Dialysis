import { useMemo } from "react";
import { type AdapterField, type AdapterSchema, getSchema } from "../api/adapterSchemas";

type Props = {
  kind: string;
  propertiesJson: string | null;
  onChange: (next: string | null) => void;
  /**
   * Optional resolver for non-outbound-adapter slots (route filters, transform stages). Defaults
   * to `getSchema` for the outbound-route editor. Pass `getRouteFilterSchema` /
   * `getTransformStageSchema` from `adapterSchemas.ts` for the upstream slots.
   */
  schemaResolver?: (kind: string) => AdapterSchema | undefined;
};

// Best-effort parse: handles empty / invalid JSON gracefully so the form keeps rendering even
// when the operator briefly types something malformed in a textarea field.
const parseSafely = (raw: string | null): Record<string, unknown> => {
  if (!raw || raw.trim() === "") return {};
  try {
    const parsed = JSON.parse(raw);
    return typeof parsed === "object" && parsed !== null ? (parsed as Record<string, unknown>) : {};
  } catch {
    return {};
  }
};

const coerce = (field: AdapterField, raw: string): unknown => {
  switch (field.type) {
    case "number":
      return raw === "" ? undefined : Number(raw);
    case "boolean":
      return raw === "true";
    case "json":
      try {
        return JSON.parse(raw);
      } catch {
        return raw; // keep the raw string so the operator can see + fix their JSON
      }
    default:
      return raw;
  }
};

const stringify = (field: AdapterField, value: unknown): string => {
  if (value === undefined || value === null) return "";
  if (field.type === "json" && typeof value === "object") return JSON.stringify(value, null, 2);
  return String(value);
};

export const AdapterParametersForm = ({
  kind,
  propertiesJson,
  onChange,
  schemaResolver = getSchema,
}: Props) => {
  const schema = useMemo(() => schemaResolver(kind), [schemaResolver, kind]);
  const current = useMemo(() => parseSafely(propertiesJson), [propertiesJson]);

  if (!schema) {
    // Unknown adapter kind — fall back to a free-form JSON textarea so the operator can still
    // wire it up; the back-end's PipelineValidation surfaces shape errors at POST time.
    return (
      <textarea
        aria-label="Adapter parameters JSON"
        value={propertiesJson ?? ""}
        onChange={(e) => onChange(e.target.value || null)}
        rows={4}
        placeholder='{"url":"https://partner.example/inbound"}'
        className="mt-2 w-full rounded-md border border-slate-700 bg-slate-900 px-2 py-1 font-mono text-[11px] text-slate-100"
      />
    );
  }

  const setFieldValue = (field: AdapterField, raw: string) => {
    const next = { ...current };
    const coerced = coerce(field, raw);
    if (coerced === undefined || coerced === "") {
      delete next[field.key];
    } else {
      next[field.key] = coerced;
    }
    onChange(Object.keys(next).length === 0 ? null : JSON.stringify(next, null, 2));
  };

  return (
    <div className="space-y-2">
      <p className="text-[11px] text-slate-500">{schema.description}</p>
      {schema.fields.length === 0 && (
        <p className="text-[11px] text-slate-500">This adapter has no configurable parameters.</p>
      )}
      {schema.fields.map((field) => {
        const raw = stringify(field, current[field.key] ?? field.defaultValue);
        const id = `route-${kind}-${field.key}`;
        return (
          <label key={field.key} htmlFor={id} className="block">
            <span className="text-[11px] text-slate-400">
              {field.label}
              {field.required && <span className="ml-1 text-rose-300">*</span>}
            </span>
            {field.type === "json" ? (
              <textarea
                id={id}
                value={raw}
                onChange={(e) => setFieldValue(field, e.target.value)}
                rows={2}
                placeholder={field.placeholder}
                className="mt-1 w-full rounded-md border border-slate-700 bg-slate-900 px-2 py-1 font-mono text-[11px] text-slate-100"
              />
            ) : field.type === "boolean" ? (
              <select
                id={id}
                value={raw === "true" ? "true" : "false"}
                onChange={(e) => setFieldValue(field, e.target.value)}
                className="mt-1 w-full rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-100"
              >
                <option value="false">false</option>
                <option value="true">true</option>
              </select>
            ) : (
              <input
                id={id}
                type={field.type === "number" ? "number" : "text"}
                value={raw}
                onChange={(e) => setFieldValue(field, e.target.value)}
                placeholder={field.placeholder}
                className="mt-1 w-full rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-100"
              />
            )}
            {field.hint && (
              <span className="mt-0.5 block text-[10px] text-slate-500">{field.hint}</span>
            )}
          </label>
        );
      })}
    </div>
  );
};
