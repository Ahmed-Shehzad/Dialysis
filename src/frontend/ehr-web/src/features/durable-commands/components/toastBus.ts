/**
 * Minimal in-memory pub/sub for toasts. The host component (<ToastHost />)
 * subscribes once and renders the buffer; anywhere else in the app can call
 * <see cref="notify"/> to push a new toast.
 *
 * We deliberately do NOT pull in an external toast library — the surface area
 * is small enough (one host, three kinds, auto-dismiss) that a 20-line bus
 * keeps the SPA bundle smaller and the contract obvious.
 */
export type ToastKind = "info" | "success" | "error";

export interface ToastMessage {
  id: string;
  kind: ToastKind;
  message: string;
  createdAt: number;
}

type Listener = (toasts: ToastMessage[]) => void;

const _listeners = new Set<Listener>();
let _toasts: ToastMessage[] = [];

const _emit = () => _listeners.forEach((l) => l(_toasts));

export const subscribe = (listener: Listener): (() => void) => {
  _listeners.add(listener);
  listener(_toasts);
  return () => _listeners.delete(listener);
};

export interface NotifyInput {
  kind: ToastKind;
  message: string;
  /** Default 4 s. Set to 0 to suppress auto-dismiss. */
  ttlMs?: number;
}

export const notify = ({ kind, message, ttlMs = 4_000 }: NotifyInput): string => {
  const id = `${Date.now()}-${crypto.randomUUID().slice(0, 8)}`;
  _toasts = [..._toasts, { id, kind, message, createdAt: Date.now() }];
  _emit();
  if (ttlMs > 0) {
    window.setTimeout(() => dismiss(id), ttlMs);
  }
  return id;
};

export const dismiss = (id: string): void => {
  _toasts = _toasts.filter((t) => t.id !== id);
  _emit();
};
