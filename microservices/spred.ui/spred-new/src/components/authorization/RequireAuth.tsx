import React, { useEffect } from "react"
import { useAuth } from "./AuthProvider"
import { Loading } from "@/components/loading/loading"
import { Role } from "@/components/authorization/RoleSelect"
import ErrorPage from "@/pages/ErrorPage.tsx";
import { Navigate } from "react-router-dom"
import { PATH } from "@/constants/paths.ts"

export const RequireAuth = ({
                                children,
                                allowed,
                            }: {
    children: React.ReactNode
    allowed?: Role[]
}) => {
    const { user, loading, initialized } = useAuth()

    if (!initialized || loading) {
        return <Loading />
    }

    if (!user) {
        return <Navigate to={PATH.LOGIN} replace />
    }

    if (allowed && !user.roles.some(r => allowed.map(a => a.toLowerCase()).includes(r.toLowerCase()))) {
        return (
            <ErrorPage
                status="403"
                title="Access Denied"
                detail="You do not have permission to view this page."
            />
        )
    }

    return <>{children}</>
}
