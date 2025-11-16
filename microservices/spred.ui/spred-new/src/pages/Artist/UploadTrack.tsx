import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useToast } from '@/hooks/use-toast.ts';
import FileUpload from '@/components/ui/FileUpload.tsx';
import { Button } from '@/components/ui/button.tsx';
import { TrackCreate } from '@/types/Track.ts';
import {apiFetch} from "@/hooks/apiFetch.tsx";
import {SERVICES} from "@/constants/services.tsx";
import {PATH} from '@/constants/paths.ts';
import {useUploadLimit} from "@/hooks/useUploadLimit.tsx";

interface TrackAnalysis {
  genres: string[];
  moods: string[];
  tempo: number;
  key: string;
  vibe: string;
  similarArtists: string[];
}

const UploadTrack: React.FC = () => {
  const [fileId, setFileId] = useState<string | null>(null);
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [analyzeProgress, setAnalyzeProgress] = useState(0);
  const [analysis, setAnalysis] = useState<TrackAnalysis | null>(null);
  const navigate = useNavigate();
  const { toast } = useToast();
  const { updateFromHeaders } = useUploadLimit();

  useEffect(() => {
    if (fileId && isAnalyzing) {
      const interval = setInterval(async () => {
        try {
          const res = await apiFetch(`${SERVICES.INFERENCE}/status/${fileId}`, {
            method: 'GET',
          });

          const data = await res.json();
          const status = data.status;

          if (status === 'completed') {
            clearInterval(interval);
            setAnalyzeProgress(100);
            setAnalysis(data.analysis);
            setIsAnalyzing(false);
            toast({
              title:'Track analysis complete!',
              description:'',
              duration:2000
            });
          } else if (status === 'failed') {
            clearInterval(interval);
            toast({
              title:'Track analysis failed.',
              description:'',
              duration:2000
            });
            setIsAnalyzing(false);
          } else {
            setAnalyzeProgress((prev) => Math.min(prev + 10, 95));
          }
        } catch (error) {
          clearInterval(interval);
          setIsAnalyzing(false);
        }
      }, 1500);

      return () => clearInterval(interval);
    }
  }, [fileId, isAnalyzing]);

  const handleFileSelected = (selectedFile: File, updateFromHeaders: (headers: Headers) => void) => {
    const trackCreate: TrackCreate = {
      Title: selectedFile.name.split('.')[0],
      Description: '',
      TrackUrl: '',
    };

    const jsonData = JSON.stringify(trackCreate);
    const encodedData = btoa(String.fromCharCode(...new TextEncoder().encode(jsonData)));
    const formData = new FormData();
    formData.append('file', selectedFile);

    setAnalysis(null);
    setIsUploading(true);
    setAnalyzeProgress(0);

    const uploadInterval = setInterval(() => {
      setAnalyzeProgress((prev) => {
        const increment = Math.floor(Math.random() * 5) + 8;
        const next = Math.min(prev + increment, 60);
        if (next >= 60) clearInterval(uploadInterval);
        return next;
      });
    }, 500);

    apiFetch(SERVICES.TRACK, {
      method: 'POST',
      body: formData,
      headers: {
        'X-JSON-Data': encodedData,
      },
    })
        .then(async (res: Response) => {
          updateFromHeaders(res.headers);
          if (!res.ok) throw new Error();
          const idRaw = await res.json();
          setFileId(idRaw.id);
          window.gtag?.('event', 'track_uploaded', {
            track_id: idRaw.id,
            filename: selectedFile.name,
            page_path: window.location.pathname,
          });
          setIsAnalyzing(true);

          const analyzeInterval = setInterval(() => {
            setAnalyzeProgress((prev) => {
              const increment = Math.floor(Math.random() * 5) + 8;
              const next = Math.min(prev + increment, 95);
              if (next >= 95) clearInterval(analyzeInterval);
              return next;
            });
          }, 500);
        })
        .catch(() => {
          toast({
            title: 'Track upload failed.',
            description: '',
            duration: 2000,
          });
        })
        .finally(() => {
          clearInterval(uploadInterval);
          setIsUploading(false);
        });
  };
  
  const handleFindOpportunities = () => {
    navigate(`/artist/tracks/${fileId}`)
  };

  return (
    <div className="min-h-[calc(100vh-128px)] bg-background text-foreground">
      <div className="max-w-3xl mx-auto px-6 pt-12">
        <div className="animate-fade-in">
          <h1 className="text-4xl font-bold mb-2">Upload Your Track</h1>
          <p className="text-muted-foreground mb-6">Our AI will analyze your music and find the perfect opportunities for you.</p>
          
          <div className="bg-accent/50 p-8 mb-6">
            <FileUpload
              onFileSelected={(file) => handleFileSelected(file, updateFromHeaders)}
              isUploading={isUploading}
              isAnalyzing={isAnalyzing}
              analyzeProgress={analyzeProgress}
            />
          </div>

          <div className="mt-6 text-center text-sm text-muted-foreground">
            To learn how we use your uploaded audios — check{" "}
            <span className="italic">“Music Uploads”</span> section in our{" "}
            <a
                href={PATH.PRIVACY_POLICY}
                className="underline underline-offset-4 hover:text-spred-yellowdark transition-colors"
            >
              Privacy Policy
            </a>{" "}
            document
          </div>

          {!isUploading && !isAnalyzing && fileId && (
            <div className="mt-6 text-center">
              <Button
                  className="bg-spred-yellow text-black hover:bg-spred-yellow/90 font-medium py-6 px-8 text-lg"
                  onClick={handleFindOpportunities}
              >
                Find Matching Opportunities
              </Button>
            </div>
          )}

          {/*{analysis && !isAnalyzing && (*/}
          {/*    <div className="animate-slide-up">*/}
          {/*      <div className="bg-accent/50 p-8 mb-8">*/}
          {/*        <div className="flex items-center justify-center mb-6">*/}
          {/*          <div className="w-12 h-12 bg-green-500/20 rounded-full flex items-center justify-center">*/}
          {/*          <Check className="w-6 h-6 text-green-500" />*/}
          {/*        </div>*/}
          {/*        <div className="ml-4">*/}
          {/*          <h3 className="text-xl font-bold">Analysis Complete</h3>*/}
          {/*          <p className="text-muted-foreground">Here's what we found in your track</p>*/}
          {/*        </div>*/}
          {/*      </div>*/}
          {/*      */}
          {/*      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">*/}
          {/*        <div>*/}
          {/*          <h4 className="flex items-center text-lg font-medium mb-3">*/}
          {/*            <Tag className="mr-2" size={18} />*/}
          {/*            Genres*/}
          {/*          </h4>*/}
          {/*          <div className="flex flex-wrap gap-2">*/}
          {/*            {analysis.genres.map(genre => (*/}
          {/*              <Badge key={genre} variant="secondary" className="bg-accent">*/}
          {/*                {genre}*/}
          {/*              </Badge>*/}
          {/*            ))}*/}
          {/*          </div>*/}
          {/*        </div>*/}
          {/*        */}
          {/*        <div>*/}
          {/*          <h4 className="flex items-center text-lg font-medium mb-3">*/}
          {/*            <Music className="mr-2" size={18} />*/}
          {/*            Moods*/}
          {/*          </h4>*/}
          {/*          <div className="flex flex-wrap gap-2">*/}
          {/*            {analysis.moods.map(mood => (*/}
          {/*              <Badge key={mood} variant="secondary" className="bg-accent">*/}
          {/*                {mood}*/}
          {/*              </Badge>*/}
          {/*            ))}*/}
          {/*          </div>*/}
          {/*        </div>*/}
          {/*        */}
          {/*        <div className="md:col-span-2">*/}
          {/*          <h4 className="text-lg font-medium mb-3">Technical Details</h4>*/}
          {/*          <div className="grid grid-cols-2 gap-4">*/}
          {/*            <div className="bg-accent p-3 rounded-md">*/}
          {/*              <p className="text-sm text-muted-foreground">Tempo</p>*/}
          {/*              <p className="font-medium">{analysis.tempo} BPM</p>*/}
          {/*            </div>*/}
          {/*            <div className="bg-accent p-3 rounded-md">*/}
          {/*              <p className="text-sm text-muted-foreground">Key</p>*/}
          {/*              <p className="font-medium">{analysis.key}</p>*/}
          {/*            </div>*/}
          {/*          </div>*/}
          {/*        </div>*/}
          {/*        */}
          {/*        <div className="md:col-span-2">*/}
          {/*          <h4 className="text-lg font-medium mb-3">Overall Vibe</h4>*/}
          {/*          <div className="bg-accent p-4 rounded-md">*/}
          {/*            <p className="text-spred-yellow font-medium">{analysis.vibe}</p>*/}
          {/*          </div>*/}
          {/*        </div>*/}
          {/*        */}
          {/*        <div className="md:col-span-2">*/}
          {/*          <h4 className="text-lg font-medium mb-3">Similar Artists</h4>*/}
          {/*          <div className="flex flex-wrap gap-2">*/}
          {/*            {analysis.similarArtists.map(artist => (*/}
          {/*              <Badge key={artist} variant="outline" className="border-muted">*/}
          {/*                {artist}*/}
          {/*              </Badge>*/}
          {/*            ))}*/}
          {/*          </div>*/}
          {/*        </div>*/}
          {/*      </div>*/}
          {/*      */}

          {/*    </div>*/}
          {/*  </div>*/}
          {/*)}*/}
        </div>
      </div>
    </div>
  );
};

export default UploadTrack;
