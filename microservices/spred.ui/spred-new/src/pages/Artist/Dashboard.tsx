import React, { useEffect, useRef, useState, useCallback } from 'react';
import { Search } from 'lucide-react';
import UserTracks from '@/components/tracks/UserTracks.tsx';
import { Input } from '@/components/ui/input.tsx';
import { useAuth } from "@/components/authorization/AuthProvider.tsx";
import { Track } from "@/types/Track.ts";
import { apiFetch } from "@/hooks/apiFetch.tsx";
import { SERVICES } from "@/constants/services.tsx";
import { Loading } from "@/components/loading/loading.tsx";
import {toast} from "sonner";
import {useTheme} from "@/components/theme/useTheme.ts";
import {UpgradeBanner} from "@/components/banner/UpgradeBanner.tsx";


const Dashboard: React.FC = () => {
    const [searchQuery, setSearchQuery] = useState('');
    const { user } = useAuth();
    const [tracks, setTracks] = useState<Track[]>([]);
    const [loading, setLoading] = useState(false);
    const [offset, setOffset] = useState(0);
    const [limit] = useState(9);
    const [hasMore, setHasMore] = useState(true);
    const loadingRef = useRef(false);
    const hasMoreRef = useRef(true);
    const loaderRef = useRef<HTMLDivElement | null>(null);
    const { resolvedTheme } = useTheme();

    const fetchTracks = useCallback(async (currentOffset: number, append: boolean) => {
        if (loadingRef.current || !hasMoreRef.current) return;
        loadingRef.current = true;

        try {
            setLoading(true);
            const query = searchQuery
                ? `?title=${encodeURIComponent(searchQuery)}&offset=${currentOffset}&limit=${limit}`
                : `?offset=${currentOffset}&limit=${limit}`;

            const res = await apiFetch(`${SERVICES.TRACK}${query}`, {
                method: "GET",
                headers: { 'Content-Type': 'application/json' },
            });

            const data = await res.json();
            const fetchedTracks: Track[] = Array.isArray(data?.tracks) ? data.tracks : [];

            setTracks(prev => append ? [...prev, ...fetchedTracks] : fetchedTracks);

            if (fetchedTracks.length < limit) {
                hasMoreRef.current = false;
                setHasMore(false);
            } else {
                hasMoreRef.current = true;
                setHasMore(true);
            }
        } catch (err) {
            setHasMore(false);
            toast.error("Failed to fetch tracks");
            console.error(err);
        } finally {
            loadingRef.current = false;
            setLoading(false);
        }
    }, [searchQuery, limit]);

    useEffect(() => {
        setTracks([]);
        setHasMore(true);
        hasMoreRef.current = true;
        setOffset(0);
        loadingRef.current = false;
        setLoading(false);
        (async () => {
            await fetchTracks(0, false);
        })();
    }, [searchQuery, fetchTracks]);

    useEffect(() => {
        (async () => {
            await fetchTracks(offset, offset > 0).catch(console.error);
        })();
    }, [offset, fetchTracks]);

    useEffect(() => {
        const observer = new IntersectionObserver((entries) => {
            const target = entries[0];
            if (target.isIntersecting && !loading && hasMore) {
                setOffset(prev => prev + limit);
            }
        });

        const currentLoader = loaderRef.current;
        if (currentLoader) observer.observe(currentLoader);

        return () => {
            if (currentLoader) observer.unobserve(currentLoader);
        };
    }, [loading, hasMore, limit]);

    const handleDeleted = (id: string) => {
        setTracks(prev => prev.filter(t => t.id !== id));
    };

    return (
        <div className="h-[calc(100vh-128px)] overflow-y-auto">
            <div className="max-w-7xl mx-auto px-4 md:px-6 pt-8 md:pt-12">
                <div className="animate-fade-in">
                    <h1 className="text-3xl md:text-4xl font-bold mb-2">Welcome back, {user.username}</h1>
                    <p className="text-muted-foreground mb-6 md:mb-8">
                        Here are your uploaded tracks. Select one to see personalized recommendations.
                    </p>

                    {!user?.subscription?.isActive && (
                        <div className="mb-6 md:mb-8">
                            <UpgradeBanner />
                        </div>
                    )}

                    {/* Search Bar */}
                    <div className="flex flex-col md:flex-row items-stretch md:items-center gap-4 mb-8 md:mb-10">
                        <div className="relative flex-grow">
                            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground" size={18} />
                            <Input
                                type="text"
                                placeholder="Search your tracks..."
                                className="pl-10 bg-background/5 border-border"
                                value={searchQuery}
                                theme={resolvedTheme}
                                onChange={(e) => setSearchQuery(e.target.value)}
                            />
                        </div>
                    </div>

                    {/* User Tracks */}
                    <div className="space-y-12">
                        <UserTracks tracks={tracks} onDeleted={handleDeleted} />
                        {loading && <Loading />}
                        <div ref={loaderRef} className="h-10" />
                        {!hasMore && tracks.length > 0 && <p className="text-muted-foreground text-center">No more tracks to load.</p>}
                    </div>
                </div>
            </div>
        </div>
    );
};

export default Dashboard;
