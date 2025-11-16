import React, { useEffect, useRef, useState } from "react"
import { ChevronDown, Check } from "lucide-react"
import { cn } from "@/lib/utils"

type SelectTheme = "light" | "dark"

interface CustomSelectOption {
    value: string
    label: string
}

interface CustomSelectProps {
    id?: string
    name?: string
    required?: boolean
    value: string
    onChange: (
        e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>
    ) => void
    placeholder?: string
    options: CustomSelectOption[]
    className?: string
    theme?: SelectTheme
}

const Select: React.FC<CustomSelectProps> = ({
                                                 id,
                                                 name,
                                                 required,
                                                 value,
                                                 onChange,
                                                 placeholder = "Select an option",
                                                 options,
                                                 className,
                                                 theme = "light",
                                             }) => {
    const [open, setOpen] = useState(false)
    const triggerRef = useRef<HTMLButtonElement | null>(null)
    const listRef = useRef<HTMLDivElement | null>(null)
    const selected = options.find((o) => o.value === value)

    useEffect(() => {
        const handler = (e: MouseEvent) => {
            if (
                listRef.current &&
                !listRef.current.contains(e.target as Node) &&
                !triggerRef.current?.contains(e.target as Node)
            ) {
                setOpen(false)
            }
        }
        document.addEventListener("mousedown", handler)
        return () => document.removeEventListener("mousedown", handler)
    }, [])

    const emitChange = (newValue: string) => {
        onChange({
            target: {
                name: name || "",
                value: newValue,
            },
        } as React.ChangeEvent<HTMLInputElement>)
    }

    const surface = cn(
        "flex h-10 w-full rounded-none border px-3 py-2 text-base md:text-sm placeholder:text-muted-foreground transition-all disabled:opacity-50",
        theme === "light" && "bg-white text-black focus:ring-2 focus:ring-black border-black/10",
        theme === "dark" && "bg-black text-white focus:ring-2 focus:ring-white border-white/10"
    )

    const listSurface = cn(
        "absolute z-50 mt-1 max-h-60 w-full overflow-auto rounded-md border shadow-md",
        theme === "light" && "bg-white text-black border-black/10",
        theme === "dark" && "bg-black text-white border-white/10"
    )

    const itemSurface = (active: boolean) =>
        cn(
            "relative flex w-full cursor-default select-none items-center rounded-sm py-1.5 pl-8 pr-2 text-sm outline-none transition-colors",
            active
                ? theme === "light"
                    ? "bg-black/5"
                    : "bg-white/10"
                : theme === "light"
                    ? "hover:bg-black/5"
                    : "hover:bg-white/10"
        )

    return (
        <div className="relative w-full">
            <select
                id={id}
                name={name}
                value={value}
                onChange={() => {}}
                style={{ opacity: 0, position: "absolute", pointerEvents: "none" }}
                required={required}
                tabIndex={-1}
                aria-hidden="true"
            >
                <option value="" disabled hidden />
                {options.map((opt) => (
                    <option key={opt.value} value={opt.value}>
                        {opt.label}
                    </option>
                ))}
            </select>

            <button
                ref={triggerRef}
                type="button"
                onClick={() => setOpen((prev) => !prev)}
                className={cn(surface, "items-center justify-between", className)}
            >
        <span className={cn("line-clamp-1", !selected && "opacity-70")}>
          {selected ? selected.label : placeholder}
        </span>
                <ChevronDown className="h-4 w-4 opacity-60" />
            </button>

            {open && (
                <div ref={listRef} className={listSurface}>
                    {options.map((opt) => {
                        const active = value === opt.value
                        return (
                            <button
                                key={opt.value}
                                type="button"
                                onClick={() => {
                                    emitChange(opt.value)
                                    setOpen(false)
                                    triggerRef.current?.focus()
                                }}
                                className={itemSurface(active)}
                            >
                                {active && <Check className="absolute left-2 h-4 w-4" />}
                                {opt.label}
                            </button>
                        )
                    })}
                </div>
            )}
        </div>
    )
}

export default Select
