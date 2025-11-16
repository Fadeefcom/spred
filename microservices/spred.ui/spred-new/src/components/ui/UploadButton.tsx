import React, { useRef, useState } from 'react';
import { Upload, Image, Check, X } from 'lucide-react';
import { cn } from '@/lib/utils';

interface UploadButtonProps {
    accept?: string;
    multiple?: boolean;
    maxSize?: number; // in MB
    onFilesSelect: (files: File[]) => void;
    className?: string;
    variant?: 'default' | 'compact' | 'avatar';
    disabled?: boolean;
}

export const UploadButton: React.FC<UploadButtonProps> = ({
                                                              accept = 'image/*',
                                                              multiple = false,
                                                              maxSize = 10,
                                                              onFilesSelect,
                                                              className,
                                                              variant = 'default',
                                                              disabled = false,
                                                          }) => {
    const fileInputRef = useRef<HTMLInputElement>(null);
    const [dragActive, setDragActive] = useState(false);
    const [uploading, setUploading] = useState(false);
    const [uploadStatus, setUploadStatus] = useState<'idle' | 'success' | 'error'>('idle');

    const handleDrag = (e: React.DragEvent) => {
        e.preventDefault();
        e.stopPropagation();
        if (e.type === 'dragenter' || e.type === 'dragover') {
            setDragActive(true);
        } else if (e.type === 'dragleave') {
            setDragActive(false);
        }
    };

    const handleDrop = (e: React.DragEvent) => {
        e.preventDefault();
        e.stopPropagation();
        setDragActive(false);

        if (disabled) return;

        const files = Array.from(e.dataTransfer.files);
        handleFiles(files);
    };

    const handleFileInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const files = Array.from(e.target.files || []);
        handleFiles(files);
    };

    const handleFiles = async (files: File[]) => {
        if (files.length === 0) return;

        // Filter files by size
        const validFiles = files.filter(file => {
            const fileSizeMB = file.size / (1024 * 1024);
            return fileSizeMB <= maxSize;
        });

        if (validFiles.length !== files.length) {
            setUploadStatus('error');
            setTimeout(() => setUploadStatus('idle'), 3000);
            return;
        }

        setUploading(true);
        setUploadStatus('idle');

        try {
            // Simulate upload delay for demo
            await new Promise(resolve => setTimeout(resolve, 1000));
            onFilesSelect(validFiles);
            setUploadStatus('success');
            setTimeout(() => setUploadStatus('idle'), 2000);
        } catch (error) {
            setUploadStatus('error');
            setTimeout(() => setUploadStatus('idle'), 3000);
        } finally {
            setUploading(false);
        }
    };

    const openFileDialog = () => {
        if (disabled) return;
        fileInputRef.current?.click();
    };

    const getVariantStyles = () => {
        switch (variant) {
            case 'compact':
                return 'px-4 py-2 text-sm';
            case 'avatar':
                return 'w-24 h-24 rounded-full flex-col text-xs';
            default:
                return 'px-6 py-8 flex-col';
        }
    };

    const getStatusIcon = () => {
        if (uploading) {
            return <Upload className="w-5 h-5 animate-pulse" />;
        }
        if (uploadStatus === 'success') {
            return <Check className="w-5 h-5 text-green-500" />;
        }
        if (uploadStatus === 'error') {
            return <X className="w-5 h-5 text-red-500" />;
        }
        return variant === 'avatar' ? <Image className="w-6 h-6" /> : <Upload className="w-6 h-6" />;
    };

    const getStatusText = () => {
        if (uploading) return 'Uploading...';
        if (uploadStatus === 'success') return 'Success!';
        if (uploadStatus === 'error') return `Max ${maxSize}MB`;
        if (variant === 'avatar') return 'Upload';
        if (variant === 'compact') return 'Choose file';
        return 'Drop files here or click to browse';
    };

    return (
        <div className={cn('relative', className)}>
            <input
                ref={fileInputRef}
                type="file"
                accept={accept}
                multiple={multiple}
                onChange={handleFileInputChange}
                className="hidden"
                disabled={disabled}
            />

            <div
                onClick={openFileDialog}
                onDragEnter={handleDrag}
                onDragLeave={handleDrag}
                onDragOver={handleDrag}
                onDrop={handleDrop}
                className={cn(
                    // Base styles using design system
                    'h-10 group relative flex items-center justify-center gap-3',
                    'bg-upload-bg border-2 border-dashed border-upload-border',
                    'rounded-lg cursor-pointer transition-all duration-300',
                    'hover:bg-upload-bg-hover hover:border-upload-border-hover',
                    'focus:outline-none focus:ring-2 focus:ring-upload focus:ring-offset-2',

                    // Interactive states
                    !disabled && [
                        'hover:shadow-[var(--shadow-upload)]',
                        'active:scale-98 active:shadow-[var(--shadow-upload-hover)]'
                    ],

                    // Drag active state
                    dragActive && [
                        'bg-upload-bg-hover border-upload-border-hover',
                        'shadow-[var(--shadow-upload-hover)] scale-105'
                    ],

                    // Upload status states
                    uploadStatus === 'success' && 'border-green-400 bg-green-50 dark:bg-green-900/20',
                    uploadStatus === 'error' && 'border-red-400 bg-red-50 dark:bg-red-900/20',

                    // Disabled state
                    disabled && 'opacity-50 cursor-not-allowed hover:bg-upload-bg hover:border-upload-border',

                    // Variant-specific styles
                    getVariantStyles()
                )}
            >
                {/* Background gradient effect */}
                <div className="absolute inset-0 rounded-lg bg-gradient-to-br from-transparent to-transparent group-hover:from-upload/5 group-hover:to-upload/10 transition-all duration-300" />

                {/* Content */}
                <div className="relative flex items-center gap-3">
                    <div className={cn(
                        'flex items-center justify-center transition-transform duration-200',
                        'text-upload-text group-hover:text-upload-text-hover',
                        uploading && 'animate-bounce',
                        variant !== 'compact' && 'group-hover:scale-110'
                    )}>
                        {getStatusIcon()}
                    </div>

                    <div className={cn(
                        'font-medium transition-colors duration-200',
                        'text-upload-text group-hover:text-upload-text-hover',
                        variant === 'avatar' && 'hidden'
                    )}>
                        {getStatusText()}
                    </div>
                </div>

                {/* Loading overlay */}
                {uploading && (
                    <div className="absolute inset-0 bg-upload-bg/80 backdrop-blur-sm rounded-lg flex items-center justify-center">
                        <div className="w-6 h-6 border-2 border-upload border-t-transparent rounded-full animate-spin" />
                    </div>
                )}
            </div>

            {/* Helper text */}
            {variant === 'default' && !uploading && uploadStatus === 'idle' && (
                <p className="mt-2 text-xs text-muted-foreground text-center">
                    {accept.includes('image') ? 'PNG, JPG, GIF' : 'Select files'} up to {maxSize}MB
                </p>
            )}
        </div>
    );
};

export default UploadButton;