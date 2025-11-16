import React, { useState } from "react"
import { cn } from "@/lib/utils"
import {
    Dialog,
    DialogContent,
    DialogHeader,
    DialogTitle,
    DialogFooter,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { UserModel } from "@/types/UserModel.ts"
import { useTheme } from "@/components/theme/useTheme"
import {theme} from "@/components/theme/theme-context.ts";
import {apiFetch} from "@/hooks/apiFetch.tsx";

type SettingsDialogProps = {
    open: boolean
    onClose: () => void
    user: UserModel
    onSave: (updated: UserModel) => void
}

type StreamingProvider = "spotify" | "applemusic" | "deezer" | "tidal" | "youtube"

const YoutubeIcon = () => (
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" className="size-5" fill="#FF0000">
        <path d="M23.5 6.2s-.2-1.6-.8-2.3c-.8-.9-1.7-.9-2.1-1-3-.2-7.6-.2-7.6-.2h-.1s-4.6 0-7.6.2c-.4 0-1.3 0-2.1 1-.6.7-.8 2.3-.8 2.3S2 8.1 2 10.1v1.8c0 2 .2 3.9.2 3.9s.2 1.6.8 2.3c.8.9 1.9.9 2.4 1 1.8.2 7.5.2 7.5.2s4.6 0 7.6-.2c.4 0 1.3 0 2.1-1 .6-.7.8-2.3.8-2.3s.2-1.9.2-3.9v-1.8c0-2-.2-3.9-.2-3.9zM9.8 14.6V8.4l6.3 3.1-6.3 3.1z"/>
    </svg>
)

const SpotifyIcon = () => (
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" className="size-5" fill="#1DB954">
        <path d="M12 0C5.4 0 0 5.4 0 12s5.4 12 12 12 12-5.4 12-12S18.66 0 12 0zm5.521 17.34c-.24.371-.721.49-1.101.24-3.021-1.851-6.832-2.271-11.312-1.241-.418.07-.832-.211-.901-.63-.07-.418.211-.832.63-.901 4.921-1.121 9.142-.601 12.452 1.441.39.241.509.721.241 1.101zm1.471-3.291c-.301.459-.921.6-1.381.3-3.461-2.131-8.722-2.751-12.842-1.511-.491.15-1.021-.12-1.171-.621-.15-.491.12-1.021.621-1.171 4.681-1.411 10.522-.721 14.452 1.711.448.3.6.931.3 1.381zm.127-3.421c-4.151-2.461-11.012-2.692-14.973-1.491-.601.18-1.231-.181-1.411-.781-.181-.601.181-1.231.781-1.411 4.561-1.381 12.132-1.111 16.893 1.721.571.331.761 1.081.421 1.651-.33.571-1.08.761-1.65.421z"/>
    </svg>
)

const AppleMusicIcon = () => (
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" className="size-5" fill="#FA243C">
        <path d="M16.365 1.43c.15-.42-.25-.84-.65-.67l-7.76 3.06c-.33.13-.55.46-.55.82v13.84c0 .54.52.92 1.03.76l7.76-2.34c.35-.1.59-.43.59-.8V1.43z"/>
    </svg>
)

const DeezerIcon = () => (
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" className="size-5" fill="#EF5466">
        <path d="M2 9h4v6H2zM7 6h4v12H7zM12 3h4v18h-4zM17 0h4v24h-4z"/>
    </svg>
)

const TidalIcon = () => (
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" className="size-5" fill="#000">
        <path d="M7 0l5 5-5 5-5-5zm10 0l5 5-5 5-5-5zM12 10l5 5-5 5-5-5zm10 0l2 2-7 7-2-2z"/>
    </svg>
)

const SettingsDialog = ({ open, onClose, user, onSave }: SettingsDialogProps) => {
    const { resolvedTheme, setTheme, theme } = useTheme()
    const [linkedAccounts, setLinkedAccounts] = useState<string[]>(user?.linkedAccounts ?? [])

    const providerConfig: Record<
        StreamingProvider,
        { icon: React.ReactNode; label: string; brand: string }
    > = {
        spotify: { icon: <SpotifyIcon />, label: "Spotify", brand: "#1DB954" },
        applemusic: { icon: <AppleMusicIcon />, label: "Apple Music", brand: "#FA243C" },
        deezer: { icon: <DeezerIcon />, label: "Deezer", brand: "#EF5466" },
        tidal: { icon: <TidalIcon />, label: "Tidal", brand: "#000000" },
        youtube: { icon: <YoutubeIcon />, label: "YouTube Music", brand: "#FF0000" },
    }

    const themes: theme[] = ["light", "dark", "system"] as const
    const providers: StreamingProvider[] = ["spotify", "applemusic", "deezer", "tidal", "youtube"]

    const handleLinkAccount = (provider: string) => {
        if (!linkedAccounts.includes(provider)) {
            setLinkedAccounts([...linkedAccounts, provider])
        }
    }

    const handleUnlinkAccount = (provider: string) => {
        setLinkedAccounts(linkedAccounts.filter(acc => acc !== provider))
    }

    // theme-based classes
    const bgClass =
        resolvedTheme === "dark"
            ? "bg-zinc-900 text-zinc-100 border-zinc-800"
            : "bg-white text-zinc-900 border-zinc-200"

    const inputBg =
        resolvedTheme === "dark" ? "bg-zinc-800 text-zinc-100" : "bg-zinc-50 text-zinc-900"

    return (
        <Dialog open={open} onOpenChange={onClose}>
            <DialogContent className="sm:max-w-[600px] p-6 bg-background text-foreground border border-border">
                <DialogHeader>
                    <DialogTitle className="text-base-custom leading-6">Settings</DialogTitle>
                </DialogHeader>

                <div className="flex flex-col gap-6 mt-4">
                    {/* Email (read only) */}
                    <div>
                        <p className="text-xs-custom text-muted-foreground mb-1">Email</p>
                        <Input
                            value={user.email}
                            disabled
                            className="text-sm w-full bg-muted text-foreground"
                        />
                    </div>

                    {/* Linked Accounts */}
                    <div>
                        <p className="text-xs-custom text-muted-foreground mb-2">Streaming Accounts</p>
                        <div className="flex flex-col gap-3">
                            {providers.map((provider) => {
                                const isLinked = linkedAccounts.includes(provider)
                                const cfg = providerConfig[provider]

                                return (
                                    <div
                                        key={provider}
                                        className="flex items-center justify-between rounded-lg border border-border bg-card p-3 shadow-sm hover:shadow-md transition"
                                    >
                                        <div className="flex items-center gap-2">
                                            {cfg.icon}
                                            <span className="text-sm font-medium">{cfg.label}</span>
                                        </div>

                                        {isLinked ? (
                                            <Button
                                                size="sm"
                                                variant="outline"
                                                className="text-red-500 hover:bg-red-50 dark:hover:bg-red-950"
                                                onClick={() => handleUnlinkAccount(provider)}
                                            >
                                                Unlink
                                            </Button>
                                        ) : (
                                            <Button
                                                size="sm"
                                                variant="outline"
                                                className={cn("hover:border", `hover:border-[${cfg.brand}]`)}
                                                onClick={() => handleLinkAccount(provider)}
                                            >
                                                Connect
                                            </Button>
                                        )}
                                    </div>
                                )
                            })}
                        </div>
                    </div>

                    {/* Appearance */}
                    <div>
                        <p className="text-xs-custom text-muted-foreground mb-1">Appearance</p>
                        <div className="flex gap-3">
                            {themes.map((t) => (
                                <Button
                                    key={t}
                                    type="button"
                                    variant={theme === t ? "default" : "outline"}
                                    onClick={() => setTheme(t)}
                                    className="text-sm"
                                >
                                    {t.charAt(0).toUpperCase() + t.slice(1)}
                                </Button>
                            ))}
                        </div>
                    </div>
                </div>
            </DialogContent>
        </Dialog>
    )
}

export default SettingsDialog
