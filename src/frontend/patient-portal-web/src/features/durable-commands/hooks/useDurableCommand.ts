import { useEffect, useRef, useState } from "react";
import { useMutation, type UseMutationResult } from "@tanstack/react-query";
import {
  fetchDurableCommandStatus,
  type DurableCommandAcceptance,
  type DurableCommandStatus,
} from "../api/durableCommandsApi";
import { notify } from "../components/toastBus";

/**
 * Tracking state surfaced to call sites so a single component can render the
 * pending → applied → failed flow without subscribing to internal mutation state.
 */
export type DurableCommandTrackingState =
  | { phase: "idle" }
  | { phase: "enqueueing" }
  | { phase: "pending"; acceptance: DurableCommandAcceptance }
  | { phase: "applied"; acceptance: DurableCommandAcceptance; result?: unknown }
  | {
      phase: "failed";
      acceptance: DurableCommandAcceptance | null;
      error: string;
    };

export interface UseDurableCommandOptions<TVariables> {
  /** POSTs to the controller endpoint; returns the 202 acceptance envelope. */
  mutationFn: (variables: TVariables) => Promise<DurableCommandAcceptance>;
  /** Human-readable label for toast / progress copy. e.g. "session reading". */
  label: string;
  /**
   * Fires once the consumer applies the command on the server. Call sites can
   * invalidate TanStack Query keys / refresh dependent data here.
   */
  onApplied?: (result: DurableCommandStatus, acceptance: DurableCommandAcceptance) => void;
  /** Poll interval for the status endpoint. Defaults to 500 ms. */
  pollIntervalMs?: number;
  /**
   * Max time (ms) to keep polling before giving up and rendering the
   * acceptance as still-pending. The command remains durably in flight on the
   * server; the UI just stops watching. Default: 30 s.
   */
  maxPollMs?: number;
}

export type UseDurableCommandResult<TVariables> = UseMutationResult<
  DurableCommandAcceptance,
  Error,
  TVariables
> & {
  tracking: DurableCommandTrackingState;
};

/**
 * Wraps a write that publishes through the durable command bus. The mutation
 * function should call the controller endpoint that returns 202 + the acceptance
 * envelope; <see cref="useDurableCommand"/> handles the rest:
 *   * polls the status endpoint until the row flips to Applied / Failed
 *   * fires toasts on each transition (queued, applied, failed)
 *   * surfaces a `tracking` state callers can render alongside the form via
 *     <see cref="DurableCommandProgress"/>
 *
 * Usage:
 *   const qc = useQueryClient();
 *   const { mutate, tracking } = useDurableCommand({
 *     label: "session reading",
 *     mutationFn: vars => apiClient
 *       .post(`/portal/api/_x/pdms/api/v1.0/sessions/${sid}/readings`, vars)
 *       .then(r => r.data),
 *     onApplied: () => qc.invalidateQueries({ queryKey: ["readings", sid] }),
 *   });
 */
export const useDurableCommand = <TVariables>(
  options: UseDurableCommandOptions<TVariables>,
): UseDurableCommandResult<TVariables> => {
  const { label, onApplied, pollIntervalMs = 500, maxPollMs = 30_000, mutationFn } = options;

  const [tracking, setTracking] = useState<DurableCommandTrackingState>({ phase: "idle" });
  const cancelRef = useRef<() => void>(() => {});

  useEffect(() => () => cancelRef.current(), []);

  const mutation = useMutation<DurableCommandAcceptance, Error, TVariables>({
    mutationFn,
    onMutate: () => {
      setTracking({ phase: "enqueueing" });
      cancelRef.current();
    },
    onError: (error) => {
      setTracking({ phase: "failed", acceptance: null, error: error.message });
      notify({ kind: "error", message: `${label} failed: ${error.message}` });
    },
    onSuccess: (acceptance) => {
      setTracking({ phase: "pending", acceptance });
      notify({ kind: "info", message: `${label} queued — applying…` });
      // Begin polling. The interval has an upper bound so the polling stops
      // even if the consumer is wedged; the command stays durably in flight on
      // the server and the user can refresh to see the final state.
      const startedAt = Date.now();
      let stopped = false;
      const interval = window.setInterval(async () => {
        if (stopped) return;
        if (Date.now() - startedAt > maxPollMs) {
          stopped = true;
          window.clearInterval(interval);
          return;
        }
        try {
          const status = await fetchDurableCommandStatus(acceptance.statusEndpoint);
          if (status.status === "Applied") {
            stopped = true;
            window.clearInterval(interval);
            setTracking({ phase: "applied", acceptance, result: status.result });
            notify({ kind: "success", message: `${label} applied` });
            onApplied?.(status, acceptance);
          } else if (status.status === "Failed") {
            stopped = true;
            window.clearInterval(interval);
            const failureSummary =
              typeof status.failure === "object" && status.failure !== null
                ? JSON.stringify(status.failure)
                : String(status.failure ?? "unknown");
            setTracking({ phase: "failed", acceptance, error: failureSummary });
            notify({ kind: "error", message: `${label} failed: ${failureSummary}` });
          }
        } catch {
          // Transient poll failure — keep trying until maxPollMs.
        }
      }, pollIntervalMs);
      cancelRef.current = () => {
        stopped = true;
        window.clearInterval(interval);
      };
    },
  });

  return { ...mutation, tracking };
};
