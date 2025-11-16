interface Step {
    id: string;
    label: string;
    duration: number;
}

interface StatusStepperProps {
    steps: Step[];
    currentStep: number;
    completedSteps: string[];
}

export const StatusStepper = ({ steps, currentStep, completedSteps }: StatusStepperProps) => {
    return (
        <div className="space-y-4">
            {steps.map((step, index) => {
                const isCompleted = completedSteps.includes(step.id);
                const isCurrent = index === currentStep;
                const isPending = index > currentStep;

                return (
                    <div key={step.id} className="stepper-item">
                        <div
                            className={`stepper-dot ${
                                isCompleted
                                    ? 'completed'
                                    : isCurrent
                                        ? 'active'
                                        : 'pending'
                            }`}
                        />
                        <span className={`text-sm flex-1 ${
                            isCompleted
                                ? 'text-success'
                                : isCurrent
                                    ? 'text-foreground font-medium'
                                    : 'text-muted-foreground'
                        }`}>
                          {step.label}
                        </span>

                        {isCurrent && (
                            <div className="flex items-center gap-2">
                                <div className="flex gap-1">
                                    {[0, 1, 2].map((dot) => (
                                        <div
                                            key={dot}
                                            className="w-1 h-1 bg-primary rounded-full animate-bounce"
                                            style={{
                                                animationDelay: `${dot * 0.2}s`,
                                                animationDuration: '1s'
                                            }}
                                        />
                                    ))}
                                </div>
                            </div>
                        )}
                    </div>
                );
            })}
        </div>
    );
};