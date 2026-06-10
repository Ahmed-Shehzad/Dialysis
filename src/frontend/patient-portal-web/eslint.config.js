// Flat ESLint config (ESLint v9+). Wires TypeScript + React + React Hooks +
// React Refresh + jsx-a11y (accessibility), then turns off every rule Prettier
// owns via eslint-config-prettier.
// Run with `npm run lint`; CI fails on any warning (--max-warnings=0).
import js from "@eslint/js";
import globals from "globals";
import tseslint from "typescript-eslint";
import react from "eslint-plugin-react";
import reactHooks from "eslint-plugin-react-hooks";
import reactRefresh from "eslint-plugin-react-refresh";
import jsxA11y from "eslint-plugin-jsx-a11y";
import prettier from "eslint-config-prettier";

export default tseslint.config(
  {
    ignores: [
      "dist",
      "build",
      "coverage",
      "node_modules",
      "test-results",
      "playwright-report",
      "**/*.tsbuildinfo",
      "e2e/__shots__",
    ],
  },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  {
    files: ["**/*.{ts,tsx}"],
    languageOptions: {
      ecmaVersion: 2022,
      sourceType: "module",
      globals: { ...globals.browser, ...globals.node },
      parserOptions: {
        ecmaFeatures: { jsx: true },
      },
    },
    settings: {
      react: { version: "detect" },
    },
    plugins: {
      react,
      "react-hooks": reactHooks,
      "react-refresh": reactRefresh,
      "jsx-a11y": jsxA11y,
    },
    rules: {
      ...react.configs.recommended.rules,
      ...react.configs["jsx-runtime"].rules,
      ...reactHooks.configs.recommended.rules,
      ...jsxA11y.flatConfigs.recommended.rules,
      // Every autoFocus in these apps is either the initial focus of a modal
      // dialog (required by the WAI-ARIA dialog pattern — focus must move into
      // the dialog when it opens) or the primary input of a dedicated search
      // page. The rule targets focus-stealing on page load, which none of
      // these are, so it is tuned off rather than evaded via ref+effect.
      "jsx-a11y/no-autofocus": "off",
      // Vite HMR hint, not a correctness concern; codebase legitimately
      // colocates context constants with provider components.
      "react-refresh/only-export-components": "off",
      // Purely cosmetic in JSX text — React renders apostrophes/quotes fine.
      "react/no-unescaped-entities": "off",
      // The codebase relies on TS for prop types — disable the legacy PropTypes rule.
      "react/prop-types": "off",
      // Trailing args prefixed with _ are intentional placeholders.
      "@typescript-eslint/no-unused-vars": [
        "error",
        { argsIgnorePattern: "^_", varsIgnorePattern: "^_" },
      ],
    },
  },
  {
    files: ["**/*.{js,cjs,mjs}"],
    languageOptions: {
      ecmaVersion: 2022,
      sourceType: "module",
      globals: { ...globals.node },
    },
  },
  {
    // Static browser scripts served verbatim by Vite (e.g. public/theme-init.js) —
    // classic scripts, browser globals, not part of the TS module graph.
    files: ["public/**/*.js"],
    languageOptions: {
      ecmaVersion: 2022,
      sourceType: "script",
      globals: { ...globals.browser },
    },
  },
  prettier,
);
