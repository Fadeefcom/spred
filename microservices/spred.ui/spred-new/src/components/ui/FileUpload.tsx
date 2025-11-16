import React, { useState, useRef } from 'react';
import { Upload, Check, AlertCircle, Crown } from 'lucide-react';
import { toast } from 'sonner';
import { Progress } from './progress';
import { cn } from "@/lib/utils.ts";
import { useUploadLimit } from "@/hooks/useUploadLimit.tsx";
import { UsageCounter } from '../banner/UsageCounter';
import { useAuth } from "@/components/authorization/AuthProvider.tsx";
import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button.tsx";

interface FileUploadProps {
  onFileSelected: (file: File) => void;
  accept?: string;
  maxSize?: number;
  className?: string;
  isUploading?: boolean;
  isAnalyzing?: boolean;
  analyzeProgress?: number;
}

const FileUpload: React.FC<FileUploadProps> = ({
                                                 onFileSelected,
                                                 accept = '.mp3, .wav, .flac',
                                                 maxSize = 100,
                                                 className,
                                                 isUploading = false,
                                                 isAnalyzing = false,
                                                 analyzeProgress = 0,
                                               }) => {
  const [isDragging, setIsDragging] = useState(false);
  const [file, setFile] = useState<File | null>(null);
  const [error, setError] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const { user } = useAuth();
  const { limit, used, reset } = useUploadLimit();

  const isPremium = user?.subscription?.isActive;
  const atLimit = used >= limit && !isPremium;

  const handleDragOver = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(true);
  };

  const handleDragLeave = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(false);
  };

  const handleDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(false);
    const droppedFile = e.dataTransfer.files?.[0];
    if (droppedFile && validateFile(droppedFile)) {
      setFile(droppedFile);
      onFileSelected(droppedFile);
      toast.success(`File "${droppedFile.name}" uploaded successfully.`);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selected = e.target.files?.[0];
    if (selected && validateFile(selected)) {
      setFile(selected);
      onFileSelected(selected);
      toast.success(`File "${selected.name}" uploaded successfully.`);
    }
  };

  const handleButtonClick = () => {
    if (inputRef.current) {
      inputRef.current.value = '';
      inputRef.current.click();
    }
  };

  const validateFile = (file: File): boolean => {
    if (!file.type.startsWith('audio/')) {
      setError('Please upload an audio file.');
      return false;
    }
    if (file.size > maxSize * 1024 * 1024) {
      setError(`File size must be less than ${maxSize}MB.`);
      return false;
    }
    setError(null);
    return true;
  };

  const renderUsage = !isPremium && (
      <div className="mt-6 pt-6 border-t border-gray-700/50 w-full max-w-md">
        <UsageCounter current={used} limit={limit} resetAt={reset} label="Weekly uploads" />
      </div>
  );

  const renderIcon = (variant: 'upload' | 'check' | 'error') => {
    const config = {
      upload: { icon: <Upload className="w-8 h-8 text-spred-white" />, bg: "bg-spred-yellow/80" },
      check: { icon: <Check className="w-8 h-8 text-green-500" />, bg: "bg-green-500/20" },
      error: { icon: <AlertCircle className="w-8 h-8 text-red-500" />, bg: "bg-red-500/20" }
    }[variant];

    return (
        <div className={`w-16 h-16 ${config.bg} rounded-full flex items-center justify-center mb-4`}>
          {config.icon}
        </div>
    );
  };

  const renderUpgradePrompt = () => (
      <div className="flex flex-col items-center justify-center space-y-4">
        {renderIcon('error')}
        <p className="text-lg font-medium text-red-400">Upload limit reached</p>
        <p className="text-sm text-gray-400">Upgrade to continue uploading your tracks</p>
        <Link to="/artist/upgrade" className="w-full max-w-xs">
          <Button size="sm" className="w-full text-sm flex items-center justify-center gap-2">
            <Crown className="w-4 h-4" />
            Upgrade to Pro
          </Button>
        </Link>
      </div>
  );

  const renderUploading = () => (
      <div className="flex flex-col items-center justify-center space-y-4">
        <div className="animate-pulse text-gray-300">
          {isUploading ? 'Uploading your track...' : 'Analyzing your track...'}
        </div>
        <Progress value={analyzeProgress} className="w-full h-2" />
        <p className="text-sm text-gray-400">Please wait while our AI analyzes your track</p>
      </div>
  );

  const renderUploaded = () => (
      <div className="flex flex-col items-center justify-center space-y-4">
        {renderIcon('check')}
        <div className="text-center space-y-2">
          <p className="text-lg font-medium text-green-400">Track uploaded successfully!</p>
          <p className="text-sm text-gray-400">
            Your track is being analyzed and will appear in your library.
          </p>
        </div>

        {!isPremium && used >= limit && (
            <div className="mt-6 space-y-3 w-full max-w-xs text-center">
              <p className="text-sm text-gray-400">Unlock unlimited uploads</p>
              <Link to="/artist/upgrade">
                <Button size="sm" className="w-full text-sm flex items-center justify-center gap-2">
                  <Crown className="w-4 h-4" />
                  Upgrade to Pro
                </Button>
              </Link>
            </div>
        )}

        <div className="mt-8 w-full flex justify-center">{renderUsage}</div>
      </div>
  );

  const renderDropArea = () => (
      <div className="flex flex-col items-center justify-center">
        {error ? renderIcon('error') : renderIcon('upload')}
        {error ? (
            <p className="text-red-500 font-medium mb-2">{error}</p>
        ) : (
            <>
              <p className="text-lg font-medium mb-2">
                Drag and drop your track here or click to browse
              </p>
              <p className="text-sm text-gray-400">
                Supported formats: MP3, WAV, FLAC (max {maxSize}MB)
              </p>
            </>
        )}
        {renderUsage}
      </div>
  );

  const content = (() => {
    if (atLimit && !isUploading && !isAnalyzing && !file) return renderUpgradePrompt();
    if (isUploading || isAnalyzing) return renderUploading();
    if (file && !error) return renderUploaded();
    return renderDropArea();
  })();

  return (
      <div className={className}>
        <div
            className={cn(
                "border-2 border-dashed rounded-lg p-8 text-center transition-all cursor-pointer",
                isDragging ? "border-spred-yellow bg-white/5" : "border-gray-600 hover:border-gray-400",
                error ? "border-red-500 bg-red-500/5" : "",
                isAnalyzing ? "pointer-events-none" : ""
            )}
            onDragOver={handleDragOver}
            onDragLeave={handleDragLeave}
            onDrop={handleDrop}
            onClick={handleButtonClick}
        >
          <input
              type="file"
              ref={inputRef}
              accept={accept}
              onChange={handleFileChange}
              className="hidden"
          />
          {content}
        </div>
      </div>
  );
};

export default FileUpload;
