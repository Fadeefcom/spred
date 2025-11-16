import React from 'react';
import { ExternalLink } from 'lucide-react';
import { cn } from "@/lib/utils.ts";
import { useRecommendationModal } from "../../hooks/useRecommendationModal.tsx";
import {InferenceMetadata} from "@/types/InferenceResult.ts";
import {CatalogMetadata} from "@/types/CatalogMetadata.ts";
import {Loading} from "@/components/loading/loading.tsx";
import {FitBadge} from "@/components/ui/fit-badge.tsx";
import {Button} from "@/components/ui/button.tsx";

interface RecommendationCardProps {
  inferenceMetadata: InferenceMetadata;
  trackId: string;
  modelVersion: string;
  className?: string;
  style?: React.CSSProperties;
  catalog: CatalogMetadata;
}

const RecommendationCard: React.FC<RecommendationCardProps> = ({
  inferenceMetadata,
  trackId,
  modelVersion,
  className,
  style,
  catalog,
}) => {
  const { openRecommendationModal } = useRecommendationModal();

  const handleCardClick = () => {
    openRecommendationModal(inferenceMetadata, catalog, modelVersion, trackId);
  };

  const submitEntry = catalog?.submitUrls && Object.entries(catalog.submitUrls)[0];
  const submitUrl = submitEntry?.[1];
  const submitEmail = catalog?.submitEmail;

  return catalog ? (
      <div
          className={cn(
              "backdrop-blur-sm border rounded-lg overflow-hidden transition-all card-hover max-w-[240px] cursor-pointer" +
              "card",
              className
          )}
          style={style}
          onClick={handleCardClick}
      >
        <div className="aspect-square w-full overflow-hidden relative max-h-[180px]">
          <img
              src={catalog?.imageUrl}
              alt={catalog?.name}
              className="w-full h-full object-cover transition-transform duration-500 hover:scale-105"
          />
          <FitBadge score={inferenceMetadata.score} className="absolute top-2 right-2" />
        </div>

        <div className="p-3">
          <div className="flex items-center justify-between mb-1">
            <span className="text-xs uppercase text-gray-400">{catalog?.type}</span>
          </div>

          <h3 className="font-bold text-base mb-2 truncate">{catalog?.name}</h3>

          <div className="flex items-center justify-between mb-3 text-xs text-gray-300">
            {catalog?.type === "playlist" && catalog?.tracksTotal && (
                <span>{catalog?.tracksTotal} tracks</span>
            )}
            {catalog?.type === "label" && catalog?.followers && (
                <span>{catalog?.followers.toLocaleString()} followers</span>
            )}
          </div>

          <Button className="w-full">See more</Button>
        </div>
      </div>
  ) : (
      <div
      className={cn(
          "backdrop-blur-sm border rounded-lg overflow-hidden transition-all max-w-[240px] h-[320px] card-hover cursor-pointer",
          "card"
      )}
      style={style}>
        <Loading />
      </div>);
}

export default RecommendationCard;
