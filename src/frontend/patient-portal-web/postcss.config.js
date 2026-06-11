/**
 * Tailwind v4 runs through the dedicated Vite plugin (`@tailwindcss/vite`, see
 * vite.config.ts), which also owns vendor prefixing via Lightning CSS — so the
 * old `tailwindcss` / `autoprefixer` PostCSS plugins are gone. Vite still applies
 * this PostCSS config to the CSS after the Tailwind plugin (it runs `pre`), which
 * keeps the local fix-up below working against the generated preflight.
 *
 * Tailwind's preflight emits only `-webkit-text-size-adjust`; standards-following browsers
 * ignore the prefixed form, so pair it with `text-size-adjust` inside the same rule.
 */
const textSizeAdjustStandard = {
  postcssPlugin: "text-size-adjust-standard",
  Declaration: {
    "-webkit-text-size-adjust": (decl) => {
      const hasStandard = decl.parent.some(
        (sibling) => sibling.type === "decl" && sibling.prop === "text-size-adjust",
      );
      if (!hasStandard) decl.cloneAfter({ prop: "text-size-adjust" });
    },
  },
};

export default {
  plugins: [textSizeAdjustStandard],
};
