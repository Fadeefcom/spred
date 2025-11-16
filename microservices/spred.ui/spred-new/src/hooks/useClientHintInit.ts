import { useEffect } from 'react';

export function useClientHintInit() {
    useEffect(() => {
        const KEY = 'clientHintInitDone';

        if (sessionStorage.getItem(KEY)) return;

        fetch('/init', { method: 'GET' })
            .then(() => {
                sessionStorage.setItem(KEY, '1');
            })
            .catch((err) => {
                console.warn('Failed to init client hints:', err);
            });
    }, []);
}