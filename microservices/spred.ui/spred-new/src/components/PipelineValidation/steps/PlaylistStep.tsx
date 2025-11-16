import { useState } from "react";
import { Button } from "@/components/ui/button";
import { TokenChip } from "../components/TokenChip";
import { SpotifyAnimation } from "../components/SpotifyAnimation";
import { CheckSquare, Square, ExternalLink } from "lucide-react";
import { Platform, PlatformLabels, PlatformUrls } from "@/types/Platforms.ts";

interface PlaylistStepProps {
    token: string;
    onNext: () => void;
    platform: Platform;
}

export const PlaylistStep = ({ token, onNext, platform }: PlaylistStepProps) => {
    const [completedSteps, setCompletedSteps] = useState<number[]>([]);
    const label = PlatformLabels[platform] ?? "Platform";

    const steps = [
        <>Open {label} and create a new public playlist</>,
        <>Set the playlist <strong>title or description</strong> to the token above</>,
        <>Don't add tracks yet - keep it empty for now</>
    ];

    const toggleStep = (stepIndex: number) => {
        setCompletedSteps(prev => (prev.includes(stepIndex) ? prev.filter(i => i !== stepIndex) : [...prev, stepIndex]));
    };

    const isAllCompleted = completedSteps.length === steps.length;
    const openUrl = PlatformUrls[platform];

    return (
        <div className="space-y-8 animate-fade-in">
            <div className="text-center">
                <p className="text-sm text-muted-foreground mb-3">
                    Use this exact token as your playlist <strong>title or description</strong>:
                </p>
                <TokenChip token={token} />
            </div>

            <SpotifyAnimation platform={platform} />

            <div className="space-y-4">
                <h4 className="font-medium">Follow these steps:</h4>
                <div className="space-y-3">
                    {steps.map((step, index) => (
                        <div
                            key={index}
                            className="flex items-start gap-3 p-3 rounded-lg hover:bg-accent/50 transition-colors duration-200 cursor-pointer"
                            onClick={() => toggleStep(index)}
                        >
                            {completedSteps.includes(index) ? (
                                <CheckSquare className="w-5 h-5 text-success mt-0.5 flex-shrink-0" />
                            ) : (
                                <Square className="w-5 h-5 text-muted-foreground mt-0.5 flex-shrink-0" />
                            )}
                            <span
                                className={`text-sm ${completedSteps.includes(index) ? "line-through text-muted-foreground" : ""}`}
                            >
                                {step}
                            </span>
                        </div>
                    ))}
                </div>
            </div>

            <div className="flex flex-col sm:flex-row gap-3">
                <Button variant="outline" size="sm" className="flex-1" onClick={() => window.open(openUrl, "_blank")}>
                    <ExternalLink className="w-4 h-4 mr-2" />
                    Open {label}
                </Button>
            </div>

            <Button onClick={onNext} disabled={!isAllCompleted} className="w-full">
                I've created the playlist
            </Button>
        </div>
    );
};
