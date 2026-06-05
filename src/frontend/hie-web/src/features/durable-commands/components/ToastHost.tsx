import { useEffect, useState } from "react";
import { subscribe, dismiss, type ToastMessage } from "./toastBus";

/**
 * Subscribes to the toast bus and renders the buffer in a fixed-position stack.
 * Mount once at the shell root; toasts fired via `notify(...)` from anywhere in
 * the app appear here.
 */
export const ToastHost = (): JSX.Element => {
  const [toasts, setToasts] = useState<ToastMessage[]>([]);

  useEffect(() => subscribe(setToasts), []);

  return (
    <div
      aria-live="polite"
      aria-atomic="true"
      className="pointer-events-none fixed bottom-4 right-4 z-50 flex w-80 flex-col gap-2"
      data-testid="toast-host"
    >
      {toasts.map((toast) => (
        <button
          key={toast.id}
          type="button"
          onClick={() => dismiss(toast.id)}
          className={[
            "pointer-events-auto cursor-pointer rounded-lg border px-4 py-3 text-left text-sm shadow-lg",
            toast.kind === "success" && "border-emerald-600 bg-emerald-950/90 text-emerald-100",
            toast.kind === "error" && "border-rose-600 bg-rose-950/90 text-rose-100",
            toast.kind === "info" && "border-slate-700 bg-slate-900/95 text-slate-100",
          ]
            .filter(Boolean)
            .join(" ")}
          aria-label={`Dismiss ${toast.kind} toast`}
        >
          {toast.message}
        </button>
      ))}
    </div>
  );
};
