import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { notify } from "@/features/durable-commands";
import {
  fetchMyThreads,
  fetchThreadMessages,
  markMessageRead,
  sendMessage,
  type SecureMessage,
} from "@/features/messages/api/messagesApi";
import { humanizeError } from "@/lib/api/humanizeError";

const threadsKey = (patientId: string) => ["patient-portal", "messages", patientId];
const threadKey = (patientId: string, threadId: string) => [
  "patient-portal",
  "messages",
  patientId,
  threadId,
];

/**
 * Two-way secure messaging with the care team. Patients start a thread or reply to one; care-team
 * replies arrive as a real-time toast (see usePatientPortalNotifications) and land here on refetch.
 */
export const MessagesPanel = ({ patientId }: { patientId: string }) => {
  const queryClient = useQueryClient();
  const [openThread, setOpenThread] = useState<string | null>(null);
  const [composing, setComposing] = useState(false);
  const [subject, setSubject] = useState("");
  const [body, setBody] = useState("");
  const [reply, setReply] = useState("");

  const threads = useQuery({
    queryKey: threadsKey(patientId),
    queryFn: () => fetchMyThreads(patientId),
  });

  const messages = useQuery({
    queryKey: openThread
      ? threadKey(patientId, openThread)
      : ["patient-portal", "messages", "none"],
    queryFn: () => fetchThreadMessages(patientId, openThread as string),
    enabled: Boolean(openThread),
  });

  // Mark unacknowledged care-team replies read once the thread is open.
  useEffect(() => {
    const unread = (messages.data ?? []).filter(
      (m) => m.direction === "ProviderToPatient" && !m.readAtUtc,
    );
    if (unread.length === 0) return;
    void Promise.all(unread.map((m) => markMessageRead(patientId, m.id))).then(() =>
      queryClient.invalidateQueries({ queryKey: threadsKey(patientId) }),
    );
  }, [messages.data, patientId, queryClient]);

  const startThread = useMutation({
    mutationFn: () => sendMessage(patientId, { subject: subject.trim(), body: body.trim() }),
    onSuccess: () => {
      notify({ kind: "success", message: "Message sent to your care team." });
      setComposing(false);
      setSubject("");
      setBody("");
      void queryClient.invalidateQueries({ queryKey: threadsKey(patientId) });
    },
  });

  const replyToThread = useMutation({
    mutationFn: (thread: { threadId: string; subject: string }) =>
      sendMessage(patientId, {
        threadId: thread.threadId,
        subject: thread.subject,
        body: reply.trim(),
      }),
    onSuccess: (_data, vars) => {
      setReply("");
      void queryClient.invalidateQueries({ queryKey: threadKey(patientId, vars.threadId) });
      void queryClient.invalidateQueries({ queryKey: threadsKey(patientId) });
    },
  });

  return (
    <section className="space-y-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <header className="flex items-center justify-between">
        <div>
          <h3 className="text-sm font-medium text-slate-200">Messages</h3>
          <p className="text-xs text-slate-400">Securely message your care team.</p>
        </div>
        <button
          type="button"
          onClick={() => setComposing((v) => !v)}
          className="rounded-md border border-slate-700 px-2.5 py-1 text-xs text-slate-200 transition hover:border-slate-500"
        >
          {composing ? "Cancel" : "+ New message"}
        </button>
      </header>

      {composing && (
        <div className="space-y-2 rounded-md border border-slate-700 bg-slate-950/40 p-3">
          <input
            type="text"
            value={subject}
            onChange={(e) => setSubject(e.target.value)}
            placeholder="Subject"
            aria-label="Subject"
            className="w-full rounded-md border border-slate-700 bg-slate-950 px-2 py-1.5 text-sm text-slate-100"
          />
          <textarea
            value={body}
            onChange={(e) => setBody(e.target.value)}
            rows={3}
            placeholder="How can we help?"
            aria-label="Message"
            className="w-full rounded-md border border-slate-700 bg-slate-950 px-2 py-1.5 text-sm text-slate-100"
          />
          {startThread.error && (
            <p className="text-xs text-rose-300">{humanizeError(startThread.error)}</p>
          )}
          <button
            type="button"
            onClick={() => startThread.mutate()}
            disabled={
              startThread.isPending || subject.trim().length === 0 || body.trim().length === 0
            }
            className="rounded-md bg-clinic-600 px-3 py-1.5 text-sm font-medium text-white transition hover:bg-clinic-500 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {startThread.isPending ? "Sending…" : "Send"}
          </button>
        </div>
      )}

      {threads.isLoading && <p className="text-xs text-slate-400">Loading your messages…</p>}
      {threads.error && <p className="text-xs text-rose-300">{humanizeError(threads.error)}</p>}
      {threads.data && threads.data.length === 0 && !composing && (
        <p className="rounded-md border border-dashed border-slate-700 p-3 text-xs text-slate-500">
          No messages yet. Start a conversation with your care team.
        </p>
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
                  <span className="flex items-center gap-2">
                    {t.unreadFromCareTeam > 0 && (
                      <span className="rounded-full bg-clinic-700 px-2 py-0.5 text-xs text-white">
                        {t.unreadFromCareTeam} new
                      </span>
                    )}
                    <span className="text-xs text-slate-500">
                      {new Date(t.lastMessageAtUtc).toLocaleDateString()}
                    </span>
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
                          {m.direction === "ProviderToPatient" ? "Care team" : "You"} ·{" "}
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
                        placeholder="Reply…"
                        aria-label="Reply"
                        className="flex-1 rounded-md border border-slate-700 bg-slate-950 px-2 py-1.5 text-sm text-slate-100"
                      />
                      <button
                        type="button"
                        onClick={() =>
                          replyToThread.mutate({
                            threadId: t.threadId,
                            subject: t.subject.startsWith("Re:") ? t.subject : `Re: ${t.subject}`,
                          })
                        }
                        disabled={replyToThread.isPending || reply.trim().length === 0}
                        className="rounded-md bg-clinic-600 px-3 py-1.5 text-sm font-medium text-white transition hover:bg-clinic-500 disabled:cursor-not-allowed disabled:opacity-50"
                      >
                        Send
                      </button>
                    </div>
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
