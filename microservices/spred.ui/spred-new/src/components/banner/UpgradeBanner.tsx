import { Sparkles, ArrowRight } from "lucide-react";
import {Link, useLocation} from "react-router-dom";
import { Button } from "../ui/button.tsx";

export const UpgradeBanner = () => {
    const location = useLocation()

    const segments = location.pathname.split("/").filter(Boolean)
    const currentType = segments[0] ?? "user"

    const upgradePath = `/${currentType}/upgrade`


    return (
        <div className="relative overflow-hidden rounded-lg border border-primary/30 bg-gradient-to-r from-primary/10 via-accent/10 to-primary/10 p-6 animate-fade-in">
            <div className="absolute inset-0 bg-[url('data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAwIiBoZWlnaHQ9IjIwMCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48ZGVmcz48cGF0dGVybiBpZD0iZ3JpZCIgd2lkdGg9IjQwIiBoZWlnaHQ9IjQwIiBwYXR0ZXJuVW5pdHM9InVzZXJTcGFjZU9uVXNlIj48cGF0aCBkPSJNIDQwIDAgTCAwIDAgMCA0MCIgZmlsbD0ibm9uZSIgc3Ryb2tlPSJ3aGl0ZSIgc3Ryb2tlLW9wYWNpdHk9IjAuMDUiIHN0cm9rZS13aWR0aD0iMSIvPjwvcGF0dGVybj48L2RlZnM+PHJlY3Qgd2lkdGg9IjEwMCUiIGhlaWdodD0iMTAwJSIgZmlsbD0idXJsKCNncmlkKSIvPjwvc3ZnPg==')] opacity-50" />

            <div className="relative flex flex-col md:flex-row items-center justify-between gap-4">
                <div className="flex items-center gap-3">
                    <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary/20">
                        <Sparkles className="h-5 w-5 text-primary" />
                    </div>
                    <div>
                        <h3 className="font-semibold text-foreground">Unlock unlimited potential</h3>
                        <p className="text-sm text-muted-foreground">
                            Get unlimited uploads and access all opportunities
                        </p>
                    </div>
                </div>

                <Link to={upgradePath}>
                    <Button variant="default" className="gap-2">
                        Upgrade to Pro
                        <ArrowRight className="h-4 w-4" />
                    </Button>
                </Link>
            </div>
        </div>
    );
};