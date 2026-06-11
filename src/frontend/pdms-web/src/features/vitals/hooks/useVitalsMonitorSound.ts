import { useCallback, useEffect, useRef, useState } from "react";
import { VitalsAlarmAudioEngine } from "../audio/vitalsAlarmAudio";
import type { VitalsSeverity } from "../lib/vitalsSeverity";

const STORAGE_KEY = "pdms.chairside.monitorSound";

const loadPref = (): boolean => {
  try {
    return globalThis.localStorage?.getItem(STORAGE_KEY) === "on";
  } catch {
    return false;
  }
};

const savePref = (on: boolean): void => {
  try {
    globalThis.localStorage?.setItem(STORAGE_KEY, on ? "on" : "off");
  } catch {
    /* ignore storage failures (private mode etc.) */
  }
};

type Options = {
  /** Severity of the most recent reading. */
  severity: VitalsSeverity;
  /** Changes once per incoming reading (e.g. the reading id) — drives the per-reading moderate beep. */
  readingKey: string | undefined;
  /** Only play while the session is actually in progress. */
  active: boolean;
};

export type UseVitalsMonitorSoundResult = {
  enabled: boolean;
  /** Flip the sound on/off. Must be wired to a click so the AudioContext can be unlocked. */
  toggle: () => void;
};

/**
 * Drives the chairside monitor audio: a steady beep per reading while vitals are moderate, and a
 * continuous critical alarm while any reading is critical. The continuous tone follows `severity`;
 * the moderate beep fires on each new `readingKey`. Off by default (browsers block autoplay until a
 * gesture); the returned {@link UseVitalsMonitorSoundResult.toggle} unlocks and enables it.
 */
export const useVitalsMonitorSound = ({
  severity,
  readingKey,
  active,
}: Options): UseVitalsMonitorSoundResult => {
  const engineRef = useRef<VitalsAlarmAudioEngine | null>(null);
  engineRef.current ??= new VitalsAlarmAudioEngine();

  const [enabled, setEnabled] = useState<boolean>(loadPref);

  // Hold the latest control state in a ref so the per-reading effect can read it without depending
  // on it (we only want that effect to fire when a *new reading* arrives, not on severity flips).
  // The ref is written in an effect (not during render — react-hooks/refs); it is declared before
  // the per-reading effect below, so it always syncs first within the same commit.
  const stateRef = useRef({ enabled, active, severity });
  useEffect(() => {
    stateRef.current = { enabled, active, severity };
  }, [enabled, active, severity]);

  const toggle = useCallback(() => {
    setEnabled((prev) => {
      const next = !prev;
      savePref(next);
      const engine = engineRef.current;
      if (engine) {
        if (next) void engine.resume();
        else engine.stopAll();
      }
      return next;
    });
  }, []);

  // Tear the engine down on unmount.
  useEffect(() => {
    const engine = engineRef.current;
    return () => engine?.dispose();
  }, []);

  // Silence immediately whenever sound is off or the session is not in progress.
  useEffect(() => {
    if (!enabled || !active) engineRef.current?.stopAll();
  }, [enabled, active]);

  // The continuous critical alarm tracks severity while enabled + active.
  useEffect(() => {
    const engine = engineRef.current;
    if (!engine) return;
    if (enabled && active && severity === "critical") engine.startCriticalTone();
    else engine.stopCriticalTone();
  }, [enabled, active, severity]);

  // One monitor beep per new reading while moderate (critical uses the continuous tone above).
  useEffect(() => {
    if (!readingKey) return;
    const { enabled: on, active: live, severity: sev } = stateRef.current;
    if (on && live && sev === "moderate") engineRef.current?.playModerateBeep();
  }, [readingKey]);

  return { enabled, toggle };
};
