import { ReactElement } from "react"
import BaseLayout from "./BaseLayout"
import LabelSidebar from "./LabelSidebar"

export default function LabelLayout({ children }: { children: ReactElement }) {
    return (
        <BaseLayout sidebar={<LabelSidebar />} headerTitle="Spred – Label">
            {children}
        </BaseLayout>
    )
}