import * as React from "react"
import { Slot } from "@radix-ui/react-slot"
import { cva, type VariantProps } from "class-variance-authority"

import { cn } from "@/lib/utils.ts";

const buttonVariants = cva(
    "inline-flex items-center justify-center gap-2 whitespace-nowrap " +
    "rounded-md text-sm font-medium ring-offset-background transition-colors " +
    "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring " +
    "focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 " +
    "[&_svg]:pointer-events-none [&_svg]:size-4 [&_svg]:shrink-0",
    {
        variants: {
            variant: {
                default: "bg-primary text-primary-foreground hover:bg-primary/70",
                destructive: "bg-destructive text-destructive-foreground hover:bg-destructive/90",
                outline: "border border-input bg-background text-foreground hover:bg-accent hover:text-accent-foreground dark:border-zinc-700 dark:hover:bg-zinc-800",
                secondary: "bg-primary/50 text-sidebar-primary-foreground hover:bg-sidebar-primary/80",
                ghost: "hover:bg-accent",
                link: "text-primary underline-offset-4 hover:underline",
            },
            size: {
                default: "h-10 px-4 py-2",
                sm: "h-9 rounded-md px-3",
                lg: "h-11 rounded-md px-8",
                icon: "h-10 w-10",
            },
        },
        defaultVariants: {
            variant: "default",
            size: "default",
        },
    }
)

// Add dark theme hover effects
const darkHoverVariants = {
    default: "dark:hover:bg-primary/90",
    destructive: "dark:hover:bg-destructive/90",
    outline: "dark:hover:bg-accent dark:hover:text-accent-foreground",
    secondary: "dark:hover:bg-secondary/80",
    ghost: "dark:hover:bg-accent dark:hover:text-accent-foreground",
    link: "dark:hover:underline",
}

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, asChild = false, ...props }, ref) => {
    const Comp = asChild ? Slot : "button"
    return (
      <Comp
        className={cn(buttonVariants({ variant, size, className }))}
        ref={ref}
        {...props}
      />
    )
  }
)
Button.displayName = "Button"

export { Button, buttonVariants }
