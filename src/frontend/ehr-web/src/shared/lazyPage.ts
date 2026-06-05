import { lazy, type ComponentType, type LazyExoticComponent } from "react";

type AnyComponent = ComponentType<Record<string, unknown>>;

/**
 * Wraps `React.lazy` so modules that use named exports (which we prefer for refactor safety)
 * can still be code-split. The caller passes a dynamic `import()` for the module and the
 * name of the export to render; the result is a `LazyExoticComponent` you can drop into
 * a `<Route element={…} />` exactly like the eagerly-imported component.
 *
 * The dynamic import is what tells Vite/Rollup to emit a separate chunk per page — splitting
 * module pages this way keeps the first paint small and lets unused modules stay unloaded.
 *
 * @example
 * const HisTodayPage = lazyPage(() => import("@/modules/his/today/HisTodayPage"), "HisTodayPage");
 */
export const lazyPage = <T extends Record<string, AnyComponent>>(
  importer: () => Promise<T>,
  exportName: keyof T & string,
): LazyExoticComponent<AnyComponent> =>
  lazy(async () => {
    const mod = await importer();
    // `keyof T & string` is checked at call sites, but `noUncheckedIndexedAccess` still widens
    // the lookup to `| undefined`. A missing named export is a build-time programmer error
    // (the dynamic import would resolve to a different module shape), so we narrow here.
    return { default: mod[exportName] as AnyComponent };
  });
