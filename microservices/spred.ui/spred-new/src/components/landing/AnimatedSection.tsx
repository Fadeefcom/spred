import React, { useEffect, useRef } from 'react';
import { cn } from "@/lib/utils";

interface AnimatedSectionProps {
  children: React.ReactNode;
  className?: string;
  animation?: 'fade' | 'fade-up' | 'fade-down' | 'fade-left' | 'fade-right' | 'scale' | 'blur-in';
  delay?: 0 | 200 | 400 | 600 | 800;
  threshold?: number;
  id?: string;
}

const AnimatedSection: React.FC<AnimatedSectionProps> = ({
                                                           children,
                                                           className,
                                                           animation = 'fade-up',
                                                           delay = 0,
                                                           threshold = 0.2,
                                                           id,
                                                         }) => {
  const sectionRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const section = sectionRef.current;
    if (!section) return;

    const observer = new IntersectionObserver(
        (entries) => {
          entries.forEach((entry) => {
            if (entry.isIntersecting) {
              setTimeout(() => {
                section.classList.add('active');
              }, delay);
              observer.unobserve(section);
            }
          });
        },
        { threshold }
    );

    observer.observe(section);

    return () => {
      if (section) observer.unobserve(section);
    };
  }, [delay, threshold]);

  const getAnimationClass = () => {
    switch (animation) {
      case 'fade':
        return 'reveal';
      case 'fade-up':
        return 'reveal-up';
      case 'fade-down':
        return 'reveal-down';
      case 'fade-left':
        return 'reveal-left';
      case 'fade-right':
        return 'reveal-right';
      case 'scale':
        return 'animate-scale opacity-0';
      case 'blur-in':
        return 'animate-blur-in opacity-0';
      default:
        return 'reveal-up';
    }
  };

  return (
      <div
          ref={sectionRef}
          className={cn(getAnimationClass(), `delay-${delay}`, className)}
          id={id}
      >
        {children}
      </div>
  );
};

export default AnimatedSection; 