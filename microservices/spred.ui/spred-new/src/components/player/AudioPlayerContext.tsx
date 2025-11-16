import React, { createContext, useRef, useState, useContext } from "react";

type AudioPlayerInstance = {
    pause: () => void;
};

type AudioPlayerContextType = {
    register: (id: string, instance: AudioPlayerInstance) => void;
    setActive: (id: string) => void;
    activeId: string | null;
};

const AudioPlayerContext = createContext<AudioPlayerContextType | null>(null);

export const useAudioPlayerContext = () => {
    const ctx = useContext(AudioPlayerContext);
    if (!ctx) throw new Error("AudioPlayerContext not found");
    return ctx;
};

export const AudioPlayerProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const players = useRef<Record<string, AudioPlayerInstance>>({});
    const [activeId, setActiveId] = useState<string | null>(null);

    const register = (id: string, instance: AudioPlayerInstance) => {
        players.current[id] = instance;
    };

    const setActive = (id: string) => {
        if (activeId && activeId !== id) {
            players.current[activeId]?.pause(); // Остановить предыдущий плеер
        }
        setActiveId(id);
    };

    return (
        <AudioPlayerContext.Provider value={{ register, setActive, activeId }}>
            {children}
        </AudioPlayerContext.Provider>
    );
};
