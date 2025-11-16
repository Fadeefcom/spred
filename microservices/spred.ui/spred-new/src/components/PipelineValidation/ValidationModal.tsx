import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { ValidationState } from "./PipelineValidation";
import { PlatformSelectStep } from "./steps/PlatformSelectStep";
import { AccountIdStep } from "./steps/AccountIdStep";
import { ProcessingStep } from "./steps/ProcessingStep";
import { SuccessStep } from "./steps/SuccessStep";
import { ErrorStep } from "./steps/ErrorStep";
import {PlaylistStep} from "@/components/PipelineValidation/steps/PlaylistStep.tsx";
import {AccountStatus, PlatformLabels} from "@/types/Platforms.ts";
import { SERVICES } from "@/constants/services";
import {apiFetch} from "@/hooks/apiFetch.tsx";
import {useEffect} from "react";

interface ValidationModalProps {
    state: ValidationState;
    onStateChange: (updates: Partial<ValidationState>) => void;
    onClose: () => void;
    isOpen: boolean;
    onCompleted?: () => void;
}

export const ValidationModal = ({ state, onStateChange, onClose, isOpen, onCompleted }: ValidationModalProps) => {
    const label = PlatformLabels[state.platform] ?? "Platform";
    const title =
        state.step === "platform-select" ? "Select a platform" :
            state.step === "account-id" ? `Enter your ${label} account identifier` :
                state.step === "create-proof" ? `Create a verification proof on ${label}` :
                    state.step === "processing" ? "Verifying ownership" :
                        state.step === "success" ? "Verification complete" :
                            "Verification failed";

    const description =
        state.step === "platform-select" ? "Choose the platform you want to verify" :
            state.step === "account-id" ? `Provide your ${label} user identifier to proceed.` :
                state.step === "create-proof" ? "Follow the instructions below to complete verification." :
                    state.step === "processing" ? "Verification is running in the background. You can safely close this window." :
                        state.step === "success" ? `${label} account has been successfully verified.` :
                            "We encountered an issue during verification.";

    useEffect(() => {
        const ensureToken = async () => {
            if (state.step !== "create-proof") return;
            if (!state.platform || !state.accountId) return;
            if (state.token) return;

            const resToken = await apiFetch(
                `${SERVICES.USER}/user/accounts/${state.accountId}/token`,
                { method: "POST" }
            );

            if (!resToken.ok) {
                const err = await resToken.json().catch(() => ({}));
                onStateChange({ step: "error", error: err?.status ??  "Failed to issue token" });
                return;
            }

            const tok = (await resToken.json()) as { token?: string; tokeen?: string };
            onStateChange({ token: tok.token ?? tok.tokeen ?? "" });
        };

        ensureToken();
    }, [state.step, state.platform, state.accountId, state.token, onStateChange]);

    const handleVerifyClick = async () => {
        if (!state.accountId) return;

        const res = await apiFetch(`${SERVICES.USER}/user/accounts/${state.accountId}/verify`, {
            method: "POST"
        });

        if (res.ok) {
            onStateChange({ step: "processing" });
        } else {
            const err = await res.json().catch(() => ({}));
            onStateChange({ step: "error", error: err?.status ?? "Verification failed" });
        }
    };

    const createAccount = async (
        platform: string,
        accountId: string
    ): Promise<{ ok: boolean; accountId?: string; error?: string }> => {
        const res = await apiFetch(`${SERVICES.USER}/user/accounts`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ platform, accountId }),
        });

        if (!res.ok) {
            const err = await res.json().catch(() => ({}));
            return { ok: false, error: err?.status ?? "Failed to create account" };
        }

        const created = (await res.json()) as { accountId: string };
        return { ok: true, accountId: created.accountId };
    };

    return (
        <Dialog open={isOpen} onOpenChange={onClose}>
            <DialogContent className="sm:max-w-lg max-h-[90vh] overflow-y-auto animate-scale-in">
                <DialogHeader className="space-y-1">
                    <DialogTitle className="text-xl font-semibold">{title}</DialogTitle>
                    <p className="text-sm text-muted-foreground">{description}</p>
                </DialogHeader>

                <div className="py-4">
                    {state.step === "platform-select" && (
                        <PlatformSelectStep
                            onNext={(platform) => onStateChange({ step: "account-id", platform })}
                            onStartOAuth={(platform) => {
                                if (platform === "soundcloud") {
                                    window.location.href = `${SERVICES.USER}/auth/soundcloud`;
                                }
                                if(platform === "youtube-music"){
                                    window.location.href = `${SERVICES.USER}/auth/youtube-music`;
                                }
                            }}
                        />
                    )}

                    {state.step === "account-id" && state.platform && (
                        <AccountIdStep
                            platform={state.platform}
                            onNext={async (accountId) => {
                                const result = await createAccount(state.platform!, accountId);

                                if (!result.ok) {
                                    onStateChange({ step: "error", error: result.error });
                                    return;
                                }

                                onStateChange({
                                    accountId: result.accountId!,
                                    step: "create-proof",
                                });
                            }}
                        />
                    )}

                    {state.step === "create-proof" && state.platform && (
                        <PlaylistStep
                            token={state.token}
                            platform={state.platform}
                            onNext={handleVerifyClick}
                        />
                    )}

                    {state.step === "processing" && state.accountId && (
                        <ProcessingStep
                            accountId={state.accountId}
                            onSuccess={() => onStateChange({ step: "success" })}
                            onClose={onClose}
                        />
                    )}

                    {state.step === "success" && (
                        <SuccessStep
                            platform={state.platform!}
                            accountId={state.accountId!}
                            token={state.token!}
                            onFinish={() => {
                                onClose();
                                onCompleted?.();
                            }}
                        />
                    )}

                    {state.step === "error" && (
                        <ErrorStep
                            error={state.error}
                            platform={state.platform}
                            token={state.token}
                            onRetry={() => onStateChange({ step: "create-proof", error: undefined })}
                            onClose={onClose}
                        />
                    )}
                </div>
            </DialogContent>
        </Dialog>
    );
};
