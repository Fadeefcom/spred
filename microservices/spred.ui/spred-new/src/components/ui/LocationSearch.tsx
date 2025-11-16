import React, { useEffect, useMemo, useRef, useState } from "react"
import { createPortal } from "react-dom"
import { MapPin, Search, Loader2 } from "lucide-react"
import { cn } from "@/lib/utils"

interface NominatimAddress {
    city?: string
    town?: string
    village?: string
    municipality?: string
    country?: string
    country_code?: string
}

interface LocationResult {
    place_id: number
    display_name: string
    lat: string
    lon: string
    type: "city" | "town" | "village" | "country"
    class: string
    address?: NominatimAddress
    name?: string
}

interface LocationSearchProps {
    value?: string
    onChange?: (value: string) => void
    onLocationSelect?: (location: LocationResult) => void
    placeholder?: string
    className?: string
}

export const LocationSearch: React.FC<LocationSearchProps> = ({
                                                                  value = "",
                                                                  onChange,
                                                                  onLocationSelect,
                                                                  placeholder = "Search for cities and countries...",
                                                                  className,
                                                              }) => {
    const [query, setQuery] = useState(value)
    const [manualInput, setManualInput] = useState(false)
    const [results, setResults] = useState<LocationResult[]>([])
    const [isLoading, setIsLoading] = useState(false)
    const [isOpen, setIsOpen] = useState(false)
    const [selectedIndex, setSelectedIndex] = useState(-1)

    const rootRef = useRef<HTMLDivElement>(null)
    const inputRef = useRef<HTMLInputElement>(null)
    const listRef = useRef<HTMLDivElement>(null)
    const itemRefs = useRef<(HTMLDivElement | null)[]>([])
    const [menuStyle, setMenuStyle] = useState<React.CSSProperties>({})

    useEffect(() => {
        setQuery(value)
    }, [value])

    useEffect(() => {
        if (!manualInput) return
        const t = setTimeout(() => {
            const q = query.trim()
            if (q.length < 2) {
                setResults([])
                setIsOpen(false)
                return
            }
            void searchLocations(q)
        }, 300)
        return () => clearTimeout(t)
    }, [query, manualInput])

    useEffect(() => {
        const onDocClick = (e: MouseEvent) => {
            const t = e.target as Node
            if (rootRef.current?.contains(t)) return
            if (listRef.current?.contains(t)) return
            setIsOpen(false)
        }
        document.addEventListener("click", onDocClick)
        return () => document.removeEventListener("click", onDocClick)
    }, [])

    const recalcMenuPosition = () => {
        const el = inputRef.current
        if (!el) return
        const r = el.getBoundingClientRect()
        setMenuStyle({ position: "fixed", top: r.bottom + 4, left: r.left, width: r.width })
    }

    useEffect(() => {
        if (!isOpen) return
        recalcMenuPosition()
        const onScroll = () => recalcMenuPosition()
        const onResize = () => recalcMenuPosition()
        window.addEventListener("scroll", onScroll, true)
        window.addEventListener("resize", onResize)
        return () => {
            window.removeEventListener("scroll", onScroll, true)
            window.removeEventListener("resize", onResize)
        }
    }, [isOpen])

    useEffect(() => {
        if (selectedIndex < 0) return
        const el = itemRefs.current[selectedIndex]
        el?.scrollIntoView({ block: "nearest" })
    }, [selectedIndex])

    const searchLocations = async (searchQuery: string) => {
        setIsLoading(true)
        try {
            const response = await fetch(
                `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(
                    searchQuery
                )}&limit=8&addressdetails=1&extratags=1&accept-language=en`
            )
            if (!response.ok) {
                setResults([])
                setIsOpen(false)
                return
            }
            const data = await response.json()
            const filteredResults = data.filter((item: LocationResult) =>
                ["city", "town", "village", "country", "state", "administrative"].some(
                    (type) => item.type === type || item.class === "place" || item.class === "boundary"
                )
            )
            setResults(filteredResults)
            setIsOpen(filteredResults.length > 0)
            setSelectedIndex(-1)
        } catch {
            setResults([])
            setIsOpen(false)
        } finally {
            setIsLoading(false)
        }
    }

    const makeLabel = (loc: LocationResult) => {
        const first = (loc.display_name.split(",")[0] || "").trim()
        if (loc.type === "country") return loc.address?.country || first
        const city =
            loc.address?.city ||
            loc.address?.town ||
            loc.address?.village ||
            loc.address?.municipality ||
            first
        const country = loc.address?.country
        return country ? `${city}, ${country}` : city
    }

    const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setManualInput(true)
        setQuery(e.target.value)
        onChange?.(e.target.value)
    }

    const handleLocationSelect = (loc: LocationResult) => {
        const label = makeLabel(loc)
        setQuery(label)
        onChange?.(label)
        setIsOpen(false)
        onLocationSelect?.(loc)
    }

    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (!isOpen) return
        if (e.key === "ArrowDown") {
            e.preventDefault()
            setSelectedIndex((p) => (p < results.length - 1 ? p + 1 : p))
        } else if (e.key === "ArrowUp") {
            e.preventDefault()
            setSelectedIndex((p) => (p > 0 ? p - 1 : -1))
        } else if (e.key === "Enter") {
            e.preventDefault()
            if (selectedIndex >= 0 && results[selectedIndex])
                handleLocationSelect(results[selectedIndex])
        } else if (e.key === "Escape") {
            setIsOpen(false)
            setSelectedIndex(-1)
        }
    }

    const inputSurface = cn(
        "flex h-10 w-full rounded-none border px-3 py-2 text-base md:text-sm placeholder:text-muted-foreground transition-all disabled:opacity-50 pl-10 pr-10",
        "bg-background text-foreground border-border focus:ring-2 focus:ring-ring focus:border-transparent"
    )

    const listSurface = useMemo(
        () =>
            cn(
                "z-[2147483647] max-h-64 overflow-auto rounded-none border shadow-lg pointer-events-auto",
                "bg-background text-foreground border-border"
            ),
        []
    )

    const itemClass = (active: boolean) =>
        cn(
            "flex items-center gap-2 px-3 py-2 text-sm cursor-pointer select-none",
            active ? "bg-accent" : "hover:bg-accent"
        )

    return (
        <div ref={rootRef} className={cn("relative w-full", className)}>
            <div className="relative">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <input
                    ref={inputRef}
                    type="text"
                    placeholder={placeholder}
                    value={query}
                    onChange={handleInputChange}
                    onKeyDown={handleKeyDown}
                    onFocus={() => results.length > 0 && setIsOpen(true)}
                    className={inputSurface}
                    aria-expanded={isOpen}
                    aria-autocomplete="list"
                    role="combobox"
                />
                {isLoading && (
                    <Loader2 className="absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 animate-spin text-muted-foreground" />
                )}
            </div>

            {isOpen &&
                results.length > 0 &&
                createPortal(
                    <div style={menuStyle} className={listSurface} role="listbox" ref={listRef}>
                        {results.map((loc, i) => (
                            <div
                                key={loc.place_id}
                                ref={(el) => (itemRefs.current[i] = el)}
                                className={itemClass(i === selectedIndex)}
                                onMouseEnter={() => setSelectedIndex(i)}
                                onMouseDown={(e) => {
                                    e.preventDefault()
                                    handleLocationSelect(loc)
                                }}
                                role="option"
                                aria-selected={i === selectedIndex}
                            >
                                <MapPin className="h-4 w-4 shrink-0 text-primary" />
                                <div className="truncate">{makeLabel(loc)}</div>
                            </div>
                        ))}
                    </div>,
                    document.body
                )}
        </div>
    )
}
