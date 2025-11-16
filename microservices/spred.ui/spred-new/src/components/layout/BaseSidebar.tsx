import { useLocation, NavLink } from "react-router-dom"
import { Menu, X, LogOut } from "lucide-react"
import { Shield, FileText } from "lucide-react"
import { ThemeToggle } from "@/components/theme/theme-toggle"
import {
    Sidebar,
    SidebarContent,
    SidebarFooter,
    SidebarGroup,
    SidebarGroupContent,
    SidebarGroupLabel,
    SidebarHeader,
    SidebarMenu,
    SidebarMenuButton,
    SidebarMenuItem,
    SidebarTrigger,
    useSidebar,
} from "@/components/ui/sidebar"
import { useState } from "react"
import clsx from "clsx"
import { useAuth } from "@/components/authorization/AuthProvider"
import {PATH} from '@/constants/paths.ts';

export interface SidebarItem {
    title: string
    path: string
    icon: React.ElementType
}

interface BaseSidebarProps {
    items: SidebarItem[]
}

export default function BaseSidebar({ items }: BaseSidebarProps) {
    const location = useLocation()
    const { state } = useSidebar()
    const [isOpen, setIsOpen] = useState(false)
    const { user, logout } = useAuth()

    const handleLogout = () => logout()

    return (
        <>
            {/* Mobile Toggle Button */}
            <button
                className="md:hidden fixed top-4 right-4 z-50 p-2"
                onClick={() => setIsOpen(!isOpen)}
                aria-label={isOpen ? "Close menu" : "Open menu"}
            >
                {isOpen ? <X className="w-6 h-6" /> : <Menu className="w-6 h-6" />}
            </button>

            {/* Overlay background */}
            {isOpen && (
                <div
                    className="fixed inset-0 z-40 bg-black/50 md:hidden"
                    onClick={() => setIsOpen(false)}
                />
            )}

            {/* Mobile slide-in menu */}
            <div
                className={clsx(
                    "fixed inset-0 bg-background text-foreground z-50 p-6 pt-24 flex flex-col h-full",
                    "transform transition-transform duration-300 ease-in-out md:hidden",
                    isOpen ? "translate-x-0" : "translate-x-full"
                )}
            >
                <button
                    className="absolute top-4 right-4 p-2 rounded-full hover:bg-accent"
                    onClick={() => setIsOpen(false)}
                    aria-label="Close menu"
                >
                    <X className="w-6 h-6" />
                </button>

                <div className="mt-2 mb-4 py-2 rounded-md w-full pb-7">
                    <span className="block font-semibold text-xl text-left">{user?.username}</span>
                </div>

                <div className="flex flex-col space-y-6">
                    {items.map((item) => {
                        const Icon = item.icon
                        return (
                            <NavLink
                                key={item.title}
                                to={item.path}
                                className={({ isActive }) =>
                                    clsx(
                                        "flex items-center space-x-3 text-lg font-medium transition-colors",
                                        isActive ? "active-nav-link" : "text-foreground hover:text-primary"
                                    )
                                }
                                onClick={() => setIsOpen(false)}
                            >
                                <Icon className="w-5 h-5" />
                                <span>{item.title}</span>
                            </NavLink>
                        )
                    })}
                </div>

                <div className="mt-auto flex items-center justify-between">
                    <button
                        onClick={() => {
                            handleLogout()
                            setIsOpen(false)
                        }}
                        className="flex items-center justify-center gap-2 px-6 py-3 text-lg font-semibold
              text-muted-foreground hover:text-foreground border border-border rounded-md"
                    >
                        <LogOut className="w-5 h-5 mr-2" />
                        Logout
                    </button>
                    <ThemeToggle />
                </div>
            </div>

            {/* Desktop sidebar */}
            <div
                className={clsx(
                    "fixed inset-0 z-40 bg-background transition-transform duration-300 ease-in-out md:static md:translate-x-0",
                    isOpen ? "translate-x-0" : "translate-x-full",
                    "md:w-auto"
                )}
            >
                <Sidebar collapsible="icon" className="h-full shadow md:shadow-none">
                    <SidebarHeader className="px-4 py-6" />

                    <SidebarContent>
                        <SidebarGroup>
                            <SidebarGroupLabel>Menu</SidebarGroupLabel>
                            <SidebarGroupContent>
                                <SidebarMenu>
                                    {items.map((item) => (
                                        <SidebarMenuItem key={item.title}>
                                            <SidebarMenuButton
                                                asChild
                                                isActive={location.pathname === item.path}
                                                tooltip={state === "collapsed" ? item.title : undefined}
                                                onClick={() => setIsOpen(false)}
                                                className={clsx(
                                                    location.pathname === item.path && "active-nav-link"
                                                )}
                                            >
                                                <NavLink
                                                    to={item.path}
                                                    className={({ isActive }) =>
                                                        clsx(isActive && "active-nav-link")
                                                    }
                                                >
                                                    <item.icon />
                                                    <span>{item.title}</span>
                                                </NavLink>
                                            </SidebarMenuButton>
                                        </SidebarMenuItem>
                                    ))}
                                </SidebarMenu>
                            </SidebarGroupContent>
                        </SidebarGroup>
                    </SidebarContent>

                    <SidebarFooter className="p-4 space-y-3">
                        <SidebarMenu>
                            <SidebarMenuItem>
                                <SidebarMenuButton
                                    asChild
                                    tooltip={state === "collapsed" ? "Privacy Policy" : undefined}
                                    className={clsx(location.pathname === PATH.PRIVACY_POLICY && "active-nav-link")}
                                    size="sm"
                                >
                                    <NavLink
                                        to={PATH.PRIVACY_POLICY}
                                        className="hover:text-spred-yellowdark"
                                    >
                                        <Shield className="w-4 h-4" />
                                        <span>Privacy Policy</span>
                                    </NavLink>
                                </SidebarMenuButton>
                            </SidebarMenuItem>

                            <SidebarMenuItem>
                                <SidebarMenuButton
                                    asChild
                                    tooltip={state === "collapsed" ? "Terms of Use" : undefined}
                                    className={clsx(location.pathname === PATH.TERMS_OF_USE && "active-nav-link")}
                                    size="sm"
                                >
                                    <NavLink
                                        to={PATH.TERMS_OF_USE}
                                        className="hover:text-spred-yellowdark text-sm"
                                    >
                                        <FileText />
                                        <span>Terms of Use</span>
                                    </NavLink>
                                </SidebarMenuButton>
                            </SidebarMenuItem>
                        </SidebarMenu>

                        <div className="flex items-center justify-between pt-3 border-t border-border">
                            <SidebarTrigger />
                        </div>
                    </SidebarFooter>
                </Sidebar>
            </div>
        </>
    )
}
