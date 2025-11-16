import { createContext } from "react"

export type theme = "dark" | "light" | "system"

export type ThemeProviderState = {
    theme: theme
    resolvedTheme: "dark" | "light"
    setTheme: (theme: theme) => void
}

export const ThemeProviderContext = createContext<ThemeProviderState | undefined>(undefined)
