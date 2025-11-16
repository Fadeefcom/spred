export interface CatalogMetadata {
    id: string;
    type?: string;
    name?: string;
    description?: string;
    tracksTotal?: number;
    followers?: number;
    isPublic: boolean;
    collaborative: boolean;
    tracks?: string[];
    tags?: string[];
    listenUrls: Record<string, string>;
    submitUrls: Record<string, string>;
    submitEmail?: string;
    imageUrl?: string;
    tracksHref?: string;
    createdAt?: string;
    updatedAt: string;
    followerChange: number;
    platform: string;
}

export interface CatalogStats {
    playlistId: string;
    totalSubmissions: number;
    pendingSubmissions: number;
    acceptedSubmissions: number;
    declinedSubmissions: number;
    weeklySubmissions: number;
    monthlySubmissions: number;
}