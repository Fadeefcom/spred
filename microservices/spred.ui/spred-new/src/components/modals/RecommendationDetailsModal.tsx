import React, { useEffect, useRef, useState } from "react";
import { ThumbsUp, ThumbsDown, ExternalLink, ChevronUp, ChevronDown } from "lucide-react";
import { FaSpotify, FaSoundcloud, FaVk, FaTelegram, FaYandex, FaYoutube } from "react-icons/fa";
import { useRecommendationModal } from "@/hooks/useRecommendationModal";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle
} from "@/components/ui/alert-dialog";
import { useToast } from "@/hooks/use-toast";
import { Loading } from "@/components/loading/loading";
import { apiFetch } from "@/hooks/apiFetch";
import { SERVICES } from "@/constants/services";
import { Track } from "@/types/Track";
import { FitBadge } from "@/components/ui/fit-badge";
import { TagsBlock } from "@/components/ui/TagsBlock";
import type { SimilarTrack } from "@/hooks/useRecommendationModal";

const platformIcons: Record<string, JSX.Element> = {
  spotify: <FaSpotify size={20} />,
  soundcloud: <FaSoundcloud size={20} />,
  vk: <FaVk size={20} />,
  yandex: <FaYandex size={20} />,
  telegram: <FaTelegram size={20} />,
  youtube: <FaYoutube size={20} />
};

function ModalHeader({
                       type,
                       score,
                       name,
                       updatedAt,
                       listenUrls
                     }: {
  type: string;
  score: string;
  name: string;
  updatedAt: string | number | Date;
  listenUrls?: Record<string, string>;
}) {
  return (
      <DialogHeader className="px-6 pt-6">
        <div className="flex gap-2 items-center text-[11px] leading-[14px] text-zinc-400 uppercase mb-3 tracking-[0.5px]">
          <span>{type}</span>
          <FitBadge score={score} />
        </div>

        <div className="flex items-center justify-start gap-5 mb-2">
          <DialogTitle className="text-[18px] leading-6 text-zinc-100">{name}</DialogTitle>
        </div>

        <div className="flex items-center justify-between mt-4 text-[11px] text-zinc-400">
        <span>
          Last updated:{" "}
          <span className="text-emerald-500">{new Date(updatedAt).toLocaleDateString()}</span>
        </span>
          <div className="flex items-center gap-3.5">
            <span>Listen on:</span>
            {listenUrls &&
                Object.entries(listenUrls).map(([platform, url]) => (
                    <a
                        key={platform}
                        href={url}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="hover:text-zinc-100 transition-colors"
                    >
                      {platformIcons[platform] ?? <ExternalLink size={16} />}
                    </a>
                ))}
          </div>
        </div>
      </DialogHeader>
  );
}

function CatalogMain({
                       imageUrl,
                       name,
                       description,
                       followers,
                       followerChange,
                       tracksTotal,
                       tags
                     }: {
  imageUrl?: string;
  name: string;
  description?: string;
  followers?: number;
  followerChange?: number;
  tracksTotal?: number;
  tags?: string[];
}) {
  return (
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mt-4 px-6">
        <div className="md:col-span-1 flex">
          <div className="rounded-lg overflow-hidden w-full">
            {imageUrl ? (
                <img
                    src={imageUrl}
                    alt={name}
                    className="w-full aspect-square object-cover bg-zinc-900"
                    loading="lazy"
                />
            ) : (
                <div className="w-full aspect-square bg-zinc-900" />
            )}
          </div>
        </div>

        <div className="md:col-span-2 flex flex-col">
          {description && (
              <div className="relative group">
                <p className="line-clamp-3 text-[12px] leading-5 text-zinc-400">{description}</p>
                <div className="pointer-events-none opacity-0 group-hover:pointer-events-auto group-hover:opacity-100 absolute z-10 left-0 top-[100%] translate-y-2 w-[300px] bg-zinc-950 border border-zinc-800 text-[12px] text-zinc-100 px-3 py-2 rounded shadow-xl transition-opacity duration-200">
                  {description}
                </div>
              </div>
          )}

          <div className="flex flex-wrap items-center gap-2 text-zinc-100 mt-4">
            {typeof followers === "number" && (
                <div className="flex items-center gap-1 relative bg-transparent border border-zinc-800 px-[9px] py-[4px] rounded-[4px] select-none text-sm">
                  {followers.toLocaleString()} followers
                  {typeof followerChange === "number" && followers !== 0 && followerChange !== 0 && (
                      <>
                  <span
                      className={`ml-1 ${
                          followerChange > 0 ? "text-emerald-500" : "text-red-500"
                      }`}
                  >
                    {followerChange > 0 ? `↑${followerChange}` : `↓${Math.abs(followerChange)}`}
                  </span>
                        <div className="absolute -top-1.5 -right-1.5 group">
                          <div className="w-2.5 h-2.5 bg-zinc-400 text-zinc-800 text-[9px] flex items-center justify-center rounded-full border border-zinc-700 select-none">
                            i
                          </div>
                          <div className="absolute left-1/2 -translate-x-1/2 mt-2 hidden group-hover:block bg-zinc-950 text-zinc-100 text-[12px] px-2 py-1 rounded shadow z-10 whitespace-nowrap border border-zinc-800">
                            New followers in the last 30 days
                          </div>
                        </div>
                      </>
                  )}
                </div>
            )}
            {typeof tracksTotal === "number" && (
                <div className="bg-transparent border border-zinc-800 px-[9px] py-[4px] rounded-[4px] select-none text-sm">
                  {tracksTotal} tracks
                </div>
            )}
          </div>

          {tags?.length ? <div className="mt-4"><TagsBlock tags={tags} /></div> : null}
        </div>
      </div>
  );
}

