import { useEffect, useState } from "react";
import { StatusStepper } from "../components/StatusStepper";

interface ProcessingStepProps {
    onClose: () => void;
    onSuccess: () => void;
    accountId: string;
}

const processingSteps = [
    { id: "token", label: "Requesting token", duration: 1000 },
    { id: "verifying", label: "Verifying ownership", duration: 1000 },
    { id: "done", label: "Done", duration: 500 }
];

export const ProcessingStep = ({ onSuccess, onClose }: ProcessingStepProps) => {
    const [currentStep, setCurrentStep] = useState(0);
    const [completedSteps, setCompletedSteps] = useState<string[]>([]);

    useEffect(() => {
        setCompletedSteps(processingSteps.map(s => s.id));
        setCurrentStep(1);
    }, []);

    return (
        <div className="space-y-6 animate-fade-in">
            <div className="text-center">
                <div className="inline-flex items-center gap-2 px-4 py-2 bg-muted rounded-full mb-4">
                    <div className="w-2 h-2 bg-primary rounded-full animate-pulse"></div>
                    <span className="text-sm font-medium">Verification started...</span>
                </div>
            </div>

            <StatusStepper
                steps={processingSteps}
                currentStep={currentStep}
                completedSteps={completedSteps}
            />

            <div className="bg-muted rounded-lg p-4 text-center">
                <p className="text-sm text-muted-foreground mb-3">
                    Verification is running in the background. You can safely close this window.
                </p>
                <button
                    onClick={onClose}
                    className="px-4 py-2 rounded-md border text-sm hover:bg-accent transition-colors"
                >
                    Close
                </button>
            </div>
        </div>
    );
};
