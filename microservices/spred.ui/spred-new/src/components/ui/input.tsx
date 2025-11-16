import * as React from "react"

import { cn } from "@/lib/utils.ts";

type InputTheme = "light" | "dark"

interface InputProps extends React.ComponentProps<"input"> {
    theme?: InputTheme
}

const Input = React.forwardRef<HTMLInputElement, InputProps>(
    ({ className, type, theme = "light", ...props }, ref) => {
        return (
            <input
                type={type}
                className={cn(
                    "flex h-10 w-full rounded-none border px-3 py-2 text-base md:text-sm placeholder:text-muted-foreground " +
                    "transition-all disabled:opacity-50 focus:ring-white border-white/10",
                    theme === "light" && "bg-white text-black focus:ring-black border-black/10",
                    theme === "dark" && "bg-black text-white focus:ring-white border-white/10",
                    className
                )}
                ref={ref}
                {...props}
            />
        )
    }
)

Input.displayName = "Input"

export {Input}
