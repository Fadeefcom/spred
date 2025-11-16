import * as React from "react"
import { cn } from "@/lib/utils"

type TextareaTheme = "light" | "dark"

export interface TextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
    theme?: TextareaTheme
}

const Textarea = React.forwardRef<HTMLTextAreaElement, TextareaProps>(
    ({ className, theme = "light", ...props }, ref) => {
        return (
            <textarea
                ref={ref}
                className={cn(
                    "flex w-full rounded-none border px-3 py-2 text-sm placeholder:text-muted-foreground transition-all disabled:opacity-50",
                    theme === "light" && "bg-white text-black focus:ring-2 focus:ring-black border-black/10",
                    theme === "dark" && "bg-black text-white focus:ring-2 focus:ring-white border-white/10",
                    className
                )}
                {...props}
            />
        )
    }
)

Textarea.displayName = "Textarea"
export { Textarea }
