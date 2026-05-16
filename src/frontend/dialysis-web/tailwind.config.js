/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        // Brand accent palette — clinical green, anchored at shade 500
        // (#00a97a); full 50–900 ramp so the 100/200/300 shades the
        // components already reference actually resolve.
        clinic: {
          50: "#e6f7f1",
          100: "#c4ece0",
          200: "#93ddc6",
          300: "#59caa6",
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
