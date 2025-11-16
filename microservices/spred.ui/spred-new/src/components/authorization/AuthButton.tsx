import React from 'react';
import { Button } from "@/components/ui/button";
import { cn } from '@/lib/utils';
import { Provider } from "@/components/authorization/AuthProvider.tsx";

const SpotifyIcon = () => (
    <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        width="24"
        height="24"
        fill="#1DB954"
        className="size-5"
    >
        <path d="M12 0C5.4 0 0 5.4 0 12s5.4 12 12 12 12-5.4 12-12S18.66 0 12 0zm5.521 17.34c-.24.371-.721.49-1.101.24-3.021-1.851-6.832-2.271-11.312-1.241-.418.07-.832-.211-.901-.63-.07-.418.211-.832.63-.901 4.921-1.121 9.142-.601 12.452 1.441.39.241.509.721.241 1.101zm1.471-3.291c-.301.459-.921.6-1.381.3-3.461-2.131-8.722-2.751-12.842-1.511-.491.15-1.021-.12-1.171-.621-.15-.491.12-1.021.621-1.171 4.681-1.411 10.522-.721 14.452 1.711.448.3.6.931.3 1.381zm.127-3.421c-4.151-2.461-11.012-2.692-14.973-1.491-.601.18-1.231-.181-1.411-.781-.181-.601.181-1.231.781-1.411 4.561-1.381 12.132-1.111 16.893 1.721.571.331.761 1.081.421 1.651-.33.571-1.08.761-1.65.421z"/>
    </svg>
);

const GoogleIcon = () => (
    <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        width="24"
        height="24"
        className="size-5"
    >
        <path
            fill="#4285F4"
            d="M23.745 12.27c0-.79-.07-1.54-.19-2.27h-11.3v4.51h6.47c-.29 1.48-1.14 2.73-2.4 3.58v3h3.86c2.26-2.09 3.56-5.17 3.56-8.82z"
        />
        <path
            fill="#34A853"
            d="M12.255 24c3.24 0 5.95-1.08 7.93-2.91l-3.86-3c-1.08.72-2.45 1.16-4.07 1.16-3.13 0-5.78-2.11-6.73-4.96h-3.98v3.09C3.515 21.3 7.565 24 12.255 24z"
        />
        <path
            fill="#FBBC05"
            d="M5.525 14.29c-.25-.72-.38-1.49-.38-2.29s.14-1.57.38-2.29V6.62h-3.98a11.86 11.86 0 000 10.76l3.98-3.09z"
        />
        <path
            fill="#EA4335"
            d="M12.255 4.75c1.77 0 3.35.61 4.6 1.8l3.42-3.42C18.205 1.19 15.495 0 12.255 0c-4.69 0-8.74 2.7-10.71 6.62l3.98 3.09c.95-2.85 3.6-4.96 6.73-4.96z"
        />
    </svg>
);

const YandexIcon = () => (
    <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        width="24"
        height="24"
        className="size-5"
    >
        <circle cx="12" cy="12" r="12" fill="#FC3F1D"/>
        <path
            d="M13.5 5.1H12c-2.4 0-3.9 1.2-3.9 3.5 0 1.7 0.8 2.7 2.4 3.8l1.2 0.8-3.5 5.1h2.3l3-4.5v4.5h2V5.1zM13.5 12l-1.4-1c-1.2-0.9-1.6-1.5-1.6-2.4 0-1.1 0.7-1.8 1.9-1.8h1.1V12z"
            fill="#FFFFFF"
        />
    </svg>
);

interface AuthButtonProps {
    provider: Provider;
    onClick: (provider: Provider) => void;
    isLoading?: boolean;
}

const providerConfig: Record<Provider, {
    icon: React.ReactNode,
    label: string,
    className: string,
    hoverClass: string
}> = {
    spotify: {
        icon: <SpotifyIcon />,
        label: "Continue with Spotify",
        className: "bg-transparent border-border text-white",
        hoverClass: "hover:bg-[#1DB954]/10 hover:border-[#1DB954]/50"
    },
    google: {
        icon: <GoogleIcon />,
        label: "Continue with Google",
        className: "bg-transparent border-border text-white",
        hoverClass: "hover:bg-[#4285F4]/10 hover:border-[#4285F4]/50"
    },
    yandex: {
        icon: <YandexIcon />,
        label: "Continue with Yandex",
        className: "bg-transparent border-border text-white",
        hoverClass: "hover:bg-[#FC3F1D]/10 hover:border-[#FC3F1D]/50"
    }
};

const AuthButton: React.FC<AuthButtonProps> = ({
                                                   provider,
                                                   onClick,
                                                   isLoading = false
                                               }) => {
    const config = providerConfig[provider];

    return (
        <Button
            variant="outline"
            className={cn(
                "w-full flex items-center justify-center gap-2 h-12 transition-all duration-300 font-medium",
                config.className,
                config.hoverClass
            )}
            onClick={() => {
                window.gtag?.('event', 'auth_button_clicked', {
                    provider,
                });
                onClick(provider);
            }}
            disabled={isLoading}
        >
            {isLoading ? (
                <div className="size-5 border-2 border-current border-t-transparent rounded-full animate-spin" />
            ) : (
                config.icon
            )}
            <span>{config.label}</span>
        </Button>
    );
};

export default AuthButton;