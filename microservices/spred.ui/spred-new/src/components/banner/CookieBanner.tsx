import { useState, useEffect } from 'react';
import { X } from 'lucide-react';

export const CookieBanner = () => {
    const [isVisible, setIsVisible] = useState(false);
    const [isClosing, setIsClosing] = useState(false);

    useEffect(() => {
        const hasAcceptedCookies = localStorage.getItem('cookiesAccepted');
        if (!hasAcceptedCookies) {
            const timeout = setTimeout(() => {
                setIsVisible(true);
            }, 500);

            return () => clearTimeout(timeout)
        }
    }, []);

    const handleAccept = () => {
        setIsClosing(true);
        setTimeout(() => {
            localStorage.setItem('cookiesAccepted', 'true');
            setIsVisible(false);
        }, 300);
    };

    if (!isVisible) return null;

    return (
        <div
            className={`fixed bottom-0 left-0 right-0 bg-white z-50 border-t border-gray-200 p-4 shadow-lg transition-opacity duration-300 ${
                isClosing ? 'opacity-0' : 'opacity-100'
            }`}
        >
            <div className="max-w-7xl mx-auto flex items-start justify-between gap-4 relative">
                <p className="text-sm text-black">
                    We use cookies for essential functionality such as authentication and for analytics (Google Analytics) to improve our service. By continuing to use the site, you consent to this use.
                </p>
                <button
                    onClick={handleAccept}
                    className="p-2 text-gray-500 hover:text-gray-700 transition-colors"
                    aria-label="Close cookie banner"
                >
                    <X className="w-5 h-5" />
                </button>
            </div>
        </div>
    );
};
