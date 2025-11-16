import { ReactElement } from "react"
import { SidebarProvider } from "@/components/ui/sidebar"
import UserMenu from "./UserMenu"
import { Link } from "react-router-dom"

export interface BaseLayoutProps {
    children: ReactElement
    sidebar: ReactElement
    headerTitle: string
}

export default function BaseLayout({ children, sidebar, headerTitle }: BaseLayoutProps) {
    return (
        <SidebarProvider>
            <div className="min-h-screen flex w-full max-h-screen">
                {sidebar}
                <div className="flex-1 flex flex-col">
                    <header className="p-4 border-b">
                        <div className="flex items-center justify-between">
                            <Link to="/" className="text-2xl font-bold">
                                {headerTitle}
                            </Link>
                            <UserMenu />
                        </div>
                    </header>
                    <main className="flex-1 px-6">
                        {children}
                    </main>
                </div>
            </div>
        </SidebarProvider>
    )
}
