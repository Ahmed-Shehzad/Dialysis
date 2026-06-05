// Apply the persisted/OS theme before first paint to avoid a flash.
// Dark is the CSS default; only add the `light` class when needed.
// Must mirror ThemeProvider (storage key + resolution order).
// Loaded as a same-origin <script src> (not inline) so it passes a strict
// `script-src 'self'` CSP in production.
(function () {
  let t = "dark";
  try {
    const stored = globalThis.localStorage.getItem("dialysis-theme");
    if (stored === "dark" || stored === "light") {
      t = stored;
    } else if (globalThis.matchMedia("(prefers-color-scheme: light)").matches) {
      t = "light";
    }
  } catch {
    t = "dark"; // storage/matchMedia unavailable — use the dark default
  }
  if (t === "light") document.documentElement.classList.add("light");
})();
