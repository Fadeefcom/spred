import { ComponentType } from "react";
import {
    SpotifyIcon,
    AppleMusicIcon,
    DeezerIcon,
    YoutubeMusicIcon,
    SoundCloudIcon,
} from "@/components/icons/StreamingIcons";

export type Platform = "spotify" | "apple-music" | "deezer" | "youtube-music" | "soundcloud";
export type AccountStatus =
    | "PlatformSelect"
    | "Pending"
    | "TokenIssued"
    | "ProofSubmitted"
    | "Verified"
    | "Error"
    | "Deleted";

export const PlatformLabels: Record<Platform, string> = {
    "spotify": "Spotify",
    "apple-music": "Apple Music",
    "deezer": "Deezer",
    "youtube-music": "YouTube Music",
    "soundcloud": "SoundCloud",
};

export const PlatformUrls: Record<Platform, string> = {
    "spotify": "https://open.spotify.com",
    "apple-music": "https://music.apple.com",
    "deezer": "https://www.deezer.com",
    "youtube-music": "https://music.youtube.com",
    "soundcloud": "https://soundcloud.com",
};

export const getPlatformProfileUrl = (platform: Platform, id: string): string => {
    const normalized = platform.toLowerCase() as Platform;
    switch (normalized) {
        case "spotify":
            return `${PlatformUrls.spotify}/user/${encodeURIComponent(id)}`;

        case "apple-music":
            return `${PlatformUrls["apple-music"]}/profile/${encodeURIComponent(id)}`;

        case "deezer":
            return `${PlatformUrls.deezer}/profile/${encodeURIComponent(id)}`;

        case "youtube-music":
            return `${PlatformUrls["youtube-music"]}/channel/${encodeURIComponent(id)}`;

        case "soundcloud":
            return `${PlatformUrls.soundcloud}/${encodeURIComponent(id)}`;

        default:
            return "#";
    }
};

export interface ConnectedAccountType {
    platform: Platform;
    accountId: string;
    status: AccountStatus;
    connectedAt: string;
    profileUrl: string;
}

export interface PlatformInfo {
    id: Platform;
    name: string;
    bgClass: string;
    icon: ComponentType<{ className?: string }>;
    comingSoon?: boolean;
}

export const platforms: PlatformInfo[] = [
    { id: "spotify",        name: "Spotify",        bgClass: "bg-platforms-spotify",        icon: SpotifyIcon },
    { id: "apple-music",    name: "Apple Music",    bgClass: "bg-platforms-apple-music",    icon: AppleMusicIcon },
    { id: "deezer",         name: "Deezer",         bgClass: "bg-platforms-deezer",         icon: DeezerIcon,       comingSoon: true },
    { id: "youtube-music",  name: "YouTube Music",  bgClass: "bg-platforms-youtube-music",  icon: YoutubeMusicIcon },
    { id: "soundcloud",     name: "SoundCloud",     bgClass: "bg-platforms-soundcloud",     icon: SoundCloudIcon },
];

export const getPlatformInfo = (platformId: string) => {
    const normalized = platformId.toLowerCase() as Platform;
    return platforms.find((p) => p.id === normalized);
};