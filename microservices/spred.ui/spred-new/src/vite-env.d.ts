/// <reference types="vite/client" />
interface Window {
    gtag?: (
        command: 'config' | 'event' | 'set',
        targetIdOrEventName: string,
        params?: Record<string, unknown>
    ) => void;
}