import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { AlertCircle, CheckCircle2 } from "lucide-react";
import { cn } from "@/lib/utils";
import {Platform, PlatformLabels} from "@/types/Platforms.ts";

interface AccountIdStepProps {
    platform: Platform;
    onNext: (platformUserId: string) => void;
}

export const AccountIdStep = ({ platform, onNext }: AccountIdStepProps) => {
    const [accountId, setAccountId] = useState("");
    const [isValid, setIsValid] = useState<boolean | null>(null);

    const validateAccountId = (value: string) => {
        // Basic validation - not empty, reasonable length, no spaces
        const trimmed = value.trim();
        if (!trimmed) {
            setIsValid(null);
            return;
        }

        const valid = trimmed.length >= 3 && trimmed.length <= 50 && !/\s/.test(trimmed);
        setIsValid(valid);
    };

    const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const value = e.target.value;
        setAccountId(value);
        validateAccountId(value);
    };

    const handleNext = () => {
        if (isValid && accountId.trim()) {
            onNext(accountId.trim());
        }
    };

    const label = PlatformLabels[platform] ?? "Platform";

    return (
        <div className="space-y-6 animate-fade-in">
            <div className="space-y-3">
                <Label htmlFor="account-id" className="text-sm font-medium">
                    {label} User ID
                </Label>

                <div className="relative">
                    <Input
                        id="account-id"
                        type="text"
                        placeholder="e.g., john_doe_123"
                        value={accountId}
                        onChange={handleInputChange}
                        className={cn(
                            "pr-10 focus-ring",
                            isValid === false && "border-destructive focus:ring-destructive",
                            isValid === true && "border-success focus:ring-success"
                        )}
                    />

                    {isValid === true && (
                        <CheckCircle2 className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-success" />
                    )}

                    {isValid === false && (
                        <AlertCircle className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-destructive" />
                    )}
                </div>

                <p className="text-xs text-muted-foreground">
                    Paste your {label} user ID (not display name). You can find this in your {label} profile URL.
                </p>

                {isValid === false && (
                    <div className="flex items-center gap-2 text-xs text-destructive animate-slide-up">
                        <AlertCircle className="w-3 h-3 flex-shrink-0" />
                        <span>Please enter a valid {label} user ID without spaces.</span>
                    </div>
                )}
            </div>

            <div className="bg-muted rounded-lg p-4">
                <h4 className="text-sm font-medium mb-2">How to find your {label} User ID:</h4>
                <ol className="text-xs text-muted-foreground space-y-1 list-decimal list-inside">
                    <li>Open {label} and go to your profile</li>
                    <li>Click the three dots (⋯) menu</li>
                    <li>Select "Share" → "Copy link to profile"</li>
                    <li>Your User ID is the part after "/user/" in the URL</li>
                </ol>
            </div>

            <Button
                onClick={handleNext}
                disabled={!isValid || !accountId.trim()}
                className="w-full spotify-button"
            >
                Continue
            </Button>
        </div>
    );
};