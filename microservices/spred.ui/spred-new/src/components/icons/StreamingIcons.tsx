import React from "react";

type IconProps = { className?: string; color?: string };

export const YoutubeMusicIcon = ({ className, color = "#FF0000" }: IconProps) => (
    <svg viewBox="0 0 24 24" className={className} fill={color} xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
        <path d="M23.5 6.2s-.2-1.6-.8-2.3c-.8-.9-1.7-.9-2.1-1-3-.2-7.6-.2-7.6-.2h-.1s-4.6 0-7.6.2c-.4 0-1.3 0-2.1 1-.6.7-.8 2.3-.8 2.3S2 8.1 2 10.1v1.8c0 2 .2 3.9.2 3.9s.2 1.6.8 2.3c.8.9 1.9.9 2.4 1 1.8.2 7.5.2 7.5.2s4.6 0 7.6-.2c.4 0 1.3 0 2.1-1 .6-.7.8-2.3.8-2.3s.2-1.9.2-3.9v-1.8c0-2-.2-3.9-.2-3.9zM9.8 14.6V8.4l6.3 3.1-6.3 3.1z"/>
    </svg>
);

export const SpotifyIcon = ({ className, color = "#1DB954" }: IconProps) => (
    <svg viewBox="0 0 24 24" className={className} fill={color} xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
        <path d="M12 0C5.4 0 0 5.4 0 12s5.4 12 12 12 12-5.4 12-12S18.66 0 12 0zm5.521 17.34c-.24.371-.721.49-1.101.24-3.021-1.851-6.832-2.271-11.312-1.241-.418.07-.832-.211-.901-.63-.07-.418.211-.832.63-.901 4.921-1.121 9.142-.601 12.452 1.441.39.241.509.721.241 1.101zm1.471-3.291c-.301.459-.921.6-1.381.3-3.461-2.131-8.722-2.751-12.842-1.511-.491.15-1.021-.12-1.171-.621-.15-.491.12-1.021.621-1.171 4.681-1.411 10.522-.721 14.452 1.711.448.3.6.931.3 1.381zm.127-3.421c-4.151-2.461-11.012-2.692-14.973-1.491-.601.18-1.231-.181-1.411-.781-.181-.601.181-1.231.781-1.411 4.561-1.381 12.132-1.111 16.893 1.721.571.331.761 1.081.421 1.651-.33.571-1.08.761-1.65.421z"/>
    </svg>
);

export const AppleMusicIcon = ({ className, color = "#FA243C" }: IconProps) => (
    <svg viewBox="0 0 24 24" className={className} fill={color} xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
        <path d="M16.365 1.43c.15-.42-.25-.84-.65-.67l-7.76 3.06c-.33.13-.55.46-.55.82v13.84c0 .54.52.92 1.03.76l7.76-2.34c.35-.1.59-.43.59-.8V1.43z"/>
    </svg>
);

export const DeezerIcon = ({ className, color = "#EF5466" }: IconProps) => (
    <svg viewBox="0 0 24 24" className={className} fill={color} xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
        <path d="M2 9h4v6H2zM7 6h4v12H7zM12 3h4v18h-4zM17 0h4v24h-4z"/>
    </svg>
);

export const SoundCloudIcon = ({ className, color = "#FF7700" }: IconProps) => (
    <svg viewBox="0 0 24 24" className={className} fill={color} xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
        <path d="M6 19h13a4 4 0 0 0 0-8 6 6 0 0 0-11-3A5 5 0 0 0 6 19z"/>
    </svg>
);
