import { useNavigate } from "react-router-dom"
import {getStoredRoles, hasValidCookie, useAuth} from "@/components/authorization/AuthProvider"
import clsx from "clsx"
import { PATH } from "@/constants/paths"

interface AccessDemoButtonProps {
  label?: string
  className?: string
  children?: React.ReactNode
}

export default function AccessDemoButton({
                                           label = "Access Demo",
                                           className,
                                           children,
                                         }: AccessDemoButtonProps) {
  const navigate = useNavigate()
  const { user } = useAuth()

  const resolveRole = (): string | undefined => {
    const direct = user?.roles?.[0]
    if (direct) return direct.toLowerCase()
    if (hasValidCookie()) {
      const stored = getStoredRoles()
      const first = stored?.[0]
      return first ? first.toLowerCase() : undefined
    }
    return undefined
  }

  const handleClick = () => {
    const role = resolveRole()

    window.gtag?.("event", "navigate_try_platform", {
      event_category: "navigation",
      event_label: user ? role ?? "unknown" : "guest",
      destination: user
          ? role === "artist"
              ? PATH.ARTIST.ROOT
              : role === "curator"
                  ? PATH.CURATOR.ROOT
                  : PATH.LABEL.ROOT
          : PATH.LOGIN,
    })

    if (!user && !(hasValidCookie() && role)) {
      navigate(PATH.LOGIN)
      return
    }

    switch (role) {
      case "artist":
        navigate(PATH.ARTIST.ROOT)
        break
      case "curator":
        navigate(PATH.CURATOR.ROOT)
        break
      case "label":
        navigate(PATH.LABEL.ROOT)
        break
      default:
        navigate("/")
        break
    }
  }

  return (
      <button onClick={handleClick} className={clsx("spred-button", className)}>
        {children ?? label}
      </button>
  )
}
