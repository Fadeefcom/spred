import React from 'react';
import { cn } from "@/lib/utils.ts";

interface CardFeatureProps {
  icon: React.ReactNode;
  title: string;
  description: string;
  className?: string;
  style?: React.CSSProperties;
}

const CardFeature: React.FC<CardFeatureProps> = ({ 
  icon, 
  title, 
  description,
  className,
  style
}) => {
  return (
    <div 
      className={cn(
        "p-8 border border-border rounded-lg bg-transparent transition-all hover:bg-muted/5 animate-fade-in", 
        className
      )}
      style={style}
    >
      <div className="feature-icon mb-6">
        {icon}
      </div>
      <h3 className="text-xl font-bold mb-4">{title}</h3>
      <p className="text-muted-foreground leading-relaxed">{description}</p>
    </div>
  );
};

export default CardFeature;
