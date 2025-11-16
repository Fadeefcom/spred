import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { CheckCircle2, Music, User, Clock, PlayCircle } from "lucide-react";
import {Platform, PlatformLabels} from "@/types/Platforms.ts";

interface SuccessStepProps {
    accountId: string;
    token: string;
    onFinish: () => void;
    platform: Platform;
}

export const SuccessStep = ({ accountId, token, onFinish, platform }: SuccessStepProps) => {
    const label = PlatformLabels[platform] ?? "Platform";

    return (
        <div className="space-y-6 animate-fade-in">
            {/* Success Header */}
            <div className="text-center">
                <div className="inline-flex items-center justify-center w-16 h-16 bg-success-light rounded-full mb-4">
                    <CheckCircle2 className="w-8 h-8 text-success" />
                </div>
                <Badge className="bg-success text-success-foreground mb-4">
                    Verified
                </Badge>
                <h3 className="text-lg font-semibold mb-2">Account Successfully Verified</h3>
                <p className="text-sm text-muted-foreground">
                    Your Spotify account ownership has been confirmed
                </p>
            </div>

            {/* Summary Card */}
            <Card className="p-4 space-y-4">
                <h4 className="font-medium text-sm">Verification Summary</h4>

                <div className="space-y-3">
                    <div className="flex items-center gap-3">
                        <div className="w-8 h-8 bg-spotify rounded-lg flex items-center justify-center">
                            <Music className="w-4 h-4 text-spotify-foreground" />
                        </div>
                        <div className="flex-1">
                            <p className="text-sm font-medium">Platform</p>
                            <p className="text-xs text-muted-foreground">{label}</p>
                        </div>
                    </div>

                    <div className="flex items-center gap-3">
                        <div className="w-8 h-8 bg-muted rounded-lg flex items-center justify-center">
                            <User className="w-4 h-4 text-muted-foreground" />
                        </div>
                        <div className="flex-1">
                            <p className="text-sm font-medium">Account ID</p>
                            <p className="text-xs text-muted-foreground font-mono">{accountId}</p>
                        </div>
                    </div>

                    <div className="flex items-center gap-3">
                        <div className="w-8 h-8 bg-success-light rounded-lg flex items-center justify-center">
                            <PlayCircle className="w-4 h-4 text-success" />
                        </div>
                        <div className="flex-1">
                            <p className="text-sm font-medium">Playlist found</p>
                            <p className="text-xs text-muted-foreground font-mono">{token}</p>
                            <p className="text-xs text-muted-foreground">(public)</p>
                        </div>
                    </div>

                    <div className="flex items-center gap-3">
                        <div className="w-8 h-8 bg-muted rounded-lg flex items-center justify-center">
                            <Clock className="w-4 h-4 text-muted-foreground" />
                        </div>
                        <div className="flex-1">
                            <p className="text-sm font-medium">Verified at</p>
                            <p className="text-xs text-muted-foreground">
                                {new Date().toLocaleString()}
                            </p>
                        </div>
                    </div>
                </div>
            </Card>

            {/* Actions */}
            <div className="space-y-3">
                <Button onClick={onFinish} className="w-full spotify-button">
                    Finish
                </Button>

                <Button variant="outline" disabled className="w-full">
                    Try another platform
                    <span className="ml-2 text-xs text-muted-foreground">(Coming soon)</span>
                </Button>
            </div>
        </div>
    );
};