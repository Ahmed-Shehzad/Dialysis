import type { InputHTMLAttributes, ReactNode } from "react";

type FormFieldProps = {
  label: string;
  hint?: string;
  children: ReactNode;
};

export const FormField = ({ label, hint, children }: FormFieldProps) => (
  <label className="flex flex-col gap-1 text-sm">
    <span className="text-xs uppercase tracking-wide text-slate-400">{label}</span>
    {children}
    {hint ? <span className="text-xs text-slate-500">{hint}</span> : null}
  </label>
);

export const TextInput = (props: InputHTMLAttributes<HTMLInputElement>) => (
  <input
    {...props}
    className={`rounded-md border border-slate-700 bg-slate-900 px-3 py-1.5 text-sm text-slate-100 placeholder-slate-500 focus:border-clinic-500 focus:outline-none ${props.className ?? ""}`}
  />
);

type WorkflowCardProps = {
  title: string;
  description?: string;
  onSubmit: () => void;
  isPending: boolean;
  errorMessage?: string;
  successMessage?: string;
  children: ReactNode;
  submitLabel?: string;
};

export const WorkflowCard = ({
  title,
  description,
  onSubmit,
  isPending,
  errorMessage,
  successMessage,
  children,
  submitLabel = "Submit",
}: WorkflowCardProps) => (
  <form
    onSubmit={(e) => {
      e.preventDefault();
      onSubmit();
    }}
    className="space-y-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4"
  >
    <header>
      <h3 className="text-sm font-medium text-slate-100">{title}</h3>
      {description ? <p className="text-xs text-slate-400">{description}</p> : null}
    </header>
    <div className="grid gap-3">{children}</div>
    <div className="flex items-center justify-between">
      <button
        type="submit"
        disabled={isPending}
        className="rounded-md bg-clinic-600 px-3 py-1.5 text-sm font-medium text-white transition hover:bg-clinic-700 disabled:opacity-40"
      >
        {isPending ? "Submitting…" : submitLabel}
      </button>
      <div className="text-xs">
        {errorMessage && <span className="text-rose-300">{errorMessage}</span>}
        {successMessage && <span className="text-emerald-300">{successMessage}</span>}
      </div>
    </div>
  </form>
);
