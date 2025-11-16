import { useState, useRef } from "react"
import { LogOut, User, Crown, CreditCard } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Popover, PopoverTrigger, PopoverContent } from "@/components/ui/popover"
import { useAuth } from "@/components/authorization/AuthProvider"
import {Link} from "react-router-dom";

const UserMenu = () => {
    const { user, logout } = useAuth()
    const [open, setOpen] = useState(false)
    const timeoutRef = useRef<NodeJS.Timeout | null>(null)

    const isPro = user?.subscription?.isActive

    const handleMouseEnter = () => {
        if (timeoutRef.current) clearTimeout(timeoutRef.current)
        setOpen(true)
    }

    const handleMouseLeave = () => {
        // добавляем небольшую задержку, чтобы избежать "дёргания"
        timeoutRef.current = setTimeout(() => setOpen(false), 150)
    }

    return (
        <div className="hidden md:flex items-center gap-4">
            <Popover open={open}>
                <PopoverTrigger asChild>
                    <div
                        className="flex items-center gap-2 cursor-pointer select-none group"
                        onMouseEnter={handleMouseEnter}
                        onMouseLeave={handleMouseLeave}
                    >
                        <Badge
                            variant={isPro ? "pro" : "free"}
                            className="animate-fade-in group-hover:opacity-90 transition-opacity"
                        >
                            {isPro ? "Pro Plan" : "Free Plan"}
                        </Badge>

                        <User className="w-4 h-4 group-hover:text-foreground text-muted-foreground transition-colors" />
                        <span className="font-medium group-hover:text-foreground text-muted-foreground transition-colors">
              {user?.username}
            </span>
                    </div>
                </PopoverTrigger>

                <PopoverContent
                    align="end"
                    sideOffset={8}
                    className="w-64 p-4 bg-background border border-border/50 shadow-lg rounded-lg space-y-4"
                    onMouseEnter={handleMouseEnter}
                    onMouseLeave={handleMouseLeave}
                >
                    <div className="flex items-center justify-between">
                        <h3 className="font-semibold text-sm">Subscription</h3>
                        <Badge variant={isPro ? "pro" : "free"} className="text-xs px-2 py-0.5">
                            {isPro ? "Pro" : "Free"}
                        </Badge>
                    </div>

                    {isPro ? (
                        <div className="space-y-2">
                            <p className="text-sm text-muted-foreground flex items-center gap-2">
                                <Crown className="w-4 h-4 text-spred-yellow" />
                                Active until{" "}
                                <span className="font-medium text-foreground">
                  {new Date(user?.subscription?.currentPeriodEnd ?? "").toLocaleDateString()}
                </span>
                            </p>
                            <p className="text-xs text-muted-foreground">
                                Enjoy premium analytics and unlimited uploads.
                            </p>
                            <Link to="profile#subscription" className="w-full">
                                <Button
                                    variant="outline"
                                    size="sm"
                                    className="w-full mt-2 text-sm flex items-center justify-center gap-2"
                                >
                                    <CreditCard className="w-4 h-4" />
                                    Manage Subscription
                                </Button>
                            </Link>
                        </div>
                    ) : (
                        <div className="space-y-2">
                            <p className="text-sm text-muted-foreground">
                                Upgrade to Pro for advanced analytics and priority access.
                            </p>
                            <Link to="upgrade" className="w-full">
                                <Button
                                    size="sm"
                                    className="w-full text-sm flex items-center justify-center gap-2"
                                >
                                    <Crown className="w-4 h-4" />
                                    Upgrade to Pro
                                </Button>
                            </Link>
                        </div>
                    )}
                </PopoverContent>
            </Popover>

            <Button
                variant="ghost"
                size="sm"
                onClick={logout}
                className="text-muted-foreground hover:text-foreground"
            >
                <LogOut className="w-4 h-4 mr-2" />
                Logout
            </Button>
        </div>
    )
}

export default UserMenu
