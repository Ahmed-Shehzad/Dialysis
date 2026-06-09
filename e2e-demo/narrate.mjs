#!/usr/bin/env node
// Post-process: turn the recorded scenario film into a narrated MP4.
//
// Reads the narration timeline the spec wrote (e2e-artifacts/mvp-demo/narration.json), synthesizes
// each cue to speech with macOS `say`, places each clip at its recorded video offset, mixes them into
// one track, and muxes that onto the recorded video.webm → dialysis-mvp-demo.mp4 (with audio).
//
// Usage:  node narrate.mjs            (run from e2e-demo/, after `npm run demo`)
// Requires: macOS `say`, `ffmpeg`/`ffprobe` on PATH.

import { execFileSync, spawnSync } from "node:child_process";
import { readFileSync, existsSync, mkdirSync, readdirSync } from "node:fs";
import { join, dirname } from "node:path";

const ART = join(process.cwd(), "..", "e2e-artifacts", "mvp-demo");
const manifest = JSON.parse(readFileSync(join(ART, "narration.json"), "utf8"));
const voice = process.env.DEMO_VOICE ?? manifest.voice ?? "Samantha";
const cues = manifest.cues ?? [];
if (!cues.length) throw new Error("no narration cues found");

// Locate the recorded video.
function findVideo(dir) {
  for (const e of readdirSync(dir, { withFileTypes: true })) {
    const p = join(dir, e.name);
    if (e.isDirectory()) {
      const f = findVideo(p);
      if (f) return f;
    } else if (e.name.endsWith(".webm")) return p;
  }
  return null;
}
const video = findVideo(join(ART, "test-results"));
if (!video) throw new Error("video.webm not found — run `npm run demo` first");
console.log("video:", video);

// Synthesize each cue to an AIFF clip.
const clipsDir = join(ART, "narration");
mkdirSync(clipsDir, { recursive: true });
const clips = cues.map((c, i) => {
  const out = join(clipsDir, `cue_${String(i).padStart(2, "0")}.aiff`);
  execFileSync("say", ["-v", voice, "-o", out, "--", c.text]);
  return { file: out, offsetMs: Math.round(c.offsetSec * 1000) };
});
console.log(`synthesized ${clips.length} narration clips (voice: ${voice})`);

// Build one ffmpeg call: video + N clips → delayed-and-mixed audio muxed onto the video.
const inputs = ["-i", video];
clips.forEach((c) => inputs.push("-i", c.file));
const filters = clips
  .map((c, i) => `[${i + 1}:a]adelay=delays=${c.offsetMs}:all=1[a${i}]`)
  .join(";");
const mix = clips.map((_, i) => `[a${i}]`).join("") + `amix=inputs=${clips.length}:normalize=0:dropout_transition=0[aout]`;
const out = join(ART, "dialysis-mvp-demo.mp4");

const args = [
  "-y",
  ...inputs,
  "-filter_complex", `${filters};${mix}`,
  "-map", "0:v",
  "-map", "[aout]",
  "-c:v", "libx264",
  "-preset", "veryfast",
  "-crf", "23",
  "-pix_fmt", "yuv420p",
  "-c:a", "aac",
  "-b:a", "160k",
  "-shortest",
  "-movflags", "+faststart",
  out,
];
console.log("muxing narrated MP4 …");
const r = spawnSync("ffmpeg", args, { stdio: ["ignore", "ignore", "inherit"] });
if (r.status !== 0) throw new Error("ffmpeg failed");
console.log("\n✅ narrated demo:", out);
