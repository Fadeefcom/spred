import { useState } from "react";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import { platforms } from "@/types/Platforms";
import type { Platform } from "@/types/Platforms";
import { Button } from "@/components/ui/button";

interface Props {
    onNext: (platform: Platform) => void;
    onStartOAuth?: (platform: Platform) => void;
}

export const PlatformSelectStep = ({ onNext, onStartOAuth }: Props) => {
    const [selected, setSelected] = useState<Platform | null>(null);

    const handleClick = (pId: Platform, comingSoon?: boolean) => {
        if (comingSoon) return;
        setSelected(pId);
    };

    const handleContinue = () => {
        if (!selected) return;
        if (selected === "soundcloud") {
            onStartOAuth?.(selected);
            return;
        }
        if(selected === "youtube-music") {
            onStartOAuth?.(selected);
            return;
        }
        onNext(selected);
    };

    return (
        <div className="grid gap-3">
            {platforms.map((p) => {
                const Icon = p.icon;
                const isSelected = selected === p.id;
                return (
                    <button
                        key={p.id}
                        onClick={() => handleClick(p.id as Platform, p.comingSoon)}
                        disabled={p.comingSoon}
                        className={cn(
                            "flex items-center gap-4 p-4 rounded-2xl border-2 transition-all",
                            "hover:bg-accent/50",
                            isSelected && "border-primary bg-accent",
                            p.comingSoon && "opacity-50 cursor-not-allowed"
                        )}
                    >
                        <div className={cn("w-12 h-12 rounded-full flex items-center justify-center text-white", p.bgClass)}>
                            <Icon className="h-6 w-6" />
                        </div>
                        <div className="flex-1 text-left">
                            <div className="flex items-center gap-2">
                                <span className="font-semibold">{p.name}</span>
                                {p.comingSoon && <Badge variant="secondary" className="text-xs">Coming Soon</Badge>}
                            </div>
                            <p className="text-sm text-muted-foreground">
                                {p.comingSoon ? `${p.name} integration will be available soon` : `Connect your ${p.name} account`}
                            </p>
                        </div>
                    </button>
                );
            })}
            <Button className="w-full mt-2" onClick={handleContinue} disabled={!selected}>Continue</Button>
        </div>
    );
};
