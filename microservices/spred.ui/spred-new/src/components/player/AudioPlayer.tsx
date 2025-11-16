import React, { useEffect, useRef, useState, useId } from "react";
import { Play, Pause } from "lucide-react";
import { useAudioPlayerContext } from "./AudioPlayerContext";
import { apiFetch } from "@/hooks/apiFetch";
import {SERVICES} from "@/constants/services.tsx";

type Props = {
    trackId: string;
    variant?: "mini" | "full";
};

export const AudioPlayer: React.FC<Props> = ({ trackId, variant = "mini" }) => {
    const audioRef = useRef<HTMLAudioElement>(null);
    const [audioSrc, setAudioSrc] = useState<string | null>(null);
    const [isPlaying, setIsPlaying] = useState(false);
    const [currentTime, setCurrentTime] = useState(0);
    const [duration, setDuration] = useState(0);
    const progressRef = useRef<HTMLDivElement>(null);
    const [isSeeking, setIsSeeking] = useState(false);

    const { register, setActive } = useAudioPlayerContext();
    const id = useId();

    useEffect(() => {
        register(id, {
            pause: () => {
                audioRef.current?.pause();
                setIsPlaying(false);
            },
        });

        return () => {
            if (audioSrc) {
                URL.revokeObjectURL(audioSrc);
            }
        };
    }, [audioSrc, register]);

    const updateSeek = (clientX: number) => {
        if (!audioRef.current || !duration || !progressRef.current) return;
        const rect = progressRef.current.getBoundingClientRect();
        const ratio = (clientX - rect.left) / rect.width;
        const clamped = Math.max(0, Math.min(1, ratio));
        const newTime = clamped * duration;
        audioRef.current.currentTime = newTime;
        setCurrentTime(newTime);
    };

    useEffect(() => {
        const handleMouseMove = (e: MouseEvent) => {
            if (isSeeking) updateSeek(e.clientX);
        };

        const handleMouseUp = () => {
            if (isSeeking) {
                setIsSeeking(false);
            }
        };

        window.addEventListener("mousemove", handleMouseMove);
        window.addEventListener("mouseup", handleMouseUp);

        return () => {
            window.removeEventListener("mousemove", handleMouseMove);
            window.removeEventListener("mouseup", handleMouseUp);
        };
    }, [isSeeking, duration]);

    const handlePlayToggle = async () => {
        if (!audioRef.current) return;

        setActive(id);

        if (!audioSrc) {
            try {
                const res = await apiFetch(`${SERVICES.TRACK}/audio/${trackId}`, {
                    method: "GET",
                });
                const blob = await res.blob();
                const url = URL.createObjectURL(blob);
                setAudioSrc(url);

                setTimeout(() => {
                    if (audioRef.current) {
                        void audioRef.current.play();
                        setIsPlaying(true);
                    }
                }, 0);
            } catch (err) {
                console.error("Failed to load audio:", err);
            }
        } else {
            if (isPlaying) {
                audioRef.current.pause();
                setIsPlaying(false);
            } else {
                void audioRef.current.play();
                setIsPlaying(true);
            }
        }
    };

    const handleTimeUpdate = () => {
        if (audioRef.current) {
            setCurrentTime(audioRef.current.currentTime);
        }
    };

    const handleLoadedMetadata = () => {
        if (audioRef.current) {
            setDuration(audioRef.current.duration);
        }
    };

    const handleProgressClick = (e: React.MouseEvent<HTMLDivElement, MouseEvent>) => {
        if (!audioRef.current || !duration) return;
        const rect = e.currentTarget.getBoundingClientRect();
        const ratio = (e.clientX - rect.left) / rect.width;
        audioRef.current.currentTime = ratio * duration;
        setCurrentTime(audioRef.current.currentTime);
    };

    const formatTime = (time: number): string => {
        const minutes = Math.floor(time / 60);
        const seconds = Math.floor(time % 60);
        return `${minutes}:${seconds.toString().padStart(2, "0")}`;
    };

    return (
        <div className="flex flex-col gap-2">
            <div className="flex items-center gap-2">
                <button
                    onClick={handlePlayToggle}
                    className="bg-spred-yellow text-black rounded-full p-3 hover:bg-spred-yellow/90 transition-all"
                >
                    {isPlaying ? <Pause size={16} /> : <Play size={16} />}
                </button>

                {variant === "full" && (
                    <div className="w-full">
                      <div className="flex justify-between text-sm mb-1">
                        <span>{formatTime(currentTime)}</span>
                        <span>{formatTime(duration)}</span>
                      </div>
                      <div className="w-full bg-background/30 rounded-full h-1.5 overflow-hidden">
                          <div
                              ref={progressRef}
                              className="relative w-full bg-background/30 rounded-full h-1.5 overflow-hidden cursor-pointer"
                              onMouseDown={(e) => {
                                  setIsSeeking(true);
                                  updateSeek(e.clientX);
                              }}
                          >
                              <div
                                  className="absolute top-0 left-0 h-full bg-spred-yellow"
                                  style={{width: `${(currentTime / duration) * 100}%`}}
                              />
                              {/* Кружочек-ползунок */}
                              <div
                                  className="absolute top-1/2 -translate-y-1/2 w-3 h-3 rounded-full bg-spred-yellow"
                                  style={{left: `calc(${(currentTime / duration) * 100}% - 6px)`}}
                              />
                          </div>
                      </div>
                    </div>
                )}
            </div>

            <audio
                ref={audioRef}
                src={audioSrc ?? undefined}
                onLoadedMetadata={handleLoadedMetadata}
                onTimeUpdate={handleTimeUpdate}
                onEnded={() => setIsPlaying(false)}
            />
        </div>
    );
};
