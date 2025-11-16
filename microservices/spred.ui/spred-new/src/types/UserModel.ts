import {Role} from "@/components/authorization/RoleSelect.tsx";

export interface UserModel {
    email: string;
    expiresAt: number;
    id: string;
    justRegistered: boolean;
    roles: Role[];
    username: string;
    location?: string;
    bio?: string;
    avatarUrl?: string;
    linkedAccounts?: string[];
    subscription?: {
        isActive: boolean
        logicalState?: string
        currentPeriodEnd?: string
    }
    genres: { name: string; confidence: number }[]
    stats: {
        tracksAnalyzed: number
        matchesFound: number
        pitchesSent: number
        placements: number
    }
}