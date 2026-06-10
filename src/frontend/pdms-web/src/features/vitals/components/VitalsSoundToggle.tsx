import type { VitalsSeverity } from "../lib/vitalsSeverity";

type Props = {
  enabled: boolean;
  severity: VitalsSeverity;
  /** True while the session is in progress — when false the control shows as idle. */
  active: boolean;
  onToggle: () => void;
};

/**
 * Chairside monitor-sound control. Off by default (browser autoplay policy needs a click to unlock
 * audio); once on, it shows which alarm tone is currently sounding so staff can see the audio matches
 * the patient state at a glance.
 */
const soundToneClass = (enabled: boolean, critical: boolean): string => {
  if (!enabled) return "border-slate-700 bg-slate-900/60 text-slate-300 hover:text-slate-100";
  if (critical) return "border-rose-500 bg-rose-950/50 text-rose-100 ring-1 ring-rose-500/50";
  return "border-emerald-700/60 bg-emerald-950/30 text-emerald-100";
};

export const VitalsSoundToggle = ({ enabled, severity, active, onToggle }: Props) => {
  const sounding = enabled && active;
  const critical = sounding && severity === "critical";

  return (
    <button
      type="button"
      onClick={onToggle}
      aria-pressed={enabled}
      aria-label={enabled ? "Mute monitor sound" : "Enable monitor sound"}
      className={
        "flex items-center gap-2 rounded-md border px-3 py-1.5 text-xs font-medium transition-colors " +
        soundToneClass(enabled, critical)
      }
    >
      <span aria-hidden>{enabled ? "🔊" : "🔇"}</span>
      <span>Monitor sound {enabled ? "on" : "off"}</span>
      {sounding && (
        <span
          aria-hidden
          className={
            "rounded-full px-2 py-0.5 text-[10px] uppercase tracking-wide " +
            (critical ? "bg-rose-500/30 text-rose-100" : "bg-emerald-500/20 text-emerald-100")
          }
        >
          {critical ? "Critical" : "Moderate"}
        </span>
      )}
    </button>
  );
};
