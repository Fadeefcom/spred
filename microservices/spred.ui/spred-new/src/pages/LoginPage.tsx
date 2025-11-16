import React, { useCallback, useState, useEffect } from "react"
import {
    Card,
    CardContent,
    CardFooter,
    CardHeader,
    CardTitle,
} from "@/components/ui/card"
import { Provider } from "@/components/authorization/AuthProvider.tsx"
import AuthButton from "@/components/authorization/AuthButton.tsx"
import { toast } from "@/components/ui/use-toast"
import { PATH } from "@/constants/paths.ts"
import { SERVICES } from "@/constants/services.tsx"
import { getDeviceId } from "@/hooks/apiFetch.tsx"
import RoleSelect, { Role } from "@/components/authorization/RoleSelect"
import { useNavigate } from "react-router-dom"

const LoginPage: React.FC = () => {
    const [loadingProvider, setLoadingProvider] = useState<Provider | null>(null)
    const [role, setRole] = useState<Role>("artist")
    const navigate = useNavigate()

    const handleSignIn = useCallback(
        async (provider: Provider) => {
            setLoadingProvider(provider)

            if (provider === "spotify") {
                toast({
                    title: "Spotify Unavailable",
                    description: `Spotify login is currently unavailable. Please try a different provider.`,
                    variant: "destructive",
                })
                setLoadingProvider(null)
                return
            }

            try {
                const deviceId = await getDeviceId()
                // const params = {
                //     provider,
                //     role,
                //     deviceId,
                //     redirect_mode: "popup",
                // }
                // const query = new URLSearchParams(params).toString()
                // const popup = window.open(
                //     `${SERVICES.USER}/auth/external/login?${query}`,
                //     "_blank",
                //     "no-opener,no-referrer,width=500,height=600"
                // )
                const popup = null;

                if (!popup) {
                    console.warn("[login] popup blocked, fallback to same-tab navigation")
                    const redirectParams = {
                        provider,
                        role,
                        deviceId,
                        redirect_mode: "same",
                    }
                    const query = new URLSearchParams(redirectParams).toString()
                    window.location.href = `${SERVICES.USER}/auth/external/login?${query}`
                } else {
                    popup.focus()
                }
            } catch (error) {
                toast({
                    title: "Sign in failed",
                    description: `Failed to sign in with ${provider}. Please try again.`,
                    variant: "destructive",
                })
                setLoadingProvider(null)
            }
        },
        [role]
    )

    useEffect(() => {
        const handleMessage = async (event: MessageEvent) => {
            if (event.data === "auth_success") {
                switch (role) {
                    case "artist":
                        navigate(PATH.ARTIST.ROOT, { replace: true })
                        break
                    case "curator":
                        navigate(PATH.CURATOR.ROOT, { replace: true })
                        break
                    case "label":
                        navigate(PATH.LABEL.ROOT, { replace: true })
                        break
                    default:
                        navigate("/")
                        break
                }
            }
        }

        window.addEventListener("message", handleMessage)
        return () => window.removeEventListener("message", handleMessage)
    }, [navigate, role])

    return (
        <div className="fixed inset-0 flex items-center justify-center overflow-hidden bg-spred-black text-white">
            <div className="w-full max-w-md px-4">
                <Card className="glassmorphism border border-zinc-700 backdrop-blur-xl shadow-2xl">
                    <CardHeader className="space-y-1">
                        <CardTitle className="text-2xl text-center tracking-tight text-white">
                            Welcome to Spred
                        </CardTitle>
                        {/*<CardDescription className="text-center text-zinc-300">*/}
                        {/*    Choose your role and sign in method*/}
                        {/*</CardDescription>*/}
                    </CardHeader>

                    <CardContent className="space-y-6">
                        {/*<RoleSelect value={role} onChange={setRole} />*/}

                        <div className="space-y-3">
                            <AuthButton
                                provider="google"
                                onClick={handleSignIn}
                                isLoading={loadingProvider === "google"}
                            />
                            <AuthButton
                                provider="yandex"
                                onClick={handleSignIn}
                                isLoading={loadingProvider === "yandex"}
                            />
                        </div>
                    </CardContent>

                    <CardFooter className="flex justify-center">
                        <p className="text-xs text-zinc-400 text-center">
                            By continuing, you agree receive emails from us (only useful things!), to our {" "}
                            <a
                                href={PATH.PRIVACY_POLICY}
                                className="underline underline-offset-4 text-zinc-200 hover:text-spred-yellowdark transition-colors"
                            >
                                Privacy Policy
                            </a>{" "}
                            and{" "}
                            <a
                                href={PATH.TERMS_OF_USE}
                                className="underline underline-offset-4 text-zinc-200 hover:text-spred-yellowdark transition-colors"
                            >
                                Terms of Use
                            </a>
                        </p>
                    </CardFooter>
                </Card>
            </div>
        </div>
    )
}

export default LoginPage
