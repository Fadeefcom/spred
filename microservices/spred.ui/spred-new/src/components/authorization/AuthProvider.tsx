import React, { createContext, useCallback, useContext, useEffect, useState } from "react"
import { UserModel } from "@/types/UserModel.ts"
import { apiFetch } from "@/hooks/apiFetch"
import { SERVICES } from "@/constants/services"
import {toast} from "@/components/ui/use-toast.ts";

type AuthContextType = {
    user: UserModel | null
    loading: boolean
    initialized: boolean
    logout: () => void
    refetchUser: () => Promise<void>
}

export type Provider = "spotify" | "google" | "yandex"

const AuthContext = createContext<AuthContextType>({
    user: null,
    loading: false,
    initialized: false,
    logout: () => {},
    refetchUser: () => Promise.resolve(),
})

export const useAuth = () => useContext(AuthContext)

const COOKIE_META_KEY = "auth.cookieMeta"
const NINETY_DAYS_MS = 90 * 24 * 60 * 60 * 1000

type CookieMeta = {
    startedAt: number
    expiresAt: number
    roles: string[]
}

function readCookieMeta(): CookieMeta | null {
    try {
        const raw = localStorage.getItem(COOKIE_META_KEY)
        if (!raw) return null
        const parsed = JSON.parse(raw) as CookieMeta
        if (typeof parsed.expiresAt !== "number") return null
        return parsed
    } catch {
        return null
    }
}

function writeCookieMeta(meta: CookieMeta) {
    try {
        localStorage.setItem(COOKIE_META_KEY, JSON.stringify(meta))
    } catch {
        /* ignore */
    }
}

export function ensureCookieMetaExistsOrExtend(roles: string[] = [], onExtend = false) {
    const now = Date.now()
    const existing = readCookieMeta()
    if (!existing) {
        writeCookieMeta({ startedAt: now, expiresAt: now + NINETY_DAYS_MS, roles })
        return
    }
    if (onExtend) {
        writeCookieMeta({ startedAt: existing.startedAt ?? now, expiresAt: now + NINETY_DAYS_MS, roles })
    }
}

export function clearCookieMeta() {
    try {
        localStorage.removeItem(COOKIE_META_KEY)
    } catch {
        /* ignore */
    }
}

export function hasValidCookie(): boolean {
    const meta = readCookieMeta()
    return !!meta && Date.now() < meta.expiresAt
}

export function getStoredRoles(): string[] {
    const meta = readCookieMeta()
    return meta?.roles ?? []
}

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
    const [user, setUser] = useState<UserModel | null>(null)
    const [loading, setLoading] = useState(false)
    const [initialized, setInitialized] = useState(false)

    const fetchSubscriptionStatus = useCallback(async (): Promise<UserModel["subscription"]> => {
        try {
            const response = await apiFetch(`${SERVICES.SUBSCRIPTION}/status`, { method: "GET" })
            if (!response.ok) {
                toast({
                    title: "Subscription error",
                    description: "Failed to load subscription status.",
                    variant: "destructive",
                })
                return { isActive: false }
            }

            const data = await response.json()
            return {
                isActive: data.isActive ?? data.status?.isActive ?? false,
                logicalState: data.logicalState,
            }
        } catch (error) {
            console.error("Subscription fetch error:", error)
            toast({
                title: "Error",
                description: "Unable to fetch subscription status.",
                variant: "destructive",
            })
            return { isActive: false }
        }
    }, [])

    const fetchUser = useCallback(async () => {
        try {
            setLoading(true)

            const me = async () =>
                await apiFetch(`${SERVICES.USER}/user/me`, {
                    headers: { "Content-Type": "application/json" },
                    credentials: "include",
                })

            let res = await me()

            if (res.status === 401) {
                const refresh = await apiFetch(`${SERVICES.USER}/auth/login`, {
                    method: "GET",
                    credentials: "include",
                })

                if (!refresh.ok) {
                    clearCookieMeta()
                    setUser(null)
                    return
                }

                res = await me()
                if (res.status === 401 || !res.ok) {
                    setUser(null)
                    return
                }

                const data = await res.json()
                const expiresAt = Date.now() + 55 * 60 * 1000
                setUser({ ...data, expiresAt })
                ensureCookieMetaExistsOrExtend(Array.isArray(data.roles) ? data.roles : [], true)

                window.gtag?.("event", "user_logged_in", {
                    user_id: data.id,
                    method: "cookie",
                    page_path: window.location.pathname,
                })
                window.gtag?.("config", "<G-ID>", { user_id: data.id })
                return
            }

            if (!res.ok) throw new Error("me failed")

            const data = await res.json()
            const expiresAt = Date.now() + 55 * 60 * 1000
            const subscription = await fetchSubscriptionStatus()
            setUser({ ...data, expiresAt, subscription })
            ensureCookieMetaExistsOrExtend(Array.isArray(data.roles) ? data.roles : [], false)

            window.gtag?.("event", "user_logged_in", {
                user_id: data.id,
                method: "cookie",
                page_path: window.location.pathname,
            })
            window.gtag?.("config", "<G-ID>", { user_id: data.id })
        } catch {
            clearCookieMeta()
            setUser(null)
        } finally {
            setLoading(false)
            setInitialized(true)
        }
    }, [])

    useEffect(() => {
        if (!initialized) {
            void fetchUser()
            return
        }

        if (user) {
            const delay = user.expiresAt - Date.now()
            if (delay <= 0) {
                void fetchUser()
                return
            }
            const t = setTimeout(() => {
                void fetchUser()
            }, delay)
            return () => clearTimeout(t)
        }
    }, [initialized, user, fetchUser])

    const logout = async () => {
        window.gtag?.("event", "user_logged_out", { page_path: window.location.pathname })
        await apiFetch(`${SERVICES.USER}/auth/logout`, {
            method: "POST",
            credentials: "include",
        })
        clearCookieMeta()
        setUser(null)
    }

    return (
        <AuthContext.Provider value={{ user, loading, initialized, logout, refetchUser: fetchUser }}>
            {children}
        </AuthContext.Provider>
    )
}
