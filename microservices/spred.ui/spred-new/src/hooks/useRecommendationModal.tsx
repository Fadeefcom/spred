import React, { createContext, useContext, useState } from 'react';
import {InferenceMetadata, RecommendationFeedback} from "@/types/InferenceResult.ts";
import {CatalogMetadata} from "@/types/CatalogMetadata.ts";
import {apiFetch} from "@/hooks/apiFetch.tsx";
import {SERVICES} from "@/constants/services.tsx";

interface UpdateRateRequest {
  modelVersion: string;
  isLiked: boolean | null;
  hasApplied: boolean | null;
  wasAccepted: boolean | null;
}

export interface SimilarTrack {
  title: string;
  artist: { name: string }[];
  url: string;
}

type RecommendationFeedbackMap = Record<string, RecommendationFeedback>;

interface RecommendationModalContextType {
  isOpen: boolean;
  activeRecommendation: InferenceMetadata | null;
  activeCatalog: CatalogMetadata | null;
  feedbackMap: RecommendationFeedbackMap;
  openRecommendationModal: (recommendation: InferenceMetadata, catalog: CatalogMetadata, modelVersion: string, trackId: string) => void;
  closeRecommendationModal: () => void;
  setFeedback: (recommendationId: string, feedback: Partial<RecommendationFeedback>) => void;
  sendFeedbackToServer: (recommendationId: string, trackId: string, modelVersion: string, feedback: Partial<RecommendationFeedback>) => void;
  modelVersion: string;
  trackId: string;
  similarCache: Record<string, SimilarTrack>;
  setSimilarTracks: (trackId: string, tracks: SimilarTrack) => void;
}


const RecommendationModalContext = createContext<RecommendationModalContextType | undefined>(undefined);

export const RecommendationModalProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [isOpen, setIsOpen] = useState(false);
  const [activeRecommendation, setActiveRecommendation] = useState<InferenceMetadata | null>(null);
  const [activeCatalog, setActiveCatalog] = useState<CatalogMetadata | null>(null);
  const [feedbackMap, setFeedbackMap] = useState<RecommendationFeedbackMap>({});
  const [modelVersion, setModelVersion] = useState<string | null>(null);
  const [trackId, setTrackId] = useState<string | null>(null);

  const openRecommendationModal = (recommendation: InferenceMetadata, catalog: CatalogMetadata, modelVersion: string, trackId: string) => {
    setActiveRecommendation(recommendation);
    setActiveCatalog(catalog);
    setModelVersion(modelVersion);
    setTrackId(trackId);
    setIsOpen(true);
  };

  const closeRecommendationModal = () => {
    setIsOpen(false);
  };

  const [similarCache, setSimilarCache] = useState<Record<string, SimilarTrack>>({});
  const setSimilarTracks = (trackId: string, track: SimilarTrack) => {
    setSimilarCache(prev => ({
      ...prev,
      [trackId]: track
    }));
  };

  const setFeedback = (recommendationId: string, feedback: Partial<RecommendationFeedback>) => {
    setFeedbackMap(prev => ({
      ...prev,
      [recommendationId]: {
        isLiked: feedback.isLiked !== undefined ? feedback.isLiked : prev[recommendationId]?.isLiked,
        hasApplied: feedback.hasApplied !== undefined ? feedback.hasApplied : prev[recommendationId]?.hasApplied || false,
        wasAccepted: feedback.wasAccepted !== undefined ? feedback.wasAccepted : prev[recommendationId]?.wasAccepted || false,
      }
    }));
  };

  const sendFeedbackToServer = async (recommendationId: string, trackId: string, modelVersion: string, feedback: Partial<RecommendationFeedback>) => {
    const rateRequest: UpdateRateRequest = {
      modelVersion: modelVersion,
      isLiked: feedback?.isLiked,
      hasApplied: feedback?.hasApplied,
      wasAccepted: feedback?.wasAccepted
    };

    try {
      await apiFetch(`${SERVICES.INFERENCE}/rate/${trackId}/${recommendationId}`, {
        method: 'PATCH',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(rateRequest),
      });
    } catch (error) {
      console.error('Error while sending request.', error);
    }
  };

  return (
    <RecommendationModalContext.Provider
      value={{
        isOpen,
        activeRecommendation,
        activeCatalog,
        feedbackMap,
        openRecommendationModal,
        closeRecommendationModal,
        setFeedback,
        sendFeedbackToServer,
        modelVersion,
        trackId,
        similarCache,
        setSimilarTracks
      }}
    >
      {children}
    </RecommendationModalContext.Provider>
  );
};

export const useRecommendationModal = () => {
  const context = useContext(RecommendationModalContext);
  if (context === undefined) {
    throw new Error('useRecommendationModal must be used within a RecommendationModalProvider');
  }
  return context;
};
