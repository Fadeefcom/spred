import { useEffect, useRef } from 'react';
import { useLocation } from 'react-router-dom';

export const AnalyticsProvider = ({ children }: { children: React.ReactNode }) => {
    const location = useLocation();
    const hasScrolled = useRef<{ [k: number]: boolean }>({});

    // Send page_view on route change
    useEffect(() => {
        window.gtag?.('event', 'page_view', {
            page_path: location.pathname,
            page_location: window.location.href,
            page_title: document.title,
        });

        // Reset scroll flags on route change
        hasScrolled.current = {};
    }, [location.pathname]);

    // Scroll depth tracking
    useEffect(() => {
        const onScroll = () => {
            const scrollY = window.scrollY + window.innerHeight;
            const fullHeight = document.documentElement.scrollHeight;
            const percent = Math.round((scrollY / fullHeight) * 100);

            [25, 50, 75, 90].forEach((threshold) => {
                if (percent >= threshold && !hasScrolled.current[threshold]) {
                    hasScrolled.current[threshold] = true;
                    window.gtag?.('event', `scroll_${threshold}`, {
                        page_path: location.pathname,
                    });
                }
            });
        };

        window.addEventListener('scroll', onScroll);
        return () => window.removeEventListener('scroll', onScroll);
    }, [location.pathname]);

    // Engagement timer (30s and 60s)
    useEffect(() => {
        const timers = [30, 60].map((sec) =>
            setTimeout(() => {
                window.gtag?.('event', `engaged_${sec}s`, {
                    page_path: location.pathname,
                });
            }, sec * 1000)
        );

        return () => timers.forEach(clearTimeout);
    }, [location.pathname]);

    return <>{children}</>;
};
