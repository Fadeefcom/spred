import React from "react"
import { ThemeProviderContext, theme } from "./theme-context"

type ThemeProviderProps = {
    children: React.ReactNode
}

export function ThemeProvider({ children }: ThemeProviderProps) {
    const value = {
        theme: "dark" as theme,
        resolvedTheme: "dark" as const,
        setTheme: () => {}
    }

    return (
        <ThemeProviderContext.Provider value={value}>
            {children}
        </ThemeProviderContext.Provider>
    )
}