import { Home, Upload, MessageSquare, UserRound } from "lucide-react"
import BaseSidebar, { SidebarItem } from "./BaseSidebar"
import { PATH } from "@/constants/paths"

const artistItems: SidebarItem[] = [
    { title: "Dashboard", path: PATH.ARTIST.ROOT, icon: Home },
    { title: "Upload Track", path: PATH.ARTIST.UPLOAD, icon: Upload },
    { title: "Profile", path: PATH.ARTIST.PROFILE, icon: UserRound },
    { title: "Feedback", path: PATH.ARTIST.FEEDBACK, icon: MessageSquare },
]

export default function ArtistSidebar() {
    return <BaseSidebar items={artistItems} />
}