import { useState, useEffect } from "react";
import { Music, Plus, Save } from "lucide-react";
import {Platform, PlatformLabels} from "@/types/Platforms.ts";

interface SpotifyAnimationProps {
    platform: Platform;
}

export const SpotifyAnimation= ({ platform }: SpotifyAnimationProps) => {
    const [currentStep, setCurrentStep] = useState(0);
    const label = PlatformLabels[platform] ?? "Platform";

    useEffect(() => {
        const interval = setInterval(() => {
            setCurrentStep((prev) => (prev + 1) % 3);
        }, 2000);

        return () => clearInterval(interval);
    }, []);

    return (
        <div className="bg-spotify-light rounded-lg p-6 relative overflow-hidden">
            {/* Background Pattern */}
            <div className="absolute inset-0 opacity-10">
                <div className="absolute top-2 right-2 w-16 h-16 bg-spotify rounded-full blur-xl"></div>
                <div className="absolute bottom-2 left-2 w-12 h-12 bg-spotify rounded-full blur-lg"></div>
            </div>

            <div className="relative z-10">
                <div className="flex items-center justify-center gap-4 min-h-[100px]">
                    {/* Step 1: Open Spotify */}
                    <div className={`flex flex-col items-center gap-2 transition-all duration-500 ${currentStep === 0 ? 'scale-110 opacity-100' : 'scale-95 opacity-60'}`}>
                        <div className="w-12 h-12 bg-spotify rounded-lg flex items-center justify-center shadow-lg">
                            <Music className="w-6 h-6 text-spotify-foreground" />
                        </div>
                        <span className="text-xs text-center max-w-16">Open {label}</span>
                    </div>

                    {/* Arrow */}
                    <div className="text-spotify animate-pulse">
                        →
                    </div>

                    {/* Step 2: Create Playlist */}
                    <div className={`flex flex-col items-center gap-2 transition-all duration-500 ${currentStep === 1 ? 'scale-110 opacity-100' : 'scale-95 opacity-60'}`}>
                        <div className="w-12 h-12 bg-gradient-to-br from-spotify to-spotify-hover rounded-lg flex items-center justify-center shadow-lg">
                            <Plus className="w-6 h-6 text-spotify-foreground" />
                        </div>
                        <span className="text-xs text-center max-w-16">Create Playlist</span>
                    </div>

                    {/* Arrow */}
                    <div className="text-spotify animate-pulse">
                        →
                    </div>

                    {/* Step 3: Set Title */}
                    <div className={`flex flex-col items-center gap-2 transition-all duration-500 ${currentStep === 2 ? 'scale-110 opacity-100' : 'scale-95 opacity-60'}`}>
                        <div className="w-12 h-12 bg-success rounded-lg flex items-center justify-center shadow-lg">
                            <Save className="w-6 h-6 text-success-foreground" />
                        </div>
                        <span className="text-xs text-center max-w-16">Set Token Title</span>
                    </div>
                </div>

                {/* Step Indicator */}
                <div className="flex justify-center gap-2 mt-4">
                    {[0, 1, 2].map((step) => (
                        <div
                            key={step}
                            className={`w-2 h-2 rounded-full transition-all duration-300 ${
                                step === currentStep ? 'bg-spotify scale-125' : 'bg-spotify/30'
                            }`}
                        />
                    ))}
                </div>
            </div>
        </div>
    );
};