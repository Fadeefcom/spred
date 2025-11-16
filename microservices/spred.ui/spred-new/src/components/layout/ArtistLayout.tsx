import { ReactElement } from "react"
import BaseLayout from "./BaseLayout"
import ArtistSidebar from "./ArtistSidebar"

export default function ArtistLayout({ children }: { children: ReactElement }) {
    return (
        <BaseLayout sidebar={<ArtistSidebar />} headerTitle="Spred – Artist">
            {children}
        </BaseLayout>
    )
}
