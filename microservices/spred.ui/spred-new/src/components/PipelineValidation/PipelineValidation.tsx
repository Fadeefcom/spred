// PipelineValidation.tsx
import { useEffect, useMemo, useState } from "react";
import { ValidationModal } from "./ValidationModal";
import { AccountStatus, Platform } from "@/types/Platforms";

export type ValidationStep = "platform-select" | "account-id" | "create-proof" | "processing" | "success" | "error";

export interface ValidationState {
    step: ValidationStep;
    platform?: Platform;
    accountId?: string;
    token?: string;
    error?: string;
    status?: AccountStatus;
}

interface PipelineValidationProps {
    open: boolean;
    onClose: () => void;
    onCompleted?: () => void;
    status: AccountStatus;
    initialAccount?: {
        platform?: Platform;
        accountId?: string;
        platformUserId?: string;
        status?: AccountStatus;
    };
}

const PipelineValidation = ({ open, onClose, onCompleted, status, initialAccount }: PipelineValidationProps) => {
    console.log(status);

    const mapStatusToStep = (s: AccountStatus): ValidationState["step"] => {
        switch (s) {
            case "Error":
                return "error";
            case "Verified":
                return "success";
            case "ProofSubmitted":
                return "processing";
            case "TokenIssued":
                return "create-proof";
            case "Pending":
                return "create-proof";
            case "PlatformSelect":
                return "platform-select";
            default:
                return "platform-select";
        }
    };

    const initial = useMemo<ValidationState>(
        () => ({
            step: mapStatusToStep(status),
            platform: initialAccount?.platform,
            accountId: initialAccount?.accountId,
            platformUserId: initialAccount?.platformUserId,
            status: initialAccount?.status ?? status,
        }),
        [status, initialAccount]
    );

    const [state, setState] = useState<ValidationState>(initial);

    useEffect(() => {
        if (!open) return;
        setState(initial);
    }, [open, initial]);

    if (!open) return null;

    return (
        <ValidationModal
            state={state}
            onStateChange={(u) => setState((p) => ({ ...p, ...u }))}
            onClose={onClose}
            isOpen={open}
            onCompleted={onCompleted}
        />
    );
};

export default PipelineValidation;
