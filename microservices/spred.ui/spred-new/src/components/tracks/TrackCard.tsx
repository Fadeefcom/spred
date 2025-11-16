import React, {useState, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import {ExternalLink, Clock, Trash2} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Track } from "@/types/Track.ts";
import { formatDistanceToNow } from 'date-fns';
import {AudioPlayer} from "@/components/player/AudioPlayer.tsx";
import { Trash } from 'lucide-react';
import {apiFetch} from "@/hooks/apiFetch.tsx";
import {SERVICES} from "@/constants/services.tsx";
import {useToast} from "@/hooks/use-toast.ts";

interface TrackCardProps {
  track: Track;
  onDeleted?: (id: string) => void;
}

const TrackCard: React.FC<TrackCardProps> = ({ track, onDeleted }) => {
  const { toast } = useToast();
  const [isPlaying, setIsPlaying] = useState(false);
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const navigate = useNavigate();
  const artistNames = track?.artists.map((artist) => artist.name).join(', ');
  
  // Format the upload date
  const formattedDate = formatDistanceToNow(new Date(track.updateAt), { addSuffix: true });
  
  const handleViewRecommendations = () => {
    navigate(`tracks/${track.id}`)
  };

  const handleDeleteTrack = async () => {
    await apiFetch(`${SERVICES.TRACK}/${track.id}`, {
      method: "DELETE",
      headers: { 'Content-Type': 'application/json' },
    });
    toast({
      title:`Track "${track.title}" deleted successfully.`,
      description:`Track ID: ${track.id}`,
      duration:2000
    });
    onDeleted?.(track.id);
  };


  return (
    <Card onClick={handleViewRecommendations} className="group transition-shadow hover:shadow-lg hover:shadow-spred-yellowdark/30
        overflow-hidden card hover:border-border/80 transition-all">
      <div className="aspect-[3/2] relative overflow-hidden">
        <img 
          src={track.imageUrl}
          alt={track.title}
          className="object-cover w-full h-full" 
        />
        <div className="absolute inset-0 bg-gradient-to-t from-black/70 to-transparent" />
        
        <div className="absolute bottom-3 left-3 right-3 flex justify-between items-center">
          <div className="flex items-center">
            <AudioPlayer trackId={track.id}/>
          </div>
          
          <div className="flex items-center text-white/80 text-xs">
            <Clock size={14} className="mr-1" />
            {formattedDate}
          </div>
        </div>
        <div className="absolute top-2 right-2">
          <Button
              variant="ghost"
              size="sm"
              onClick={(e) => {
                e.stopPropagation();
                handleDeleteTrack();
              }}
              className="text-destructive hover:text-red-600"
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        </div>
      </div>
      
      <CardContent className="p-4">
        <div className="mb-3">
          <h3 className="font-bold text-lg">{track.title}</h3>
          <p className="text-sm text-muted-foreground">{artistNames}</p>
        </div>
        
        {/*<div className="mb-4">*/}
        {/*  <div className="text-sm font-medium mb-1">Genres:</div>*/}
        {/*  <div className="flex flex-wrap gap-1">*/}
        {/*    {track.analysis.genres.map((genre, index) => (*/}
        {/*      <Badge key={index} variant="secondary" className="bg-accent/50 text-xs">*/}
        {/*        {genre}*/}
        {/*      </Badge>*/}
        {/*    ))}*/}
        {/*  </div>*/}
        {/*</div>*/}
        
        {/*<div className="grid grid-cols-2 gap-2 mb-4">*/}
        {/*  <div className="bg-accent/50 p-2 rounded text-sm">*/}
        {/*    <span className="text-xs text-muted-foreground block">Tempo</span>*/}
        {/*    <span>{track.analysis.tempo} BPM</span>*/}
        {/*  </div>*/}
        {/*  <div className="bg-accent/50 p-2 rounded text-sm">*/}
        {/*    <span className="text-xs text-muted-foreground block">Key</span>*/}
        {/*    <span>{track.analysis.key}</span>*/}
        {/*  </div>*/}
        {/*</div>*/}
        
        <Button 
          onClick={handleViewRecommendations} 
          className="w-full bg-spred-yellow hover:bg-spred-yellow/90 text-black"
        >
          View Recommendations
          <ExternalLink size={16} className="ml-1" />
        </Button>
      </CardContent>
    </Card>
  );
};

export default TrackCard;
