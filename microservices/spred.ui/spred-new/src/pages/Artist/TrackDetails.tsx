import React, {useCallback, useEffect, useRef, useState, useMemo} from 'react';
import {Link, useNavigate, useParams} from 'react-router-dom';
import {ChevronLeft, Filter, Trash2, Edit, X} from 'lucide-react';
import RecommendationCard from '@/components/ui/RecommendationCard.tsx';
import {SERVICES} from '@/constants/services.tsx';
import {apiFetch} from '@/hooks/apiFetch.tsx';
import {Track} from '@/types/Track.ts';
import {InferenceMetadata, InferenceResult} from '@/types/InferenceResult.ts';
import {AudioPlayer} from '@/components/player/AudioPlayer.tsx';
import {CatalogMetadata} from '@/types/CatalogMetadata.ts';
import {Loading} from '@/components/loading/loading.tsx';
import {useToast} from '@/hooks/use-toast.ts';
import {TagsBlock} from "@/components/ui/TagsBlock.tsx";
import { Button } from '@/components/ui/button.tsx';
import {FilterSidebar} from "@/components/ui/FilterSidebar.tsx";
import {clsx} from "clsx";

interface EnrichedMetadata {
  catalog: CatalogMetadata;
  inference: InferenceMetadata;
}

const SkeletonCard = () => (
    <div className="backdrop-blur-sm border rounded-lg overflow-hidden transition-all max-w-[240px] h-[320px] min-h-[320px] flex-shrink-0
  card-hover animate-fade-pulse flex flex-col animate-fade-in">
      <div className="aspect-square w-full bg-muted" />
      <div className="p-3 flex flex-col gap-2">
        <div className="h-3 w-1/4 bg-muted rounded" />
        <div className="h-4 w-3/4 bg-muted rounded" />
        <div className="h-3 w-1/2 bg-muted rounded" />
        <div className="h-8 w-full bg-muted rounded mt-1" />
      </div>
    </div>
);

