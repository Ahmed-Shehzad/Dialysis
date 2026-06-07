import { useEffect, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  arrayBufferToBase64,
  attachTrustAnchor,
  fetchQhinPartner,
  fetchQhinPartners,
  issueIasJwt,
  onboardQhinPartner,
  reviseQhinPartner,
  revokeTrustAnchor,
  rotateMtlsCertificate,
  transitionQhinPartnerStatus,
  type QhinPartnerDetail,
  type QhinPartnerRow,
  type QhinPartnerStatus,
} from "@/features/tefca/api/tefcaApi";

/** Badge color for a partner's lifecycle status; Onboarding (and any future status) falls back to amber. */
const statusBadgeClass = (status: QhinPartnerStatus): string => {
  if (status === "Active") return "bg-emerald-900/40 text-emerald-200";
  if (status === "Suspended") return "bg-rose-900/40 text-rose-200";
  return "bg-amber-900/40 text-amber-200";
};

/**
 * Operator-facing TEFCA QHIN onboarding board. Lists every persistent partner row, with a
 * detail drawer for trust-anchor management, mTLS rotation, lifecycle transitions, and
 * IAS JWT issuance.
 *
 * No rows are seeded — the page renders an empty state until an operator onboards the
 * first partner.
 */
export const TefcaPartnersPage = () => {
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<"new" | { kind: "edit"; row: QhinPartnerRow } | null>(
    null,
  );
  const [openDetail, setOpenDetail] = useState<string | null>(null);

  const query = useQuery({
    queryKey: ["hie", "tefca", "partners"],
    queryFn: fetchQhinPartners,
  });

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["hie", "tefca"], exact: false });

  const rows = query.data ?? [];

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-lg font-semibold text-slate-100">TEFCA QHIN partners</h1>
          <p className="text-sm text-slate-400">
            US TEFCA Qualified Health Information Network partners. Each row carries its trust
            anchors, mTLS material, and lifecycle status. Onboard a partner here before turning them
            live in the outbound dispatcher.
          </p>
        </div>
        <button
          type="button"
          onClick={() => setEditing("new")}
          className="rounded bg-emerald-600 px-3 py-1.5 text-sm text-slate-50 hover:bg-emerald-500"
        >
          Onboard partner
        </button>
      </div>

      {query.isLoading && <div className="text-sm text-slate-400">Loading…</div>}
      {query.isError && (
        <div className="text-sm text-rose-300">Could not load partners. Retry shortly.</div>
      )}
      {!query.isLoading && rows.length === 0 && (
        <div className="rounded border border-dashed border-slate-700 p-6 text-sm text-slate-400">
          No QHIN partners onboarded yet — start by clicking "Onboard partner".
        </div>
      )}

      {rows.length > 0 && (
        <table className="w-full table-fixed border-collapse text-sm">
          <thead className="text-left text-slate-400">
            <tr>
              <th className="pb-2 font-medium">Name</th>
              <th className="w-28 pb-2 font-medium">Status</th>
              <th className="w-32 pb-2 font-medium">Anchors</th>
              <th className="w-32 pb-2 font-medium">mTLS</th>
              <th className="w-36 pb-2 font-medium">Updated</th>
              <th className="w-44 pb-2 font-medium" />
            </tr>
          </thead>
          <tbody className="text-slate-200">
            {rows.map((row) => (
              <tr key={row.id} className="border-t border-slate-800/60">
                <td className="py-2 align-top">
                  <div className="font-medium text-slate-100">{row.name}</div>
                  <div className="text-xs text-slate-500">{row.fhirBaseUrl}</div>
                </td>
                <td className="py-2 align-top text-xs">
                  <span className={"rounded px-1.5 py-0.5 " + statusBadgeClass(row.status)}>
                    {row.status}
                  </span>
                </td>
                <td className="py-2 align-top text-xs">{row.trustAnchorCount}×</td>
                <td className="py-2 align-top font-mono text-[10px]">
                  {row.mtlsCertThumbprint ?? "—"}
                </td>
                <td className="py-2 align-top font-mono text-xs">
                  {new Date(row.updatedAtUtc).toISOString().slice(0, 16).replace("T", " ")}
                </td>
                <td className="py-2 align-top text-right">
                  <button
                    type="button"
                    onClick={() => setOpenDetail(row.id)}
                    className="mr-2 rounded border border-slate-700 px-2 py-1 text-xs text-slate-200 hover:border-slate-500"
                  >
                    Manage
                  </button>
                  <button
                    type="button"
                    onClick={() => setEditing({ kind: "edit", row })}
                    className="rounded border border-slate-700 px-2 py-1 text-xs text-slate-200 hover:border-slate-500"
                  >
                    Edit
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {editing && (
        <PartnerEditDrawer
          row={editing === "new" ? null : editing.row}
          onClose={() => setEditing(null)}
          onApplied={() => {
            invalidate();
            setEditing(null);
          }}
        />
      )}

      {openDetail && (
        <PartnerDetailDrawer
          partnerId={openDetail}
          onClose={() => setOpenDetail(null)}
          onMutated={invalidate}
        />
      )}
    </div>
  );
};

const PartnerEditDrawer = ({
  row,
  onClose,
  onApplied,
}: {
  row: QhinPartnerRow | null;
  onClose: () => void;
  onApplied: () => void;
}) => {
  const [name, setName] = useState(row?.name ?? "");
  const [fhirBaseUrl, setFhirBaseUrl] = useState(row?.fhirBaseUrl ?? "");
  const [iasEndpoint, setIasEndpoint] = useState(row?.iasEndpoint ?? "");

  const upsert = useMutation({
    mutationFn: () =>
      row
        ? reviseQhinPartner(row.id, { name, fhirBaseUrl, iasEndpoint })
        : onboardQhinPartner({ name, fhirBaseUrl, iasEndpoint }).then(() => undefined),
    onSuccess: onApplied,
  });

  const canSave =
    name.trim().length > 0 &&
    fhirBaseUrl.trim().length > 0 &&
    iasEndpoint.trim().length > 0 &&
    !upsert.isPending;

  return (
    <DrawerShell title={row ? `Revise ${row.name}` : "Onboard partner"} onClose={onClose}>
      <Field label="Name">
        <input
          type="text"
          aria-label="Name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          className={inputClass}
        />
      </Field>
      <Field label="FHIR base URL">
        <input
          type="url"
          value={fhirBaseUrl}
          onChange={(e) => setFhirBaseUrl(e.target.value)}
          placeholder="https://qhin.example/fhir"
          className={inputClass}
        />
      </Field>
      <Field label="IAS endpoint">
        <input
          type="url"
          value={iasEndpoint}
          onChange={(e) => setIasEndpoint(e.target.value)}
          placeholder="https://qhin.example/ias"
          className={inputClass}
        />
      </Field>

      {upsert.isError && (
        <div className="mt-3 text-xs text-rose-300">Save failed — retry shortly.</div>
      )}

      <DrawerFooter
        onCancel={onClose}
        primary={
          <button
            type="button"
            onClick={() => upsert.mutate()}
            disabled={!canSave}
            className="rounded bg-emerald-600 px-3 py-1.5 text-slate-50 hover:bg-emerald-500 disabled:opacity-50"
          >
            {upsert.isPending ? "Saving…" : "Save"}
          </button>
        }
      />
    </DrawerShell>
  );
};

const PartnerDetailDrawer = ({
  partnerId,
  onClose,
  onMutated,
}: {
  partnerId: string;
  onClose: () => void;
  onMutated: () => void;
}) => {
  const queryClient = useQueryClient();
  const detail = useQuery({
    queryKey: ["hie", "tefca", "partners", partnerId],
    queryFn: () => fetchQhinPartner(partnerId),
  });

  const invalidate = () => {
    onMutated();
    queryClient.invalidateQueries({ queryKey: ["hie", "tefca", "partners", partnerId] });
  };

  const partner = detail.data;

  return (
    <DrawerShell title={partner?.name ?? "Partner"} onClose={onClose} wide>
      {detail.isLoading && <div className="text-sm text-slate-400">Loading…</div>}
      {partner && (
        <div className="space-y-5">
          <Section title="Lifecycle">
            <LifecycleControls partner={partner} onApplied={invalidate} />
          </Section>
          <Section title="Trust anchors">
            <TrustAnchorsPanel partner={partner} onApplied={invalidate} />
          </Section>
          <Section title="mTLS material">
            <MtlsRotatePanel partnerId={partner.id} onApplied={invalidate} />
            {partner.mtlsCertThumbprint && (
              <div className="mt-2 text-xs text-slate-400">
                Current thumbprint:{" "}
                <span className="font-mono text-[10px]">{partner.mtlsCertThumbprint}</span>
              </div>
            )}
          </Section>
          <Section title="Test IAS JWT">
            <IasJwtPanel partnerId={partner.id} />
          </Section>
        </div>
      )}
    </DrawerShell>
  );
};

const LifecycleControls = ({
  partner,
  onApplied,
}: {
  partner: QhinPartnerDetail;
  onApplied: () => void;
}) => {
  const transitionTo = useMutation({
    mutationFn: (next: QhinPartnerStatus) => transitionQhinPartnerStatus(partner.id, next),
    onSuccess: onApplied,
  });

  return (
    <div className="flex flex-wrap gap-2 text-xs">
      {(["Onboarding", "Active", "Suspended"] as const).map((status) => (
        <button
          key={status}
          type="button"
          onClick={() => transitionTo.mutate(status)}
          disabled={partner.status === status || transitionTo.isPending}
          className={
            "rounded border px-2 py-1 " +
            (partner.status === status
              ? "border-emerald-600 bg-emerald-900/30 text-emerald-200"
              : "border-slate-700 text-slate-200 hover:border-slate-500")
          }
        >
          {status}
        </button>
      ))}
      {transitionTo.isError && (
        <span className="text-xs text-rose-300">
          Transition rejected — partner needs trust anchor + mTLS first.
        </span>
      )}
    </div>
  );
};

const TrustAnchorsPanel = ({
  partner,
  onApplied,
}: {
  partner: QhinPartnerDetail;
  onApplied: () => void;
}) => {
  const [pem, setPem] = useState("");
  const attach = useMutation({
    mutationFn: () => attachTrustAnchor(partner.id, pem),
    onSuccess: () => {
      setPem("");
      onApplied();
    },
  });
  const revoke = useMutation({
    mutationFn: (anchorId: string) => revokeTrustAnchor(partner.id, anchorId),
    onSuccess: onApplied,
  });

  return (
    <div className="space-y-3">
      <ul className="space-y-1 text-xs">
        {partner.trustAnchors.map((a) => (
          <li
            key={a.id}
            className="flex items-start justify-between rounded border border-slate-800 p-2"
          >
            <div>
              <div className="text-slate-200">{a.subject}</div>
              <div className="font-mono text-[10px] text-slate-500">{a.thumbprint}</div>
              <div className="text-slate-500">
                {new Date(a.notBefore).toISOString().slice(0, 10)} →{" "}
                {new Date(a.notAfter).toISOString().slice(0, 10)} · {a.status}
              </div>
            </div>
            {a.status === "Active" && (
              <button
                type="button"
                onClick={() => revoke.mutate(a.id)}
                disabled={revoke.isPending}
                className="rounded border border-rose-700 px-2 py-1 text-rose-200 hover:border-rose-500"
              >
                Revoke
              </button>
            )}
          </li>
        ))}
      </ul>

      <label className="block text-xs">
        <span className="text-slate-400">Attach new anchor (PEM)</span>
        <textarea
          value={pem}
          onChange={(e) => setPem(e.target.value)}
          rows={5}
          placeholder="-----BEGIN CERTIFICATE-----&#10;…&#10;-----END CERTIFICATE-----"
          className={inputClass}
        />
      </label>
      <button
        type="button"
        onClick={() => attach.mutate()}
        disabled={pem.trim().length === 0 || attach.isPending}
        className="rounded bg-emerald-600 px-3 py-1.5 text-xs text-slate-50 hover:bg-emerald-500 disabled:opacity-50"
      >
        {attach.isPending ? "Attaching…" : "Attach"}
      </button>
      {attach.isError && (
        <span className="ml-2 text-xs text-rose-300">
          PEM rejected — check the certificate body.
        </span>
      )}
    </div>
  );
};

const MtlsRotatePanel = ({
  partnerId,
  onApplied,
}: {
  partnerId: string;
  onApplied: () => void;
}) => {
  const fileRef = useRef<HTMLInputElement | null>(null);
  const [password, setPassword] = useState("");
  const rotate = useMutation({
    mutationFn: async (file: File) => {
      const buffer = await file.arrayBuffer();
      return rotateMtlsCertificate(partnerId, arrayBufferToBase64(buffer), password);
    },
    onSuccess: onApplied,
  });

  return (
    <div className="space-y-2 text-xs">
      <label className="block">
        <span className="text-slate-400">PFX password</span>
        <input
          type="password"
          aria-label="PFX password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          className={inputClass}
        />
      </label>
      <input
        ref={fileRef}
        type="file"
        aria-label="Upload PFX certificate"
        className="hidden"
        accept=".pfx,application/x-pkcs12"
        onChange={(e) => {
          const file = e.target.files?.[0];
          if (!file) return;
          rotate.mutate(file);
          if (fileRef.current) fileRef.current.value = "";
        }}
      />
      <button
        type="button"
        onClick={() => fileRef.current?.click()}
        disabled={rotate.isPending}
        className="rounded bg-emerald-600 px-3 py-1.5 text-xs text-slate-50 hover:bg-emerald-500 disabled:opacity-50"
      >
        {rotate.isPending ? "Uploading…" : "Upload new PFX"}
      </button>
      {rotate.isError && (
        <span className="ml-2 text-rose-300">Rotation failed — verify the PFX password.</span>
      )}
    </div>
  );
};

const IasJwtPanel = ({ partnerId }: { partnerId: string }) => {
  const [subject, setSubject] = useState("");
  const [scope, setScope] = useState("patient.read");
  const [lifetime, setLifetime] = useState(300);

  const issue = useMutation({
    mutationFn: () => issueIasJwt(partnerId, subject.trim(), scope.trim(), lifetime),
  });

  const canIssue = subject.trim().length > 0 && !issue.isPending;

  return (
    <div className="space-y-3 text-xs">
      <Field label="Subject (patient id)">
        <input
          type="text"
          value={subject}
          onChange={(e) => setSubject(e.target.value)}
          placeholder="UUID"
          className={inputClass}
        />
      </Field>
      <Field label="Scope">
        <input
          type="text"
          aria-label="Scope"
          value={scope}
          onChange={(e) => setScope(e.target.value)}
          className={inputClass}
        />
      </Field>
      <Field label="Lifetime (seconds, 60–3600)">
        <input
          type="number"
          aria-label="Lifetime in seconds"
          min={60}
          max={3600}
          value={lifetime}
          onChange={(e) => setLifetime(Number.parseInt(e.target.value, 10) || 0)}
          className={inputClass}
        />
      </Field>
      <button
        type="button"
        onClick={() => issue.mutate()}
        disabled={!canIssue}
        className="rounded bg-emerald-600 px-3 py-1.5 text-xs text-slate-50 hover:bg-emerald-500 disabled:opacity-50"
      >
        {issue.isPending ? "Issuing…" : "Issue test IAS JWT"}
      </button>
      {issue.isError && (
        <div className="text-rose-300">
          Issuance failed — confirm Tefca:IasJwtIssuer:SigningKey is configured on the host.
        </div>
      )}
      {issue.data && (
        <div className="space-y-1">
          <div className="text-slate-400">Token</div>
          <textarea
            readOnly
            aria-label="Issued IAS token"
            value={issue.data}
            rows={6}
            className="w-full break-all rounded border border-slate-700 bg-slate-800/60 p-2 font-mono text-[10px] text-emerald-100"
          />
        </div>
      )}
    </div>
  );
};

// --- Layout helpers ---------------------------------------------------------

const inputClass = "mt-1 w-full rounded border border-slate-700 bg-slate-800/60 p-2 text-slate-100";

const Field = ({ label, children }: { label: string; children: React.ReactNode }) => (
  <label className="block text-sm">
    <span className="text-slate-400">{label}</span>
    {children}
  </label>
);

const Section = ({ title, children }: { title: string; children: React.ReactNode }) => (
  <div>
    <h3 className="mb-2 text-sm font-semibold text-slate-200">{title}</h3>
    {children}
  </div>
);

const DrawerShell = ({
  title,
  onClose,
  children,
  wide,
}: {
  title: string;
  onClose: () => void;
  children: React.ReactNode;
  wide?: boolean;
}) => {
  const ref = useRef<HTMLDialogElement>(null);
  // A native <dialog> opened with showModal() gives the proper dialog role, focus trapping,
  // and Escape-to-close on every platform — more robust than a div with role="dialog".
  useEffect(() => {
    const dialog = ref.current;
    dialog?.showModal();
    return () => dialog?.close();
  }, []);

  return (
    <dialog
      ref={ref}
      aria-label={title}
      // Only user-initiated dismissals must bubble up to the parent (which unmounts the drawer).
      // Do NOT wire the native `close` event to onClose: React StrictMode double-invokes the
      // effect above (setup → cleanup → setup), and the cleanup's programmatic dialog.close()
      // queues a `close` event that fires *after* the second setup has re-opened the dialog.
      // Routing that through onClose would unmount the freshly-opened drawer, so it flashes and
      // disappears — i.e. clicking the trigger button looks like "nothing happens". Escape fires
      // `cancel` (handled here); clicking the backdrop is matched via e.target === the dialog.
      onCancel={onClose}
      onClick={(e) => {
        if (e.target === ref.current) onClose();
      }}
      className="fixed inset-0 z-40 m-0 flex h-full max-h-none w-full max-w-none items-center justify-end border-0 bg-transparent p-0 text-slate-100 backdrop:bg-slate-950/70"
    >
      <div
        className={
          "h-full overflow-y-auto border-l border-slate-800 bg-slate-900 p-5 shadow-xl " +
          (wide ? "w-full max-w-lg" : "w-full max-w-sm")
        }
      >
        <div className="mb-3 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-slate-100">{title}</h2>
          <button
            type="button"
            onClick={onClose}
            className="rounded border border-slate-700 px-2 py-1 text-xs text-slate-200 hover:border-slate-500"
          >
            Close
          </button>
        </div>
        {children}
      </div>
    </dialog>
  );
};

const DrawerFooter = ({
  onCancel,
  primary,
}: {
  onCancel: () => void;
  primary: React.ReactNode;
}) => (
  <div className="mt-5 flex justify-end gap-2 text-sm">
    <button
      type="button"
      onClick={onCancel}
      className="rounded border border-slate-700 px-3 py-1.5 text-slate-200 hover:border-slate-500"
    >
      Cancel
    </button>
    {primary}
  </div>
);

export default TefcaPartnersPage;
