import { useEffect, useMemo, useRef, useState } from "react";
import { ExternalLink, Trash2, Users, TrendingUp, Music, Plus } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card.tsx";
import { Button } from "@/components/ui/button.tsx";
import { useToast } from "@/hooks/use-toast.ts";
import { CatalogMetadata, CatalogStats } from "@/types/CatalogMetadata.ts";
import { TagsBlock } from "@/components/ui/TagsBlock.tsx";
import { Link } from "react-router-dom";
import { SERVICES } from "@/constants/services";
import { apiFetch } from "@/hooks/apiFetch";

type StatsDto = {
    catalogId: string;
    totalSubmissions: number;
    pendingSubmissions: number;
    acceptedSubmissions: number;
    declinedSubmissions: number;
    weeklySubmissions: number;
    monthlySubmissions: number;
};

const IDS_PATH = `${SERVICES.PLAYLISTS}`;
const DETAIL_PATH = (id: string) => `${SERVICES.PLAYLISTS}/${id}`;
const STATS_PATH = `${SERVICES.SUBMISSIONS}/stats`;

const PAGE_SIZE = 9;
const CONCURRENCY = 4;

const CuratorDashboard = () => {
    const { toast } = useToast();

    const [playlistIds, setPlaylistIds] = useState<string[]>([]);
    const [statsMap, setStatsMap] = useState<Record<string, CatalogStats>>({});
    const [playlistsMap, setPlaylistsMap] = useState<Record<string, CatalogMetadata>>({});
    const [page, setPage] = useState(1);
    const [loadingIds, setLoadingIds] = useState(true);
    const [loadingStats, setLoadingStats] = useState(true);
    const [loadingBatch, setLoadingBatch] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const queueRef = useRef<string[]>([]);

    const idsToRender = useMemo(() => playlistIds.slice(0, PAGE_SIZE * page), [playlistIds, page]);

    const getPlatformBadgeColor = (platform: CatalogMetadata["platform"]) => {
        switch (platform) {
            case "spotify":
                return "text-green-500";
            case "apple-music":
                return "text-red-500";
            case "youtube-music":
                return "text-red-600";
            case "soundcloud":
                return "text-orange-500";
            default:
                return "text-gray-500";
        }
    };

    const handleDeletePlaylist = async (playlistId: string) => {
        try {
            await apiFetch(`${SERVICES.PLAYLISTS}/playlists/${playlistId}`, { method: "DELETE" });
            setPlaylistIds(prev => prev.filter(id => id !== playlistId));
            setPlaylistsMap(prev => {
                const next = { ...prev };
                delete next[playlistId];
                return next;
            });
            setStatsMap(prev => {
                const next = { ...prev };
                delete next[playlistId];
                return next;
            });
            toast({ title: "Success", description: "Playlist deleted successfully" });
        } catch {
            toast({ title: "Error", description: "Failed to delete playlist" });
        }
    };

    const totalFollowers = useMemo(
        () => idsToRender.reduce((sum, id) => sum + (playlistsMap[id]?.followers ?? 0), 0),
        [idsToRender, playlistsMap]
    );

    const totalPending = useMemo(
        () => Object.values(statsMap).reduce((sum, s) => sum + (s.pendingSubmissions ?? 0), 0),
        [statsMap]
    );

    const totalMonth = useMemo(
        () => Object.values(statsMap).reduce((sum, s) => sum + (s.monthlySubmissions ?? 0), 0),
        [statsMap]
    );

    useEffect(() => {
        let cancelled = false;
        const loadIds = async () => {
            setLoadingIds(true);
            setError(null);
            try {
                const playlists = await apiFetch(IDS_PATH).then(r => r.json() as Promise<CatalogMetadata[]>);
                setPlaylistIds(playlists.map(p => p.id));
                setPlaylistsMap(Object.fromEntries(playlists.map(p => [p.id, p])));
            } catch {
                toast({ title: "Error", description: "Failed to load playlists" });
                setPlaylistIds([]);
                setPlaylistsMap({});
            } finally {
                setLoadingIds(false);
            }
        };
        const loadStats = async () => {
            setLoadingStats(true);
            try {
                const stats = await apiFetch(STATS_PATH).then(r => r.json() as Promise<StatsDto[]>);
                if (cancelled) return;
                const map: Record<string, CatalogStats> = {};
                (stats ?? []).forEach(s => {
                    map[s.catalogId] = {
                        playlistId: s.catalogId,
                        totalSubmissions: s.totalSubmissions,
                        pendingSubmissions: s.pendingSubmissions,
                        acceptedSubmissions: s.acceptedSubmissions,
                        declinedSubmissions: s.declinedSubmissions,
                        weeklySubmissions: s.weeklySubmissions,
                        monthlySubmissions: s.monthlySubmissions
                    };
                });
                setStatsMap(map);
            } catch {
                if (cancelled) return;
                toast({ title: "Warning", description: "Failed to load stats" });
            } finally {
                if (!cancelled) setLoadingStats(false);
            }
        };
        loadIds();
        loadStats();
        return () => {
            cancelled = true;
        };
    }, []);

    return (
        <div className="container mx-auto p-6 space-y-8">
            <div className="flex justify-between items-center">
                <div>
                    <h1 className="text-3xl font-bold text-foreground">Curator Dashboard</h1>
                    <p className="text-muted-foreground mt-2">Manage your playlists and track submissions</p>
                </div>
                <Link to="/curator/accounts">
                    <Button className="transition-opacity gap-2">
                        <Plus className="h-4 w-4" />
                        Connect Accounts
                    </Button>
                </Link>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
                <Card>
                    <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                        <CardTitle className="text-sm font-medium">Total Playlists</CardTitle>
                        <Music className="h-4 w-4 text-muted-foreground" />
                    </CardHeader>
                    <CardContent>
                        <div className="text-2xl font-bold">{loadingIds ? "…" : playlistIds.length}</div>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                        <CardTitle className="text-sm font-medium">Total Followers</CardTitle>
                        <Users className="h-4 w-4 text-muted-foreground" />
                    </CardHeader>
                    <CardContent>
                        <div className="text-2xl font-bold">{loadingIds ? "…" : totalFollowers.toLocaleString()}</div>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                        <CardTitle className="text-sm font-medium">Pending Submissions</CardTitle>
                        <TrendingUp className="h-4 w-4 text-muted-foreground" />
                    </CardHeader>
                    <CardContent>
                        <div className="text-2xl font-bold">{loadingStats ? "…" : totalPending}</div>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                        <CardTitle className="text-sm font-medium">This Month</CardTitle>
                        <TrendingUp className="h-4 w-4 text-muted-foreground" />
                    </CardHeader>
                    <CardContent>
                        <div className="text-2xl font-bold">{loadingStats ? "…" : totalMonth}</div>
                    </CardContent>
                </Card>
            </div>

            <div>
                <h2 className="text-2xl font-semibold mb-6">My Playlists</h2>
                {error && <div className="text-sm text-destructive mb-4">{error}</div>}

                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                    {(loadingIds ? Array.from({ length: PAGE_SIZE }) : idsToRender).map((item, idx) => {
                        const isSkeleton = loadingIds;
                        const id = isSkeleton ? `skeleton-${idx}` : (item as string);
                        const playlist = isSkeleton ? null : playlistsMap[id];
                        const stats = isSkeleton ? null : statsMap[id];

                        return (
                            <Card key={id} className="group transition-shadow hover:shadow-lg hover:shadow-spred-yellowdark/30">
                                <CardHeader>
                                    <div className="flex justify-between items-start">
                                        <div className="flex-1">
                                            <CardTitle className={`text-lg ${!playlist ? "h-5 w-40 bg-muted animate-pulse rounded" : ""}`}>
                                                {!playlist ? "" : playlist.name}
                                            </CardTitle>
                                            <CardDescription className={`mt-1 ${!playlist ? "h-4 w-56 bg-muted animate-pulse rounded" : ""}`}>
                                                {!playlist ? "" : (playlist.description ?? "")}
                                            </CardDescription>
                                        </div>
                                        <div className="flex gap-2">
                                            <Button
                                                variant="ghost"
                                                size="sm"
                                                disabled={!playlist}
                                                onClick={() => window.open(playlist?.tracksHref ?? "#", "_blank")}
                                            >
                                                <ExternalLink className="h-4 w-4" />
                                            </Button>
                                            <Button
                                                variant="ghost"
                                                size="sm"
                                                disabled={!playlist}
                                                onClick={() => handleDeletePlaylist(id)}
                                                className="text-destructive hover:text-destructive"
                                            >
                                                <Trash2 className="h-4 w-4" />
                                            </Button>
                                        </div>
                                    </div>

                                    <div className="flex items-center gap-2 mt-3">
                                        {!playlist ? (
                                            <div className="h-6 w-32 bg-muted animate-pulse rounded" />
                                        ) : playlist.tags?.length ? (
                                            <TagsBlock tags={playlist.tags} />
                                        ) : null}
                                    </div>
                                </CardHeader>

                                <CardContent>
                                    <div className="space-y-3">
                                        <div className="flex justify-between text-sm">
                                            <span className="text-muted-foreground">Platform</span>
                                            <span className={!playlist ? "h-4 w-24 bg-muted animate-pulse rounded" : `${getPlatformBadgeColor(playlist.platform)}`}>
                        {!playlist ? "" : playlist.platform.replace("-", " ")}
                      </span>
                                        </div>

                                        <div className="flex justify-between text-sm">
                                            <span className="text-muted-foreground">Followers</span>
                                            <span className={!playlist ? "h-4 w-10 bg-muted animate-pulse rounded" : "font-medium"}>
                        {!playlist ? "" : (playlist.followers ?? 0).toLocaleString()}
                      </span>
                                        </div>

                                        {!playlist ? (
                                            <>
                                                <div className="flex justify-between text-sm">
                                                    <span className="text-muted-foreground">Pending</span>
                                                    <span className="h-4 w-8 bg-muted animate-pulse rounded" />
                                                </div>
                                                <div className="flex justify-between text-sm">
                                                    <span className="text-muted-foreground">This Week</span>
                                                    <span className="h-4 w-8 bg-muted animate-pulse rounded" />
                                                </div>
                                                <div className="flex justify-between text-sm">
                                                    <span className="text-muted-foreground">Total Submissions</span>
                                                    <span className="h-4 w-8 bg-muted animate-pulse rounded" />
                                                </div>
                                            </>
                                        ) : stats ? (
                                            <>
                                                <div className="flex justify-between text-sm">
                                                    <span className="text-muted-foreground">Pending</span>
                                                    <span className="font-medium text-yellow-600">{stats.pendingSubmissions}</span>
                                                </div>
                                                <div className="flex justify-between text-sm">
                                                    <span className="text-muted-foreground">This Week</span>
                                                    <span className="font-medium">{stats.weeklySubmissions}</span>
                                                </div>
                                                <div className="flex justify-between text-sm">
                                                    <span className="text-muted-foreground">Total Submissions</span>
                                                    <span className="font-medium">{stats.totalSubmissions}</span>
                                                </div>
                                            </>
                                        ) : (
                                            <div className="text-xs text-muted-foreground">No stats available</div>
                                        )}
                                    </div>
                                </CardContent>
                            </Card>
                        );
                    })}
                </div>

                {PAGE_SIZE * page < playlistIds.length && (
                    <div className="flex justify-center mt-8">
                        <Button
                            onClick={() => setPage(p => p + 1)}
                            disabled={loadingBatch}
                            className="min-w-40"
                        >
                            {loadingBatch ? "Loading…" : "Load more"}
                        </Button>
                    </div>
                )}
            </div>
        </div>
    );
};

export default CuratorDashboard;
