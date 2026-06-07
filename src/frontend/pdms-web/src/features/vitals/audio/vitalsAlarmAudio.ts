/**
 * Procedural Web Audio synthesiser for the chairside monitor sounds.
 *
 * The two modes are generated from oscillators rather than shipped audio files, so there are no
 * CORS, codec, autoplay, or licensing concerns and the two are crisply distinguishable:
 *   - moderate  → a single short ECG-style "beep" emitted per reading (the steady monitor cadence).
 *   - critical  → a continuous, attention-grabbing two-tone "flatline" alarm that runs until cleared.
 *
 * If a licensed clip is preferred later (e.g. a real heartbeat-monitor / flatline ringtone), swap the
 * bodies of {@link playModerateBeep} / {@link startCriticalTone} to drive an <audio>/AudioBuffer — the
 * public method contract the hook depends on stays the same.
 *
 * Browsers start an AudioContext suspended until a user gesture; call {@link resume} from a click
 * handler (the sound toggle) before expecting any output.
 */
export class VitalsAlarmAudioEngine {
  private context: AudioContext | null = null;
  private critical: { oscillators: OscillatorNode[]; gain: GainNode } | null = null;

  private ensureContext(): AudioContext | null {
    if (this.context) return this.context;
    const Ctor =
      window.AudioContext ??
      (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!Ctor) return null;
    this.context = new Ctor();
    return this.context;
  }

  /** Resume the audio context (must be invoked from a user gesture). */
  async resume(): Promise<void> {
    const ctx = this.ensureContext();
    if (ctx && ctx.state === "suspended") await ctx.resume();
  }

  /** One short monitor "beep" — call once per incoming reading while in the moderate state. */
  playModerateBeep(): void {
    const ctx = this.context;
    if (!ctx || ctx.state !== "running") return;
    const now = ctx.currentTime;
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.type = "sine";
    osc.frequency.setValueAtTime(660, now);
    // Quick attack, short decay → a crisp blip rather than a click.
    gain.gain.setValueAtTime(0.0001, now);
    gain.gain.exponentialRampToValueAtTime(0.22, now + 0.012);
    gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.15);
    osc.connect(gain).connect(ctx.destination);
    osc.start(now);
    osc.stop(now + 0.17);
  }

  /** Start the continuous critical alarm. Idempotent — a second call while running is a no-op. */
  startCriticalTone(): void {
    const ctx = this.context;
    if (!ctx || ctx.state !== "running" || this.critical) return;
    const now = ctx.currentTime;
    const gain = ctx.createGain();
    gain.gain.setValueAtTime(0.0001, now);
    gain.gain.exponentialRampToValueAtTime(0.16, now + 0.05);
    // Two slightly detuned tones beat against each other for a harsh, urgent "flatline" character.
    const a = ctx.createOscillator();
    a.type = "sawtooth";
    a.frequency.setValueAtTime(1000, now);
    const b = ctx.createOscillator();
    b.type = "sawtooth";
    b.frequency.setValueAtTime(1006, now);
    a.connect(gain);
    b.connect(gain);
    gain.connect(ctx.destination);
    a.start(now);
    b.start(now);
    this.critical = { oscillators: [a, b], gain };
  }

  /** Stop the continuous critical alarm with a short fade so it doesn't click. */
  stopCriticalTone(): void {
    const ctx = this.context;
    if (!ctx || !this.critical) return;
    const { oscillators, gain } = this.critical;
    const now = ctx.currentTime;
    gain.gain.cancelScheduledValues(now);
    gain.gain.setValueAtTime(Math.max(gain.gain.value, 0.0001), now);
    gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.08);
    oscillators.forEach((osc) => osc.stop(now + 0.1));
    this.critical = null;
  }

  /** Silence everything (leaves the context alive so it can resume without a new gesture). */
  stopAll(): void {
    this.stopCriticalTone();
  }

  /** Tear down the context entirely (component unmount). */
  dispose(): void {
    this.stopAll();
    void this.context?.close();
    this.context = null;
  }
}
