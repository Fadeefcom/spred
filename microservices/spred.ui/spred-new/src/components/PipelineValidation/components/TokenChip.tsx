import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Copy, Check } from "lucide-react";
import { useToast } from "@/hooks/use-toast";

interface TokenChipProps {
    token: string;
}

export const TokenChip = ({ token }: TokenChipProps) => {
    const [copied, setCopied] = useState(false);
    const { toast } = useToast();

    const handleCopy = async () => {
        try {
            await navigator.clipboard.writeText(token);
            setCopied(true);
            toast({
                title: "Token copied",
                description: "The playlist token has been copied to your clipboard",
            });
            setTimeout(() => setCopied(false), 2000);
        } catch (error) {
            toast({
                title: "Copy failed",
                description: "Please manually copy the token",
                variant: "destructive",
            });
        }
    };

    return (
        <div className="inline-flex items-center gap-2 max-w-full">
            <div
                className="token-chip flex-1 min-w-0 max-w-[200px] truncate"
                title={token}
            >
                {token}
            </div>
            <Button
                size="sm"
                variant="outline"
                onClick={handleCopy}
                className="flex-shrink-0 h-8 px-2"
            >
                {copied ? (
                    <Check className="w-3 h-3 text-success" />
                ) : (
                    <Copy className="w-3 h-3" />
                )}
            </Button>
        </div>
    );
};