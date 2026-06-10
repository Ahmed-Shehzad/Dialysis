import tailwindcss from "tailwindcss";
import autoprefixer from "autoprefixer";

/**
 * Tailwind's preflight emits only `-webkit-text-size-adjust`; standards-following browsers
 * ignore the prefixed form, so pair it with `text-size-adjust` inside the same rule.
 * Runs after autoprefixer so the cloned declaration isn't re-expanded.
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
  plugins: [tailwindcss, autoprefixer, textSizeAdjustStandard],
};
