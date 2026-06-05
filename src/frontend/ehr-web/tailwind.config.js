/** @type {import('tailwindcss').Config} */

// Theme-aware tokens resolve to CSS custom properties (set in styles/index.css),
// so every existing `slate-*` / `clinic-50..300` utility flips with the
// dark/light toggle without touching a single component.
const v = (name) => `rgb(var(${name}) / <alpha-value>)`;

export default {
  darkMode: "class",
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        // Neutral surface/text ramp — fully CSS-variable driven (dark default,
        // light = reversed ramp). Overrides Tailwind's built-in slate.
        slate: {
          50: v("--c-slate-50"),
          100: v("--c-slate-100"),
          200: v("--c-slate-200"),
          300: v("--c-slate-300"),
          400: v("--c-slate-400"),
          500: v("--c-slate-500"),
          600: v("--c-slate-600"),
          700: v("--c-slate-700"),
          800: v("--c-slate-800"),
          900: v("--c-slate-900"),
          950: v("--c-slate-950"),
        },
        // Brand accent — clinical green, anchored at shade 500 (#00a97a).
        // 50–300 are text-only in the app and flip per theme (light mints on
        // dark; deep greens on light) for contrast; 400–900 are theme-agnostic
        // brand fills/borders.
        clinic: {
          50: v("--c-clinic-50"),
          100: v("--c-clinic-100"),
          200: v("--c-clinic-200"),
          300: v("--c-clinic-300"),
          400: "#1cb78b",
          500: "#00a97a",
          600: "#00855f",
          700: "#006e4f",
          800: "#015941",
          900: "#06432f",
        },
        vitals: {
          systolic: "#ef4444",
          diastolic: "#3b82f6",
          heart: "#f59e0b",
          ufrate: "#10b981",
        },
      },
      fontFamily: {
        mono: ["JetBrains Mono", "ui-monospace", "monospace"],
      },
    },
  },
  plugins: [],
};
