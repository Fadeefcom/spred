import {Home, Users, MessageSquare, UserRound} from "lucide-react"
import BaseSidebar, { SidebarItem } from "./BaseSidebar"
import { PATH } from "@/constants/paths"

const labelItems: SidebarItem[] = [
    { title: "Dashboard", path: PATH.LABEL.ROOT, icon: Home },
    { title: "Artists", path: PATH.LABEL.ARTISTS, icon: Users },
    { title: "Feedback", path: PATH.LABEL.FEEDBACK, icon: MessageSquare },
    { title: "Profile", path: PATH.LABEL.PROFILE, icon: UserRound },
]

export default function LabelSidebar() {
    return <BaseSidebar items={labelItems} />
}