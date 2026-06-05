import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
import path from "node:path";

// HIS context app. Served under the gateway's `/his/*`, so the Vite base and the proxy paths are
// all scoped to `/his` — `/his/api` + `/his/identity` + `/his/hubs` go to the HIS BFF via the gateway.
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const gateway = env.VITE_GATEWAY_URL ?? "http://localhost:9090";

  return {
    base: "/his/",
    plugins: [react()],
    resolve: {
      alias: {
        "@": path.resolve(__dirname, "src"),
      },
    },
    server: {
      port: 5331,
      host: "0.0.0.0",
      proxy: {
        "/his/api": { target: gateway, changeOrigin: true, secure: false },
        "/his/hubs": { target: gateway, changeOrigin: true, ws: true, secure: false },
        "/his/identity": { target: gateway, changeOrigin: true, secure: false },
      },
    },
  };
});
