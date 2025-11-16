import { ReactElement } from "react"
import BaseLayout from "./BaseLayout"
import CuratorSidebar from "./CuratorSidebar"

export default function CuratorLayout({ children }: { children: ReactElement }) {
    return (
        <BaseLayout sidebar={<CuratorSidebar />} headerTitle="Spred – Curator">
            {children}
        </BaseLayout>
    )
}
