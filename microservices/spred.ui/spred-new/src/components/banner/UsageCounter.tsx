import { cn } from "@/lib/utils";
import {AlertCircle, Clock, Crown, Link} from "lucide-react";
import {Button} from "@/components/ui/button.tsx";

interface UsageCounterProps {
    current: number;
    limit: number | string;
    label: string;
    resetAt?: number;
    className?: string;
}

export const UsageCounter = ({
                                 current,
                                 limit,
                                 label,
                                 resetAt,
                                 className,
                             }: UsageCounterProps) => {
    const isUnlimited = limit === "unlimited";
    const numericLimit = typeof limit === "number" ? limit : 0;
    const percentage = isUnlimited ? 0 : (current / numericLimit) * 100;
    const isNearLimit = !isUnlimited && percentage >= 80;
    const isAtLimit = !isUnlimited && current >= numericLimit;

    const resetMessage =
        resetAt && resetAt > 0
            ? `Resets ${new Intl.DateTimeFormat(undefined, {
                dateStyle: "medium",
                timeStyle: "short",
            }).format(new Date(resetAt * 1000))}`
            : "";

    return (
        <div className={cn("space-y-2.5", className)}>
            <div className="flex items-center justify-between text-sm">
                <span className="text-gray-400 font-medium">{label}</span>
                <span
                    className={cn(
                        "font-semibold tabular-nums",
                        isAtLimit
                            ? "text-red-400"
                            : isNearLimit
                                ? "text-yellow-400"
                                : "text-gray-300"
                    )}
                >
          {isUnlimited ? "∞" : `${current} / ${numericLimit}`}
        </span>
            </div>

            {!isUnlimited && (
                <div className="h-1.5 w-full overflow-hidden rounded-full bg-gray-800/50">
                    <div
                        className={cn(
                            "h-full transition-all duration-500 ease-out",
                            isAtLimit
                                ? "bg-red-500"
                                : isNearLimit
                                    ? "bg-yellow-500"
                                    : "bg-spred-yellow"
                        )}
                        style={{ width: `${Math.min(percentage, 100)}%` }}
                    />
                </div>
            )}

            {isUnlimited ? (
                <div className="flex items-center gap-2 rounded-md bg-spred-yellow/10 px-3 py-1.5 text-xs text-spred-yellow animate-fade-in">
                    <Clock className="h-3 w-3" />
                    <span>Premium — unlimited uploads</span>
                </div>
            ) : isAtLimit ? (
                <div className="space-y-2 animate-fade-in">
                    <div className="flex items-center gap-2 rounded-md bg-red-500/10 px-3 py-1.5 text-xs text-red-400 animate-fade-in">
                        <AlertCircle className="h-3 w-3" />
                        <span>
                            Limit reached. Uploads reset {resetMessage.toLowerCase()}.
                        </span>
                    </div>
                </div>
            ) : resetMessage ? (
                <div className="flex items-center gap-2 text-xs text-gray-500 animate-fade-in">
                    <Clock className="h-3 w-3" />
                    <span>{resetMessage}</span>
                </div>
            ) : null}
        </div>
    );
};
