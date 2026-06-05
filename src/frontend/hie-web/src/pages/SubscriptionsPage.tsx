import { useMemo, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import {
  createSubscription,
  deleteSubscription,
  listSubscriptionTopics,
  MODULE_LABELS,
  simulateSubscriptionEvent,
  SUBSCRIPTION_MODULES,
  type SubscriptionModule,
  type SubscriptionTopic,
} from "@/features/subscriptions/api/subscriptionsApi";
import { useSubscriptionStream } from "@/features/subscriptions/hooks/useSubscriptionStream";
import { FormField, TextInput } from "@/components/ui/FormField";
import { StatusBadge } from "@/components/ui/StatusBadge";

type ActiveSubscription = {
  module: SubscriptionModule;
  id: string;
  topicUrl: string;
  topicTitle: string;
  /** Filters used at subscribe time — replayed as $simulate attributes so the event matches. */
  filters: Record<string, string>;
};

const errorMessage = (err: unknown): string => {
  const status = (err as { response?: { status?: number } })?.response?.status;
  if (status === 404) return "Unknown topic (host rejected it)";
  if (status === 401 || status === 403) return "Not authorized";
  return status ? `Failed (HTTP ${status})` : "Request failed — is the module host running?";
};

const TopicCard = ({
  module,
  topic,
  onSubscribed,
  disabled,
}: {
  module: SubscriptionModule;
  topic: SubscriptionTopic;
  onSubscribed: (sub: ActiveSubscription) => void;
  disabled: boolean;
}) => {
  const [filters, setFilters] = useState<Record<string, string>>({});

  const m = useMutation({
    mutationFn: async () => {
      const activeFilters = Object.fromEntries(
        Object.entries(filters).filter(([, v]) => v.trim().length > 0),
      );
      const reg = await createSubscription(module, {
        topic: topic.url,
        channelType: "ServerSentEvents",
        channelEndpoint: "sse:browser",
        filters: activeFilters,
      });
      return { reg, activeFilters };
    },
    onSuccess: ({ reg, activeFilters }) =>
      onSubscribed({
        module,
        id: reg.id,
        topicUrl: topic.url,
        topicTitle: topic.title,
        filters: activeFilters,
      }),
  });

  return (
    <section className="space-y-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <header>
        <h3 className="text-sm font-medium text-slate-100">{topic.title}</h3>
        <p className="text-xs text-slate-400">{topic.description}</p>
        <p className="mt-1 break-all font-mono text-[11px] text-slate-500">{topic.url}</p>
      </header>

      {topic.filterParameterNames.length > 0 && (
        <div className="grid gap-2 sm:grid-cols-2">
          {topic.filterParameterNames.map((name) => (
            <FormField key={name} label={name}>
              <TextInput
                value={filters[name] ?? ""}
                placeholder="optional filter"
                onChange={(e) => setFilters((p) => ({ ...p, [name]: e.target.value }))}
              />
            </FormField>
          ))}
        </div>
      )}

      <div className="flex items-center gap-3">
        <button
          type="button"
          onClick={() => m.mutate()}
          disabled={m.isPending || disabled}
          className="rounded-md bg-clinic-600 px-3 py-1.5 text-sm font-medium text-white transition hover:bg-clinic-700 disabled:opacity-40"
        >
          {m.isPending ? "Subscribing…" : "Subscribe (live SSE)"}
        </button>
        {m.error && <span className="text-xs text-rose-300">{errorMessage(m.error)}</span>}
      </div>
    </section>
  );
};

const LiveFeed = ({ active, onStop }: { active: ActiveSubscription; onStop: () => void }) => {
  const { notifications, status, clear } = useSubscriptionStream(active.module, active.id);

  const stopM = useMutation({
    mutationFn: () => deleteSubscription(active.module, active.id),
    onSettled: onStop,
  });

  const simM = useMutation({
    mutationFn: () =>
      simulateSubscriptionEvent(active.module, {
        topic: active.topicUrl,
        attributes: active.filters,
        note: `demo @ ${new Date().toLocaleTimeString()}`,
      }),
  });

  return (
    <section className="space-y-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <header className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <h3 className="text-sm font-medium text-slate-100">
            Live notifications — {active.topicTitle}
          </h3>
          <p className="font-mono text-[11px] text-slate-500">
            {MODULE_LABELS[active.module]} · subscription {active.id}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <StatusBadge status={status} />
          <button
            type="button"
            onClick={() => simM.mutate()}
            disabled={simM.isPending}
            className="rounded-md bg-clinic-600 px-2 py-1 text-xs font-medium text-white hover:bg-clinic-700 disabled:opacity-40"
          >
            {simM.isPending ? "Simulating…" : "Simulate event"}
          </button>
          <button
            type="button"
            onClick={clear}
            className="rounded-md border border-slate-700 px-2 py-1 text-xs text-slate-300 hover:bg-slate-800"
          >
            Clear
          </button>
          <button
            type="button"
            onClick={() => stopM.mutate()}
            disabled={stopM.isPending}
            className="rounded-md bg-rose-600 px-2 py-1 text-xs font-medium text-white hover:bg-rose-700 disabled:opacity-40"
          >
            {stopM.isPending ? "Stopping…" : "Unsubscribe"}
          </button>
        </div>
      </header>

      {simM.data && (
        <p className="text-xs text-emerald-300">
          Simulated → {simM.data.matched} subscription{simM.data.matched === 1 ? "" : "s"} matched (
          {simM.data.resourceType}). Watch the feed below.
        </p>
      )}
      {simM.error && <p className="text-xs text-rose-300">{errorMessage(simM.error)}</p>}

      {notifications.length === 0 ? (
        <p className="text-xs text-slate-500">
          Waiting for events. Click <span className="text-slate-300">Simulate event</span> above to
          fan a synthetic event through the real matcher and dispatcher, or trigger the upstream
          workflow (admission, lab result, intradialytic adverse event) — matching notification
          Bundles appear here in real time.
        </p>
      ) : (
        <ul className="space-y-2">
          {notifications.map((n) => (
            <li
              key={n.seq}
              className="rounded-md border border-slate-700 bg-slate-950 p-2 text-xs text-slate-300"
            >
              <div className="mb-1 font-mono text-[11px] text-slate-500">{n.receivedAt}</div>
              <pre className="max-h-72 overflow-auto whitespace-pre-wrap break-all">
                {typeof n.payload === "string" ? n.payload : JSON.stringify(n.payload, null, 2)}
              </pre>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
};

export const SubscriptionsPage = () => {
  const [module, setModule] = useState<SubscriptionModule>("pdms");
  const [active, setActive] = useState<ActiveSubscription | null>(null);

  const topicsQuery = useQuery({
    queryKey: ["subscription-topics", module],
    queryFn: () => listSubscriptionTopics(module),
  });

  const topics = useMemo(() => topicsQuery.data ?? [], [topicsQuery.data]);

  return (
    <div className="space-y-4">
      <header>
        <h2 className="text-xl font-semibold text-clinic-50">FHIR Subscriptions (real-time)</h2>
        <p className="text-sm text-slate-400">
          R4 Subscription Backport topics published per module. Subscribe to a topic and the
          building block streams matching{" "}
          <span className="font-mono">subscription-notification</span> Bundles over Server-Sent
          Events as integration events fire. Use{" "}
          <span className="text-slate-300">Simulate event</span> to demo the end-to-end pipeline
          without driving the upstream workflow.
        </p>
      </header>

      <div className="flex flex-wrap gap-1">
        {SUBSCRIPTION_MODULES.map((mod) => (
          <button
            key={mod}
            type="button"
            onClick={() => setModule(mod)}
            className={`rounded-md px-3 py-1.5 text-sm font-medium transition ${
              module === mod
                ? "bg-clinic-600 text-white"
                : "border border-slate-700 text-slate-300 hover:bg-slate-800"
            }`}
          >
            {MODULE_LABELS[mod]}
          </button>
        ))}
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <div className="space-y-4">
          {topicsQuery.isLoading && (
            <p className="text-sm text-slate-500">Loading topic catalog…</p>
          )}
          {topicsQuery.isError && (
            <p className="text-sm text-rose-300">
              Could not load topics — the {MODULE_LABELS[module]} host may be offline or
              subscriptions disabled for that module.
            </p>
          )}
          {!topicsQuery.isLoading && !topicsQuery.isError && topics.length === 0 && (
            <p className="text-sm text-slate-500">No topics published by this module host.</p>
          )}
          {topics.map((t) => (
            <TopicCard
              key={t.url}
              module={module}
              topic={t}
              disabled={active !== null}
              onSubscribed={setActive}
            />
          ))}
        </div>

        <div>
          {active ? (
            <LiveFeed active={active} onStop={() => setActive(null)} />
          ) : (
            <section className="rounded-lg border border-dashed border-slate-700 bg-slate-900/30 p-6 text-sm text-slate-500">
              Subscribe to a topic on the left to open a live notification stream.
            </section>
          )}
        </div>
      </div>
    </div>
  );
};
