import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  DEMO_PROVIDER_ID,
  fetchPatientThreads,
  fetchThreadMessages,
  replyToThread,
  type SecureMessage,
} from "@/features/messaging/api/messagingApi";
import { humanizeError } from "@/lib/api/humanizeError";

const threadsKey = (patientId: string) => ["ehr", "messaging", patientId];
const threadKey = (patientId: string, threadId: string) => [
  "ehr",
  "messaging",
  patientId,
  threadId,
];

/**
 * Care-team side of two-way secure messaging on the chart. Lists the patient's threads and lets a
 * clinician reply; the reply pushes a real-time toast to the patient's portal session.
 */
export const MessagingCard = ({ patientId }: { patientId: string }) => {
  const queryClient = useQueryClient();
  const [openThread, setOpenThread] = useState<string | null>(null);
  const [reply, setReply] = useState("");

  const threads = useQuery({
    queryKey: threadsKey(patientId),
    queryFn: () => fetchPatientThreads(patientId),
    enabled: Boolean(patientId),
  });

  const messages = useQuery({
    queryKey: openThread ? threadKey(patientId, openThread) : ["ehr", "messaging", "none"],
    queryFn: () => fetchThreadMessages(patientId, openThread as string),
    enabled: Boolean(openThread),
  });

  const send = useMutation({
    mutationFn: (thread: { threadId: string; subject: string }) =>
      replyToThread(patientId, thread.threadId, {
        providerId: DEMO_PROVIDER_ID,
        subject: thread.subject.startsWith("Re:") ? thread.subject : `Re: ${thread.subject}`,
        body: reply.trim(),
      }),
    onSuccess: (_data, vars) => {
      setReply("");
      void queryClient.invalidateQueries({ queryKey: threadKey(patientId, vars.threadId) });
      void queryClient.invalidateQueries({ queryKey: threadsKey(patientId) });
    },
  });

  return (
    <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <h3 className="mb-2 text-sm font-medium text-slate-200">
        Patient messages <span className="text-slate-500">(secure)</span>
      </h3>

      {threads.isLoading && <p className="text-xs text-slate-400">Loading…</p>}
      {threads.error && <p className="text-xs text-amber-300">{humanizeError(threads.error)}</p>}
      {threads.data && threads.data.length === 0 && (
        <p className="text-xs text-slate-500">No messages from this patient.</p>
      )}

      {threads.data && threads.data.length > 0 && (
        <ul className="space-y-2">
          {threads.data.map((t) => {
            const open = openThread === t.threadId;
            return (
              <li key={t.threadId} className="rounded-md border border-slate-700">
                <button
                  type="button"
                  onClick={() => setOpenThread(open ? null : t.threadId)}
                  className="flex w-full items-center justify-between gap-2 px-3 py-2 text-left text-sm"
                >
                  <span className="text-slate-200">{t.subject}</span>
                  <span className="text-xs text-slate-500">
                    {t.messageCount} msg · {new Date(t.lastMessageAtUtc).toLocaleDateString()}
                  </span>
                </button>

                {open && (
                  <div className="space-y-2 border-t border-slate-800 px-3 py-2">
                    {messages.isLoading && <p className="text-xs text-slate-400">Loading…</p>}
                    {(messages.data ?? []).map((m: SecureMessage) => (
                      <div
                        key={m.id}
                        className={`rounded-md p-2 text-sm ${
                          m.direction === "ProviderToPatient"
                            ? "bg-clinic-950/50 text-clinic-50"
                            : "bg-slate-800/60 text-slate-200"
                        }`}
                      >
                        <p className="text-[10px] uppercase tracking-wide text-slate-500">
                          {m.direction === "ProviderToPatient" ? "Care team" : "Patient"} ·{" "}
                          {new Date(m.sentAtUtc).toLocaleString()}
                        </p>
                        <p className="whitespace-pre-wrap">{m.body}</p>
                      </div>
                    ))}
                    <div className="flex items-end gap-2">
                      <textarea
                        value={reply}
                        onChange={(e) => setReply(e.target.value)}
                        rows={2}
                        placeholder="Reply to the patient…"
                        aria-label="Reply to the patient"
                        className="flex-1 rounded-md border border-slate-700 bg-slate-950 px-2 py-1.5 text-sm text-slate-100"
                      />
                      <button
                        type="button"
                        onClick={() => send.mutate({ threadId: t.threadId, subject: t.subject })}
                        disabled={send.isPending || reply.trim().length === 0}
                        className="rounded-md bg-clinic-600 px-3 py-1.5 text-sm font-medium text-white transition hover:bg-clinic-500 disabled:cursor-not-allowed disabled:opacity-50"
                      >
                        {send.isPending ? "Sending…" : "Reply"}
                      </button>
                    </div>
                    {send.error && (
                      <p className="text-xs text-rose-300">{humanizeError(send.error)}</p>
                    )}
                  </div>
                )}
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
};