function SimilarTracks({
                         isLoading,
                         items
                       }: {
  isLoading: boolean;
  items: SimilarTrack[];
}) {
  const safe = items.filter(t => {
    try {
      return t.url?.trim() && Boolean(new URL(t.url));
    } catch {
      return false;
    }
  });

  if (!isLoading && safe.length === 0) return null;

  return (
      <div className="mt-5 px-6">
        <p className="text-sm font-medium mb-3">Similar Tracks:</p>
        <div className="space-y-2">
          {isLoading &&
              Array.from({ length: 3 }).map((_, i) => (
                  <div key={i} className="h-[56px] bg-zinc-800 rounded-md animate-pulse" />
              ))}
          {!isLoading &&
              safe.map((track, i) => (
                  <a
                      key={`${track.url}-${i}`}
                      href={track.url}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="flex items-center justify-between p-2 rounded-md bg-zinc-900 hover:bg-zinc-800 transition-colors"
                  >
                    <div>
                      <p className="font-medium text-sm">{track.title}</p>
                      <p className="text-xs text-zinc-400 truncate break-all max-w-[220px]">
                        {track.artist.map(a => a.name).join(", ")}
                      </p>
                    </div>
                    <ExternalLink size={16} className="text-zinc-400" />
                  </a>
              ))}
        </div>
      </div>
  );
}

function FeedbackControls({
                            feedback,
                            onLike
                          }: {
  feedback: { isLiked?: boolean; hasApplied?: boolean; wasAccepted?: boolean | null };
  onLike: (isLiked: boolean) => void;
}) {
  return (
      <div className="px-6 mt-6">
        <p className="font-medium mb-2 text-zinc-100">Was this recommendation relevant to your track?</p>
        <div className="flex gap-3">
          <Button
              variant={feedback.isLiked === true ? "default" : "outline"}
              size="sm"
              onClick={() => onLike(true)}
          >
            <ThumbsUp /> Relevant
          </Button>
          <Button
              variant={feedback.isLiked === false ? "default" : "outline"}
              size="sm"
              onClick={() => onLike(false)}
          >
            <ThumbsDown /> Not Relevant
          </Button>
        </div>
      </div>
  );
}

