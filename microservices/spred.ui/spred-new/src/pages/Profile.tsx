import React, { useEffect, useState } from "react"
import {
    User,
    MapPin,
    Music,
    Edit,
    Crown,
    Sparkles,
    CreditCard,
    AlertTriangle,
    ExternalLink,
} from "lucide-react"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
    AlertDialogTrigger,
} from "@/components/ui/alert-dialog"
import { toast } from "@/components/ui/use-toast"
import { useAuth } from "@/components/authorization/AuthProvider"
import EditProfileDialog from "@/components/profile/EditProfileDialog"
import SettingsDialog from "@/components/profile/SettingsDialog"
import { apiFetch } from "@/hooks/apiFetch"
import { SERVICES } from "@/constants/services"
import { UserActivity } from "@/types/UserActivity"
import {Link} from "react-router-dom";
import {UpgradeBanner} from "@/components/banner/UpgradeBanner.tsx";

const Profile: React.FC = () => {
    const { user, refetchUser } = useAuth()
    const [activities, setActivities] = useState<UserActivity[]>([])
    const [editOpen, setEditOpen] = useState(false)
    const [settingsOpen, setSettingsOpen] = useState(false)

    useEffect(() => {
        const fetchActivities = async () => {
            const res = await apiFetch("/me/activity")
            if (res.ok) setActivities(await res.json())
        }
        fetchActivities()
    }, [])

    useEffect(() => {
        if (window.location.hash) {
            const element = document.querySelector(window.location.hash)
            if (element) {
                element.scrollIntoView({ behavior: "smooth" })
            }
        }
    }, [])

    return (
        <div className="h-[calc(100vh-128px)] overflow-y-auto scroll-smooth">
            <div className="max-w-6xl mx-auto px-6 md:px-12 py-12 space-y-10">
                {/* --- Header --- */}
                <header className="flex flex-col md:flex-row gap-6 items-start md:items-center">
                    <Avatar className="w-24 h-24 border-2 border-spred-yellow">
                        {user?.avatarUrl ? (
                            <AvatarImage src={user.avatarUrl} />
                        ) : (
                            <AvatarFallback className="bg-spred-yellow text-black text-xl">
                                {user?.username?.[0]?.toUpperCase() ?? "U"}
                            </AvatarFallback>
                        )}
                    </Avatar>

                    <div className="flex-1 space-y-3">
                        <h1 className="text-4xl font-bold">{user?.username}</h1>
                        <div className="flex flex-wrap gap-4 text-muted-foreground">
                            {user?.location && (
                                <div className="flex items-center gap-1">
                                    <MapPin size={16} />
                                    <span>{user.location}</span>
                                </div>
                            )}
                            {user?.genres?.length > 0 && (
                                <div className="flex items-center gap-1">
                                    <Music size={16} />
                                    <span>{user.genres.map((g) => g.name).join(", ")}</span>
                                </div>
                            )}
                        </div>

                        <div className="flex flex-wrap gap-3">
                            <Button
                                variant="outline"
                                onClick={() => setEditOpen(true)}
                                className="gap-2 bg-accent/50"
                            >
                                <Edit size={16} />
                                Edit Profile
                            </Button>

                            {/*<Button*/}
                            {/*    variant="outline"*/}
                            {/*    onClick={() => setSettingsOpen(true)}*/}
                            {/*    className="gap-2 bg-accent/50"*/}
                            {/*>*/}
                            {/*    <User size={16} />*/}
                            {/*    Settings*/}
                            {/*</Button>*/}
                        </div>
                    </div>
                </header>

                {/* --- Tabs Section --- */}
                <section id="tabs" className="space-y-4">
                    <div className="flex justify-end gap-4">
                        <a href="#tabs" className="text-sm text-muted-foreground hover:underline">
                            Overview
                        </a>
                        <a
                            href="#subscription"
                            className="text-sm text-muted-foreground hover:underline"
                        >
                            Subscription
                        </a>
                    </div>

                    <Tabs defaultValue="overview" className="w-full">
                        <TabsList className="grid w-full grid-cols-2 mb-8 bg-accent/50">
                            <TabsTrigger value="overview">Overview</TabsTrigger>
                            <TabsTrigger value="history">Activity</TabsTrigger>
                        </TabsList>

                        {/* Overview Tab */}
                        <TabsContent value="overview" className="space-y-8">
                            <div className="grid md:grid-cols-3 gap-6">
                                <div className="glassmorphism p-6">
                                    <h3 className="font-bold text-lg mb-4">Artist Bio</h3>
                                    <p className="text-muted-foreground">
                                        {user?.bio ?? "No bio provided yet."}
                                    </p>
                                </div>

                                <div className="glassmorphism p-6">
                                    <h3 className="font-bold text-lg mb-4">Genre Expertise</h3>
                                    {user?.genres?.length ? (
                                        user.genres.map((g) => (
                                            <div key={g.name} className="mb-3">
                                                <div className="flex justify-between text-sm mb-1">
                                                    <span>{g.name}</span>
                                                    <span>{g.confidence}%</span>
                                                </div>
                                                <div className="w-full bg-accent/50 h-2 rounded-full">
                                                    <div
                                                        className="bg-spred-yellow h-2 rounded-full"
                                                        style={{ width: `${g.confidence}%` }}
                                                    />
                                                </div>
                                            </div>
                                        ))
                                    ) : (
                                        <p className="text-muted-foreground text-sm">
                                            No genres specified.
                                        </p>
                                    )}
                                </div>

                                <div className="glassmorphism p-6">
                                    <h3 className="font-bold text-lg mb-4">Stats</h3>
                                    {user && (
                                        <div className="grid grid-cols-2 gap-4">
                                            {[
                                                ["Tracks Analyzed", user?.stats?.tracksAnalyzed],
                                                ["Matches Found", user?.stats?.matchesFound],
                                                ["Pitches Sent", user?.stats?.pitchesSent],
                                                ["Placements", user?.stats?.placements],
                                            ].map(([label, value]) => (
                                                <div
                                                    key={label}
                                                    className="bg-accent/50 p-3 rounded-md text-center"
                                                >
                                                    <p className="text-2xl font-bold text-spred-yellow">
                                                        {value ?? 0}
                                                    </p>
                                                    <p className="text-sm text-muted-foreground">
                                                        {label}
                                                    </p>
                                                </div>
                                            ))}
                                        </div>
                                    )}
                                </div>
                            </div>
                        </TabsContent>

                        {/* History Tab */}
                        <TabsContent value="history">
                            <div className="glassmorphism p-6">
                                <h3 className="font-bold text-lg mb-4">Recent Activity</h3>
                                {activities.length === 0 ? (
                                    <p className="text-muted-foreground text-sm">
                                        No recent activity yet.
                                    </p>
                                ) : (
                                    <div className="space-y-3">
                                        {activities.map((a) => (
                                            <div
                                                key={a.id}
                                                className="flex items-center gap-3 bg-accent/50 p-3 rounded-md"
                                            >
                                                <div className="w-10 h-10 bg-spred-yellow/20 rounded-full flex items-center justify-center">
                                                    {a.type === "track_upload" && (
                                                        <Music size={18} className="text-spred-yellow" />
                                                    )}
                                                    {a.type === "pitch" && (
                                                        <ExternalLink size={18} className="text-spred-yellow" />
                                                    )}
                                                    {a.type === "user_update" && (
                                                        <User size={18} className="text-spred-yellow" />
                                                    )}
                                                </div>
                                                <div>
                                                    <p className="font-medium">{a.title}</p>
                                                    <p className="text-sm text-muted-foreground">
                                                        {new Date(a.timestamp).toLocaleDateString()}
                                                    </p>
                                                </div>
                                            </div>
                                        ))}
                                    </div>
                                )}
                            </div>
                        </TabsContent>
                    </Tabs>
                </section>

                {/* --- Subscription Section --- */}
                <section id="subscription" className="space-y-4">
                    <h2 className="text-2xl font-bold flex items-center gap-2">
                        Subscription
                        {user?.subscription?.isActive ? (
                            <Crown className="h-5 w-5 text-primary" />
                        ) : (
                            <Sparkles className="h-5 w-5 text-muted-foreground" />
                        )}
                    </h2>

                    <div className="glassmorphism p-6">
                        {user?.subscription?.isActive ? (
                            <div className="space-y-6">
                                <div className="flex items-center justify-between">
                                    <div>
                                        <h3 className="text-lg font-semibold">Pro Plan</h3>
                                        <p className="text-sm text-muted-foreground">
                                            Unlimited uploads, premium analytics & recommendations.
                                        </p>
                                    </div>
                                    <Badge variant="pro" className="text-base px-3 py-1">
                                        Pro
                                    </Badge>
                                </div>

                                <div className="grid sm:grid-cols-2 gap-4">
                                    <div className="rounded-lg border border-primary/20 bg-primary/5 p-4">
                                        <div className="flex items-center gap-2 mb-1">
                                            <CreditCard className="h-4 w-4 text-primary" />
                                            <p className="text-sm font-medium">Billing</p>
                                        </div>
                                        <p className="text-2xl font-bold">$19</p>
                                        <p className="text-sm text-muted-foreground">per month</p>
                                    </div>

                                    <div className="rounded-lg border border-border bg-muted/50 p-4">
                                        <div className="flex items-center gap-2 mb-1">
                                            <Crown className="h-4 w-4 text-primary" />
                                            <p className="text-sm font-medium">Status</p>
                                        </div>
                                        <p className="text-sm font-semibold text-success">Active</p>
                                        <p className="text-sm text-muted-foreground">
                                            Next billing:{" "}
                                            {new Date(
                                                user?.subscription?.currentPeriodEnd ?? ""
                                            ).toLocaleDateString() || "—"}
                                        </p>
                                    </div>
                                </div>

                                <AlertDialog>
                                    <AlertDialogTrigger asChild>
                                        <Button
                                            variant="destructive"
                                        >
                                            <AlertTriangle className="h-4 w-4 mr-2" />
                                            Cancel Subscription
                                        </Button>
                                    </AlertDialogTrigger>
                                    <AlertDialogContent>
                                        <AlertDialogHeader>
                                            <AlertDialogTitle>
                                                Cancel Pro Subscription?
                                            </AlertDialogTitle>
                                            <AlertDialogDescription>
                                                You’ll lose access to Pro features at the end of this
                                                billing cycle.
                                            </AlertDialogDescription>
                                        </AlertDialogHeader>
                                        <AlertDialogFooter>
                                            <AlertDialogCancel>Keep Pro Plan</AlertDialogCancel>
                                            <AlertDialogAction
                                                onClick={async () => {
                                                    const res = await apiFetch(
                                                        `${SERVICES.SUBSCRIPTION}/cancel`,
                                                        {
                                                            method: "POST",
                                                            headers: { "Content-Type": "application/json" },
                                                            credentials: "include",
                                                            body: JSON.stringify({
                                                                reason: "user_cancelled",
                                                            }),
                                                        }
                                                    )
                                                    if (res.ok) {
                                                        toast({
                                                            title: "Subscription cancelled",
                                                            description:
                                                                "Pro plan will remain active until the end of billing period.",
                                                        })
                                                        await refetchUser()
                                                    } else {
                                                        toast({
                                                            title: "Error",
                                                            description: "Failed to cancel subscription.",
                                                            variant: "destructive",
                                                        })
                                                    }
                                                }}
                                                className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                                            >
                                                Confirm Cancellation
                                            </AlertDialogAction>
                                        </AlertDialogFooter>
                                    </AlertDialogContent>
                                </AlertDialog>
                            </div>
                        ) : (
                            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                                <div>
                                    <h3 className="text-lg font-semibold">Free Plan</h3>
                                    <p className="text-sm text-muted-foreground">
                                        Upgrade to Pro to unlock premium analytics and unlimited
                                        uploads.
                                    </p>
                                </div>
                                <UpgradeBanner/>
                            </div>
                        )}
                    </div>
                </section>
            </div>

            <EditProfileDialog
                open={editOpen}
                onClose={() => setEditOpen(false)}
                user={user}
                onSave={refetchUser}
            />
            <SettingsDialog
                open={settingsOpen}
                onClose={() => setSettingsOpen(false)}
                user={user}
                onSave={() => {}}
            />
        </div>
    )
}

export default Profile
