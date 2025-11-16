import React, { createContext, useContext, useState, useEffect, ReactNode } from "react";
import { apiFetch } from "@/hooks/apiFetch";
import { SERVICES } from "@/constants/services.tsx";

interface UploadLimitContextType {
    limit: number;
    used: number;
    remaining: number;
    reset: number;
    updateFromHeaders: (headers: Headers) => void;
    refresh: () => Promise<void>;
}

const UploadLimitContext = createContext<UploadLimitContextType | null>(null);

export const UploadLimitProvider = ({ children }: { children: ReactNode }) => {
    const [limit, setLimit] = useState<number>(3);
    const [used, setUsed] = useState<number>(0);
    const [remaining, setRemaining] = useState<number>(3);
    const [reset, setReset] = useState<number>(0);

    const updateFromHeaders = (headers: Headers) => {
        const limitHeader = headers.get("X-RateLimit-Limit");
        const usedHeader = headers.get("X-RateLimit-Used");
        const remainingHeader = headers.get("X-RateLimit-Remaining");
        const resetHeader = headers.get("X-RateLimit-Reset");

        console.groupCollapsed("[UploadLimit] Received rate limit headers");
        console.log("X-RateLimit-Limit:", limitHeader);
        console.log("X-RateLimit-Used:", usedHeader);
        console.log("X-RateLimit-Remaining:", remainingHeader);
        console.log("X-RateLimit-Reset:", resetHeader);
        console.groupEnd();

        if (limitHeader) setLimit(limitHeader === "unlimited" ? Infinity : parseInt(limitHeader, 10));
        if (usedHeader) setUsed(parseInt(usedHeader, 10));
        if (remainingHeader) setRemaining(parseInt(remainingHeader, 10));
        if (resetHeader) setReset(parseInt(resetHeader, 10));
    };

    const refresh = async () => {
        try {
            const res = await apiFetch(`${SERVICES.TRACK}/upload/limit`);
            if (res.ok) {
                const data = await res.json();
                setLimit(data.limit);
                setUsed(data.used);
                setRemaining(data.remaining);
                setReset(data.reset);
            }
        } catch (e) {
            console.error(e);
        }
    };

    useEffect(() => {
        refresh();
    }, []);

    return (
        <UploadLimitContext.Provider
            value={{
                limit,
                used,
                remaining,
                reset,
                updateFromHeaders,
                refresh,
            }}
        >
            {children}
        </UploadLimitContext.Provider>
    );
};

export const useUploadLimit = () => {
    const ctx = useContext(UploadLimitContext);
    if (!ctx) throw new Error("useUploadLimit must be used within UploadLimitProvider");
    return ctx;
};
