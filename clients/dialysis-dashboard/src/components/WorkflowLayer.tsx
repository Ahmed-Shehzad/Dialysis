import { useCallback } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getTreatmentSession, signTreatmentSession } from "../api";
import type { TreatmentSessionContext } from "../types";
import { PreAssessmentPanel } from "./workflow/PreAssessmentPanel";
import { RunningPanel } from "./workflow/RunningPanel";
import { CompletedPanel } from "./workflow/CompletedPanel";
import { SignedPanel } from "./workflow/SignedPanel";

export type WorkflowState =
    | "pre-assessment"
    | "running"
    | "completed"
    | "signed";

interface WorkflowLayerProps {
    sessionId: string | null;
    patientMrn?: string | null;
    onSessionChange?: (sessionId: string | null) => void;
}

function deriveState(session: TreatmentSessionContext | null): WorkflowState {
    if (!session) return "pre-assessment";
    if (session.signedAt != null) return "signed";
    if (session.status?.toLowerCase() === "completed") return "completed";
    if (session.status?.toLowerCase() === "active") {
        return session.preAssessment != null ? "running" : "pre-assessment";
    }
    return "pre-assessment";
}

export function WorkflowLayer({
    sessionId,
    patientMrn,
    onSessionChange,
}: Readonly<WorkflowLayerProps>) {
    const queryClient = useQueryClient();

    const { data: session } = useQuery({
        queryKey: ["treatment-session", sessionId],
        queryFn: () => getTreatmentSession(sessionId!),
        enabled: Boolean(sessionId),
        refetchInterval: 10_000,
        staleTime: 5_000,
    });

    const signMutation = useMutation({
        mutationFn: () => signTreatmentSession(sessionId!),
        onSuccess: () => {
            void queryClient.invalidateQueries({ queryKey: ["treatment-session", sessionId] });
            void queryClient.invalidateQueries({ queryKey: ["audit"] });
        },
    });

    const handleSign = useCallback(() => {
        if (!sessionId) return;
        signMutation.mutate();
    }, [sessionId, signMutation]);

    const state = deriveState(session ?? null);

    if (!sessionId) {
        return (
            <PreAssessmentPanel
                patientMrn={patientMrn}
                onSessionSelect={onSessionChange}
            />
        );
    }

    switch (state) {
        case "pre-assessment":
            return (
                <PreAssessmentPanel
                    sessionId={sessionId}
                    patientMrn={patientMrn ?? session?.patientMrn}
                    preAssessment={session?.preAssessment}
                    onSessionSelect={onSessionChange}
                />
            );
        case "running":
            return (
                <RunningPanel session={session!} />
            );
        case "completed":
            return (
                <CompletedPanel
                    session={session!}
                    onSign={handleSign}
                    isSigning={signMutation.isPending}
                    signError={signMutation.error?.message ?? null}
                    onSessionChange={onSessionChange}
                />
            );
        case "signed":
            return (
                <SignedPanel
                    session={session!}
                    onSessionChange={onSessionChange}
                />
            );
        default:
            return (
                <PreAssessmentPanel
                    sessionId={sessionId}
                    patientMrn={patientMrn ?? session?.patientMrn}
                    preAssessment={session?.preAssessment}
                    onSessionSelect={onSessionChange}
                />
            );
    }
}
