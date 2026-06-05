import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { type ReactNode, useState } from "react";
import { BrowserRouter } from "react-router-dom";
import { AuthProvider } from "@/features/auth/components/AuthProvider";
import { ThemeProvider } from "@/features/theme/ThemeProvider";
import { PatientContextProvider } from "@/shell/PatientContextProvider";

const buildQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        gcTime: 5 * 60_000,
        refetchOnWindowFocus: false,
        retry: (failureCount, error) => {
          const status = (error as { response?: { status?: number } })?.response?.status;
          if (status === 401 || status === 403) return false;
          return failureCount < 2;
        },
      },
      mutations: { retry: 0 },
    },
  });

export const AppProviders = ({ children }: { children: ReactNode }) => {
  const [client] = useState(buildQueryClient);

  return (
    <ThemeProvider>
      <QueryClientProvider client={client}>
        <BrowserRouter
          basename="/ehr"
          future={{ v7_startTransition: true, v7_relativeSplatPath: true }}
        >
          <AuthProvider>
            <PatientContextProvider>{children}</PatientContextProvider>
          </AuthProvider>
        </BrowserRouter>
        <ReactQueryDevtools initialIsOpen={false} buttonPosition="bottom-right" />
      </QueryClientProvider>
    </ThemeProvider>
  );
};
