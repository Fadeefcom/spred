import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { TokenChip } from "../components/TokenChip";
import { AlertTriangle, RefreshCw, ExternalLink, CheckSquare, Square } from "lucide-react";
import { Platform, PlatformLabels, PlatformUrls } from "@/types/Platforms.ts";

interface ErrorStepProps {
    error?: string;
    token?: string;
    onRetry: () => void;
    onClose: () => void;
    platform?: Platform;
}

const troubleshootingSteps = [
    <>Make sure the playlist is set to Public (not Private)</>,
    <>Check that the playlist <strong>title or description</strong> matches the token exactly</>,
    <>Ensure there are no extra spaces or characters in the <strong>title or description</strong></>,
    <>Wait 1-2 minutes for servers to update</>
];
export const ErrorStep = ({ error, token, onRetry, onClose, platform }: ErrorStepProps) => {
    const label = (platform && PlatformLabels[platform]) ?? "Platform";
    const openUrl = (platform && PlatformUrls[platform]) ?? "https://music.youtube.com";
    const hasToken = token && token.trim().length > 0;

    return (
        <div className="space-y-6 animate-fade-in">
            <div className="text-center">
                <div className="inline-flex items-center justify-center w-16 h-16 bg-destructive/10 rounded-full mb-4">
                    <AlertTriangle className="w-8 h-8 text-destructive" />
                </div>
                <h3 className="text-lg font-semibold mb-2 text-destructive">Verification Failed</h3>
                <p className="text-sm text-muted-foreground">
                    {error ?? "We couldn't verify your account."}
                </p>
            </div>

            {hasToken && (
                <div className="text-center">
                    <p className="text-sm text-muted-foreground mb-3">
                        Expected playlist <strong>title or description</strong>:
                    </p>
                    <TokenChip token={token} />
                </div>
            )}

            <Card className="p-4">
                <h4 className="font-medium text-sm mb-3 flex items-center gap-2">
                    <CheckSquare className="w-4 h-4 text-muted-foreground" />
                    Troubleshooting Checklist
                </h4>
                <div className="space-y-2">
                    {troubleshootingSteps.map((step, index) => (
                        <div key={index} className="flex items-start gap-2 text-sm text-muted-foreground">
                            <Square className="w-3 h-3 mt-1 flex-shrink-0" />
                            <span>{step}</span>
                        </div>
                    ))}
                </div>
            </Card>

            <div className="space-y-3">
                <Button onClick={onRetry} className="w-full spotify-button">
                    <RefreshCw className="w-4 h-4 mr-2" />
                    Try again
                </Button>

                <div className="grid grid-cols-2 gap-3">
                    <Button
                        variant="outline"
                        size="sm"
                        onClick={() => window.open(openUrl, "_blank")}
                    >
                        <ExternalLink className="w-4 h-4 mr-2" />
                        Open {label}
                    </Button>
                    <Button variant="outline" size="sm" onClick={onClose}>
                        Close
                    </Button>
                </div>
            </div>
        </div>
    );
};
