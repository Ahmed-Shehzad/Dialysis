import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
import path from "node:path";

// HIE context app. Served under the gateway's `/hie/*`.
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const gateway = env.VITE_GATEWAY_URL ?? "http://localhost:9090";

  return {
    base: "/hie/",
    plugins: [react()],
    resolve: { alias: { "@": path.resolve(__dirname, "src") } },
    server: {
      port: 5335,
      host: "0.0.0.0",
      proxy: {
        "/hie/api": { target: gateway, changeOrigin: true, secure: false },
        "/hie/hubs": { target: gateway, changeOrigin: true, ws: true, secure: false },
        "/hie/identity": { target: gateway, changeOrigin: true, secure: false },
      },
    },
  };
});
