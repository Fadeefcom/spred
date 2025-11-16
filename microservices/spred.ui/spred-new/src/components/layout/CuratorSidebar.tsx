import {Home, List, MessageSquare, Star, UserRound} from "lucide-react"
import BaseSidebar, { SidebarItem } from "./BaseSidebar"
import { PATH } from "@/constants/paths"

const curatorItems: SidebarItem[] = [
    { title: "Dashboard", path: PATH.CURATOR.ROOT, icon: Home },
    { title: "Submissions", path: PATH.CURATOR.SUBMISSIONS, icon: List },
    { title: "Profile", path: PATH.CURATOR.PROFILE, icon: UserRound },
    { title: "Feedback", path: PATH.CURATOR.FEEDBACK, icon: MessageSquare }

]

export default function CuratorSidebar() {
    return <BaseSidebar items={curatorItems} />
}