export function PitchLinks({
                               submitUrls
                           }: {
    submitUrls?: Record<string, string>;
}) {
    const [open, setOpen] = useState(false);

    if (!submitUrls || Object.keys(submitUrls).length === 0) return null;

    const entries = Object.entries(submitUrls);
    const submithubEntries = entries.filter(([key]) =>
        key.toLowerCase().includes("submithub")
    );
    const visibleEntries = submithubEntries.length > 0 ? submithubEntries : entries;

    return (
        <div className="px-6 mt-6">
            <Button onClick={() => setOpen(!open)} className="w-full">
                Pitch Now {open ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
            </Button>

            <div
                className={`overflow-hidden transition-all duration-300 ease-in-out ${
                    open ? "max-h-[500px] opacity-100" : "max-h-0 opacity-0"
                }`}
            >
                <div className="mt-4 space-y-3">
                    {visibleEntries.map(([key, url]) => (
                        <div
                            key={key}
                            className="group relative overflow-hidden rounded-lg border border-zinc-800 bg-zinc-900 hover:bg-zinc-800 transition-all duration-200 hover:shadow-md"
                        >
                            <a
                                href={url}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="flex items-center justify-between p-4 w-full"
                            >
                                <div className="flex items-center gap-3">
                                    <div className="w-10 h-10 rounded-full bg-zinc-800 flex items-center justify-center group-hover:bg-zinc-700 transition-colors">
                                        <ExternalLink size={18} className="text-zinc-200" />
                                    </div>
                                    <div>
                                        <h4 className="font-medium text-zinc-100 text-sm">{key}</h4>
                                        <p className="text-xs text-zinc-400">Submit your track here</p>
                                    </div>
                                </div>
                                <div className="flex items-center gap-2 text-zinc-400 group-hover:text-zinc-100 transition-colors">
                                    <span className="text-xs hidden sm:inline">Open</span>
                                    <ExternalLink size={16} />
                                </div>
                            </a>
                            <div className="absolute inset-0 bg-gradient-to-r from-transparent to-zinc-700/10 opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none" />
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );
}

function PitchStatus({
                       label,
                       hasApplied,
                       wasAccepted,
                       onOpen
                     }: {
  label: string;
  hasApplied?: boolean;
  wasAccepted?: boolean | null;
  onOpen: () => void;
}) {
  return (
      <div className="px-6 mt-6">
        <div className="flex items-center justify-between">
          <div>
            <p className="font-medium text-zinc-100">{label}</p>
            <p className="text-xs text-zinc-400">
              {hasApplied ? (wasAccepted ? "Your track was accepted." : "You've pitched to this opportunity") : "Have you pitched your track here?"}
            </p>
          </div>
          <Button variant="outline" size="sm" onClick={onOpen}>
            {hasApplied ? "Update Status" : "Set Status"}
          </Button>
        </div>
      </div>
  );
}

function PitchStatusDialog({
                             open,
                             onOpenChange,
                             catalogType,
                             catalogName,
                             onConfirm
                           }: {
  open: boolean;
  onOpenChange: (v: boolean) => void;
  catalogType: string;
  catalogName: string;
  onConfirm: () => void;
}) {
  return (
      <AlertDialog open={open} onOpenChange={onOpenChange}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Did you pitch to this {catalogType} already?</AlertDialogTitle>
            <AlertDialogDescription>
              Let us know if you've already submitted your track to {catalogName}.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel className="bg-secondary text-secondary-foreground">No, not yet</AlertDialogCancel>
            <AlertDialogAction onClick={onConfirm} className="bg-primary text-primary-foreground">
              Yes, I did
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
  );
}

function PitchResultDialog({
                             open,
                             onOpenChange,
                             catalogName,
                             onResult
                           }: {
  open: boolean;
  onOpenChange: (v: boolean) => void;
  catalogName: string;
  onResult: (val: boolean | null) => void;
}) {
  return (
      <AlertDialog open={open} onOpenChange={onOpenChange}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Did they accept your track?</AlertDialogTitle>
            <AlertDialogDescription>
              Let us know the outcome of your submission to {catalogName}.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel className="bg-secondary text-secondary-foreground" onClick={() => onResult(null)}>
              Still Waiting
            </AlertDialogCancel>
            <AlertDialogCancel className="bg-secondary text-secondary-foreground" onClick={() => onResult(false)}>
              Declined
            </AlertDialogCancel>
            <AlertDialogAction onClick={() => onResult(true)} className="bg-primary text-primary-foreground">
              Accepted
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
  );
}

const RecommendationDetailsModal = () => {
  const {
    isOpen,
    activeRecommendation,
    activeCatalog,
    closeRecommendationModal,
    feedbackMap,
    setFeedback,
    sendFeedbackToServer,
    trackId,
    modelVersion,
    similarCache,
    setSimilarTracks
  } = useRecommendationModal();

  const { toast } = useToast();
  const [showPitchDialog, setShowPitchDialog] = React.useState(false);
  const [showResultDialog, setShowResultDialog] = React.useState(false);
  const [similarTrackDetails, setSimilarTrackDetails] = React.useState<SimilarTrack[]>([]);
  const [isLoadingSimilar, setIsLoadingSimilar] = React.useState(false);
  const requestId = useRef(0);

  useEffect(() => {
    const fetchAllSimilarTracks = () => {
      if (!activeRecommendation?.similarTracks?.length) return;
      const localRequestId = ++requestId.current;
      setIsLoadingSimilar(true);
      setSimilarTrackDetails([]);
      let completed = 0;
      const total = activeRecommendation.similarTracks.length;

      activeRecommendation.similarTracks.forEach(pair => {
        const handleComplete = () => {
          completed++;
          if (completed === total && localRequestId === requestId.current) {
            setIsLoadingSimilar(false);
          }
        };

        if (similarCache[pair.trackId]) {
          if (localRequestId === requestId.current) {
            setSimilarTrackDetails(prev => [...prev, similarCache[pair.trackId]]);
          }
          handleComplete();
          return;
        }

        apiFetch(`${SERVICES.TRACK}/${activeCatalog.platform}/${pair.trackOwner}/${pair.trackId}`)
            .then(res => res.json())
            .then((track: Track) => ({ title: track.title, artist: track.artists, url: track.trackUrl ?? "" }))
            .then(res => {
              if (res.url?.trim() && localRequestId === requestId.current) {
                setSimilarTracks(pair.trackId, res);
                setSimilarTrackDetails(prev => [...prev, res]);
              }
            })
            .catch(() => {})
            .finally(handleComplete);
      });
    };

    fetchAllSimilarTracks();
  }, [activeRecommendation?.metadataId, activeRecommendation?.similarTracks]);

  if (!activeRecommendation) return null;

  const feedback =
      feedbackMap[activeRecommendation.metadataId] || {
        isLiked: activeRecommendation.reaction.isLiked,
        hasApplied: activeRecommendation.reaction.hasApplied,
        wasAccepted: activeRecommendation.reaction.wasAccepted
      };

  const handleFeedback = (isLiked: boolean) => {
    setFeedback(activeRecommendation.metadataId, { isLiked });
    sendFeedbackToServer(activeRecommendation.metadataId, trackId, modelVersion, { isLiked });
    toast({
      title: "Feedback received",
      description: isLiked ? "We're glad you found this recommendation relevant!" : "We'll improve our recommendations for you.",
      duration: 3000
    });
  };

  const handlePitchStatusUpdate = (hasApplied: boolean) => {
    setFeedback(activeRecommendation.metadataId, { hasApplied });
    sendFeedbackToServer(activeRecommendation.metadataId, trackId, modelVersion, { hasApplied });
    setShowPitchDialog(false);
    if (hasApplied) {
      setShowResultDialog(true);
    }
  };

  const handlePitchResult = (wasAccepted: boolean | null) => {
    setFeedback(activeRecommendation.metadataId, { wasAccepted });
    sendFeedbackToServer(activeRecommendation.metadataId, trackId, modelVersion, { wasAccepted });
    setShowResultDialog(false);
    toast({
      title: wasAccepted === true ? "Congratulations!" : wasAccepted === false ? "Thanks for letting us know" : "Status Updated",
      description: wasAccepted === true ? "Your track was accepted." : wasAccepted === false ? "We'll keep finding new opportunities for you." : "We'll wait to hear back about your submission.",
      duration: 3000
    });
  };

  const getPitchStatusLabel = () => {
    if (feedback.hasApplied) {
      if (feedback.wasAccepted === true) return "Accepted";
      if (feedback.wasAccepted === false) return "Declined";
      return "Pitched";
    }
    return "Pitch Status";
  };

  return activeCatalog ? (
      <Dialog open={isOpen} onOpenChange={(open) => !open && closeRecommendationModal()}>
        <DialogContent
            className="
          bg-zinc-950 text-zinc-100 border-zinc-800
          w-[92vw] sm:max-w-[720px] md:max-w-[860px]
          p-0 overflow-y-auto overscroll-contain
        "
        >
          <div className="pb-8">
            <ModalHeader
                type={activeCatalog.type}
                score={activeRecommendation.score}
                name={activeCatalog.name}
                updatedAt={activeCatalog.updatedAt}
                listenUrls={activeCatalog.listenUrls}
            />

            <CatalogMain
                imageUrl={activeCatalog.imageUrl}
                name={activeCatalog.name}
                description={activeCatalog.description}
                followers={activeCatalog.followers}
                followerChange={activeCatalog.followerChange}
                tracksTotal={activeCatalog.tracksTotal}
                tags={activeCatalog.tags}
            />

            <SimilarTracks isLoading={isLoadingSimilar} items={similarTrackDetails} />

            <FeedbackControls feedback={feedback} onLike={handleFeedback} />

            <PitchStatus
                label={getPitchStatusLabel()}
                hasApplied={feedback.hasApplied}
                wasAccepted={feedback.wasAccepted}
                onOpen={() => (feedback.hasApplied ? setShowResultDialog(true) : setShowPitchDialog(true))}
            />

            <PitchLinks submitUrls={activeCatalog.submitUrls} />
          </div>
        </DialogContent>

        <PitchStatusDialog
            open={showPitchDialog}
            onOpenChange={setShowPitchDialog}
            catalogType={activeCatalog.type}
            catalogName={activeCatalog.name}
            onConfirm={() => handlePitchStatusUpdate(true)}
        />

        <PitchResultDialog
            open={showResultDialog}
            onOpenChange={setShowResultDialog}
            catalogName={activeCatalog.name}
            onResult={handlePitchResult}
        />
      </Dialog>
  ) : (
      <Loading />
  );
};

export default RecommendationDetailsModal;
