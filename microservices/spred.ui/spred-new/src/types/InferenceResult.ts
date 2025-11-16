export interface InferenceResult {
    id: string;
    modelInfo: string;
    metadata: InferenceMetadata[];
}

export interface RecommendationFeedback {
    isLiked: boolean | null;
    hasApplied: boolean | null;
    wasAccepted: boolean | null;
}

export interface InferenceMetadata {
    metadataId: string;
    metadataOwner: string;
    score: string;
    reaction: RecommendationFeedback;
    similarTracks: TrackUserPair[];
    modelVersion: string;
}

export interface TrackUserPair
{
    trackId:string,
    trackOwner: string
}