const TrackDetails: React.FC = () => {
  const { trackId } = useParams<{ trackId: string }>();
  const navigate = useNavigate();
  const { toast } = useToast();

  const PAGE_SIZE = 10;

  const [track, setTrack] = useState<Track | null>(null);
  const [playlists, setPlaylists] = useState<EnrichedMetadata[]>([]);
  const [labels, setLabels] = useState<EnrichedMetadata[]>([]);
  const [playlistPendingCount, setPlaylistPendingCount] = useState(0);
  const [labelPendingCount, setLabelPendingCount] = useState(0);
  const [isOpen, setIsOpen] = useState(false)

  const [isEditing, setIsEditing] = useState(false);
  const [newTitle, setNewTitle] = useState(track?.title ?? "");

  const hasMorePlaylists = useRef(true);
  const hasMoreLabels = useRef(true);

  const playlistPage = useRef(0);
  const labelPage = useRef(0);
  const isLoadingPlaylists = useRef(false);
  const isLoadingLabels = useRef(false);

  const [filterType, setFilterType] = useState<'all' | 'playlists' | 'labels'>('all');
  const [loading, setLoading] = useState(false);

  const playlistLoaderRef = useRef<HTMLDivElement | null>(null);
  const labelLoaderRef = useRef<HTMLDivElement | null>(null);

  const artistNames = track?.artists.map((a) => a.name).join(', ');

  const [filters, setFilters] = useState<{ type: string[]; tags: { include: string[]; exclude: string[] } }>({
    type: [],
    tags: { include: [], exclude: [] },
  });

  const fetchTrackData = useCallback(async () => {
    if (!trackId) return;
    setLoading(true);
    try {
      const res = await apiFetch(`${SERVICES.TRACK}/${trackId}`, { method: 'GET' });
      const trackData = await res.json();
      setTrack(trackData);
      setNewTitle(trackData.title);
    } catch {
      toast({ title: 'Failed to fetch track', variant: 'destructive' });
    } finally {
      setLoading(false);
    }
  }, [toast, trackId]);

  const handleUpdateTitle = async () => {
    if (!newTitle.trim()) return;
    try {
      const updatedTrack = { ...track, title: newTitle };
      const res = await apiFetch(`${SERVICES.TRACK}/${trackId}`, {
        method: "PATCH",
        body: JSON.stringify(updatedTrack),
        headers: { "Content-Type": "application/json" }
      });
      if (res.ok) {
        setTrack((prev) => prev ? { ...prev, title: newTitle } : prev);
        toast({ title: "Title updated successfully" });
        setIsEditing(false);
      } else {
        toast({ title: "Failed to update title", variant: "destructive" });
      }
    } catch {
      toast({ title: "Network error", variant: "destructive" });
    }
  };

  const fetchAndEnrich = useCallback(async (type: 'playlist' | 'record_label', page: number) => {
    const limit = PAGE_SIZE;
    const offset = (page - 1) * PAGE_SIZE;

    const res = await apiFetch(`${SERVICES.INFERENCE}/${trackId}?type=${type}&limit=${limit}&offset=${offset}`, { method: 'GET' });
    if (!res.ok) return [];
    const result: InferenceResult = await res.json();
    const data = result.metadata;
    const modelVersion = result.modelInfo;
    if (!data.length) return [];

    const owner = data[0].metadataOwner;

    return await Promise.all(
        data.map(async (inference) => {
          try {
            const catalogRes = await apiFetch(`${SERVICES.PLAYLISTS}/${owner}/${inference.metadataId}`, { method: 'GET' });
            if (!catalogRes.ok) return null;
            const catalog: CatalogMetadata = await catalogRes.json();
            //if (catalog.type !== type) return null;

            return {
              inference: { ...inference, modelVersion },
              catalog,
            };
          } catch {
            return null;
          }
        })
    ).then((results) => results.filter(Boolean) as EnrichedMetadata[]);
  }, [trackId]);

  const loadNextPlaylists = useCallback(async () => {
    if (!trackId || isLoadingPlaylists.current || !hasMorePlaylists.current) return;

    isLoadingPlaylists.current = true;
    const nextPage = playlistPage.current + 1;
    setPlaylistPendingCount(PAGE_SIZE);

    try {
      const items = await fetchAndEnrich('playlist', nextPage);

      if (items.length > 0) {
        playlistPage.current = nextPage;
        setPlaylists((prev) => [...prev, ...items]);
      }
      else
      {
        hasMorePlaylists.current = false;
      }

      setPlaylistPendingCount(items.length);
    } finally {
      isLoadingPlaylists.current = false;
      setTimeout(() => setPlaylistPendingCount(0), 300);
    }
  }, [fetchAndEnrich, trackId]);

  const loadNextLabels = useCallback(async () => {
    if (!trackId || isLoadingLabels.current || !hasMoreLabels.current) return;

    isLoadingLabels.current = true;
    const nextPage = labelPage.current + 1;
    setLabelPendingCount(PAGE_SIZE);

    try {
      const items = await fetchAndEnrich("record_label", nextPage);

      if (items.length > 0) {
        labelPage.current = nextPage;
        setLabels((prev) => [...prev, ...items]);
      } else {
        hasMoreLabels.current = false;
      }

      setLabelPendingCount(items.length);
    } finally {
      isLoadingLabels.current = false;
      setTimeout(() => setLabelPendingCount(0), 300);
    }
  }, [fetchAndEnrich, trackId]);

  useEffect(() => { void fetchTrackData(); }, [fetchTrackData]);

  useEffect(() => {
    if (!trackId) return;
    playlistPage.current = 0;
    labelPage.current = 0;
    setPlaylists([]);
    setLabels([]);
    void loadNextPlaylists();
    void loadNextLabels();
  }, [loadNextLabels, loadNextPlaylists, trackId]);

  useEffect(() => {
    const observers: IntersectionObserver[] = [];

    if ((filterType === 'all' || filterType === 'playlists') && playlistLoaderRef.current && hasMorePlaylists.current) {
      const playlistObserver = new IntersectionObserver(([entry]) => {
        if (entry.isIntersecting) void loadNextPlaylists();
      }, { threshold: 0.1 });
      playlistObserver.observe(playlistLoaderRef.current);
      observers.push(playlistObserver);
    }

    if ((filterType === 'all' || filterType === 'labels') && labelLoaderRef.current && hasMoreLabels.current) {
      const labelObserver = new IntersectionObserver(([entry]) => {
        if (entry.isIntersecting) void loadNextLabels();
      }, { threshold: 0.1 });
      labelObserver.observe(labelLoaderRef.current);
      observers.push(labelObserver);
    }

    return () => observers.forEach((o) => o.disconnect());
  }, [filterType, playlists.length, labels.length, loadNextPlaylists, loadNextLabels]);

  const allTags = useMemo(() => {
    const tagsSet = new Set<string>();
    [...playlists, ...labels].forEach(({ catalog }) => {
      catalog.tags?.forEach((t) => tagsSet.add(t));
    });
    return Array.from(tagsSet).sort((a, b) => a.localeCompare(b));
  }, [playlists, labels]);

  function matchByTags(catalogTags?: string[] | null) {
    const include = filters.tags.include;
    const exclude = filters.tags.exclude;
    if (!catalogTags || catalogTags.length === 0) {
      if (include.length > 0) return false;
      return exclude.length === 0;
    }
    if (exclude.length > 0 && catalogTags.some(t => exclude.includes(t))) return false;
    if (include.length > 0 && !catalogTags.some(t => include.includes(t))) return false;
    return true;
  }

  const filteredPlaylists = useMemo(() => {
    return playlists.filter(({ catalog }) => matchByTags(catalog.tags));
  }, [playlists, filters.tags]);

  const filteredLabels = labels;

  const handleDelete = async () => {
    if (!track) return;
    await apiFetch(`${SERVICES.TRACK}/${track.id}`, { method: 'DELETE' });
    toast({ title: `Track "${track.title}" deleted successfully.`, duration: 2000 });
    navigate("../..", { relative: "path" });
  };

  const handleFiltersChange = (newFilters: { type: string[]; tags: { include: string[]; exclude: string[] } }) => {
    setFilters(newFilters);
    if (newFilters.type.includes("playlist") && !newFilters.type.includes("label")) {
      setFilterType("playlists");
    } else if (newFilters.type.includes("label") && !newFilters.type.includes("playlist")) {
      setFilterType("labels");
    } else {
      setFilterType("all");
    }
    hasMorePlaylists.current = true;
    hasMoreLabels.current = true;
  };

  if (loading || !track) return <Loading />;

  return (
      <div className="flex h-[calc(100vh-64px)]">

        {/* Desktop sidebar */}
        <div className="hidden md:block w-[12rem] border-r border-sidebar-border shrink-0 h-full sticky top-0">
          <FilterSidebar onFiltersChange={handleFiltersChange} tags={allTags} />
        </div>

        {/* Overlay */}
        {isOpen && (
            <div
                className="fixed inset-0 z-40 bg-black/50 md:hidden"
                onClick={() => setIsOpen(false)}
            />
        )}

        {/* Mobile slide-in filters */}
        <div
            className={clsx(
                "fixed inset-y-0 left-0 w-64 bg-background border-r border-sidebar-border z-50 p-4",
                "transform transition-transform duration-300 ease-in-out md:hidden",
                isOpen ? "translate-x-0" : "-translate-x-full"
            )}
        >
          <button
              className="absolute top-4 right-4 p-2 rounded-full hover:bg-accent"
              onClick={() => setIsOpen(false)}
              aria-label="Close filters"
          >
            <X className="w-6 h-6" />
          </button>

          <FilterSidebar onFiltersChange={handleFiltersChange} tags={allTags} />
        </div>

        <div className="flex-1 h-[calc(100vh-64px)] overflow-y-auto pb-10">
          <div className="max-w-7xl mx-auto px-4 md:px-6 pt-8 md:pt-12">
            <Link to="/app" className="flex items-center text-muted-foreground hover:text-foreground mb-6">
              <ChevronLeft size={16} /> <span>Back to Dashboard</span>
            </Link>

            <div className="card rounded-lg p-6 mb-10 relative">
              <div className="absolute top-4 right-4">
                <Button variant="ghost" size="sm" onClick={handleDelete} className="text-destructive hover:text-red-600">
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>

              <div className="flex flex-col md:flex-row gap-6 mb-8">
                <div className="md:w-1/3 lg:w-1/4">
                  <div className="aspect-square rounded-md overflow-hidden">
                    <img src={track.imageUrl} alt={track.title} className="w-full h-full object-cover" />
                  </div>
                </div>

                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-2">
                    {isEditing ? (
                        <div className="flex items-center gap-2">
                          <input
                              value={newTitle}
                              onChange={(e) => setNewTitle(e.target.value)}
                              className="bg-transparent border-b border-border focus:border-foreground text-3xl font-bold outline-none px-1 transition-colors"
                              autoFocus
                          />
                          <Button onClick={handleUpdateTitle} size="sm" className="h-7 px-3">Save</Button>
                          <Button onClick={() => { setIsEditing(false); setNewTitle(track.title); }} variant="secondary" size="sm" className="h-7 px-3">Cancel</Button>
                        </div>
                    ) : (
                        <div className="flex items-center gap-2">
                          <h1 className="text-3xl font-bold">{track.title}</h1>
                          <button onClick={() => setIsEditing(true)} className="text-muted-foreground hover:text-foreground transition-colors">
                            <Edit size={18} />
                          </button>
                        </div>
                    )}
                  </div>
                  <p className="text-lg text-muted-foreground mb-4">{artistNames}</p>
                  <TagsBlock tags={track.genre.split(',')} />
                </div>
              </div>

              <div className="bg-background/20 p-4 rounded-md">
                <AudioPlayer trackId={trackId} variant="full" />
              </div>
            </div>

            {/* Sticky recommendations header */}
            <div className="sticky top-0 z-30 bg-background/90 backdrop-blur-md border-b mb-6 px-1 py-3">
              <div className="flex items-center justify-between">
                <div className="flex flex-col">
                  <h2 className="text-2xl font-bold">Recommendations</h2>
                  <p className="text-sm text-muted-foreground">Sorted from the most to the least relevant</p>
                </div>

                <button
                    className="md:hidden p-2 rounded-md hover:bg-accent"
                    onClick={() => setIsOpen(!isOpen)}
                    aria-label={isOpen ? "Close filters" : "Open filters"}
                >
                  {isOpen ? <X className="w-5 h-5" /> : <Filter className="w-5 h-5" />}
                </button>
              </div>
            </div>

            <div className="flex flex-col gap-10">
              {(filteredPlaylists.length === 0 && filteredLabels.length === 0 && playlistPendingCount === 0 && labelPendingCount === 0) && (
                  <span>Nothing's showing up for you right now, but we're constantly adding new opportunities — so check back soon!</span>
              )}

              {(filterType === 'all' || filterType === 'playlists') && (filteredPlaylists.length > 0 || playlistPendingCount > 0) && (
                  <section>
                    <h3 className="text-xl font-bold mb-4 flex items-center">
                      <Filter className="mr-2" size={20} /> Playlists
                    </h3>
                    <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-4">
                      {filteredPlaylists.map(({ catalog, inference }) => (
                          <RecommendationCard
                              key={inference.metadataId}
                              trackId={trackId}
                              catalog={catalog}
                              inferenceMetadata={inference}
                              modelVersion={inference.modelVersion}
                              className="animate-slide-up animate-fade-in"
                          />
                      ))}
                      {playlistPendingCount > 0 &&
                          Array.from({ length: playlistPendingCount }).map((_, i) => (
                              <SkeletonCard key={`playlist-skeleton-${i}`} />
                          ))}
                      <div ref={playlistLoaderRef} className="h-1" />
                    </div>
                  </section>
              )}

              {(filterType === 'all' && filteredPlaylists.length > 0 && filteredLabels.length > 0) && (
                  <hr className="border-muted opacity-50" />
              )}

              {(filterType === 'all' || filterType === 'labels') && (filteredLabels.length > 0 || labelPendingCount > 0) && (
                  <section>
                    <h3 className="text-xl font-bold mb-4 flex items-center">
                      <Filter className="mr-2" size={20} /> Labels
                    </h3>
                    <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-4">
                      {filteredLabels.map(({ catalog, inference }) => (
                          <RecommendationCard
                              key={inference.metadataId}
                              trackId={trackId}
                              catalog={catalog}
                              inferenceMetadata={inference}
                              modelVersion={inference.modelVersion}
                              className="animate-slide-up animate-fade-in"
                          />
                      ))}
                      {labelPendingCount > 0 &&
                          Array.from({ length: labelPendingCount }).map((_, i) => (
                              <SkeletonCard key={`label-skeleton-${i}`} />
                          ))}
                      <div ref={labelLoaderRef} className="h-1" />
                    </div>
                  </section>
              )}
            </div>
          </div>
        </div>
      </div>
  );
};

export default TrackDetails;
