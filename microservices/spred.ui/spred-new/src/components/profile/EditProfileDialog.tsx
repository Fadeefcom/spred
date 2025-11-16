import React, { useState, useEffect } from "react"
import { Loader2 } from "lucide-react"
import {
    Dialog,
    DialogContent,
    DialogHeader,
    DialogTitle,
    DialogFooter,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Textarea } from "@/components/ui/textarea"
import { UserModel } from "@/types/UserModel.ts"
import { useTheme } from "@/components/theme/useTheme"
import {Avatar, AvatarFallback, AvatarImage} from "@/components/ui/avatar.tsx";
import { UploadButton } from "@/components/ui/UploadButton.tsx"
import {LocationSearch} from "@/components/ui/LocationSearch.tsx"
import {apiFetch} from "@/hooks/apiFetch.tsx";
import {Input} from "@/components/ui/input.tsx";
import { SERVICES } from "@/constants/services"

type EditProfileDialogProps = {
    open: boolean
    onClose: () => void
    user: UserModel
    onSave: () => void
}

const EditProfileDialog = ({ open, onClose, user, onSave }: EditProfileDialogProps) => {
    const [bio, setBio] = useState(user?.bio ?? "")
    const [location, setLocation] = useState(user?.location ?? "")
    const [name, setName] = useState(user?.username ?? "")
    const [loading, setLoading] = useState(false)
    const [avatar, setAvatar] = useState<File | null>(null);
    const [previewDataUrl, setPreviewDataUrl] = useState<string | null>(null)
    const { resolvedTheme } = useTheme()

    const handleFileSelect = (fileOrFiles: File | File[] | null) => {
        const file = Array.isArray(fileOrFiles) ? fileOrFiles[0] ?? null : fileOrFiles
        setAvatar(file)
        if (!file) { setPreviewDataUrl(null); return }
        const reader = new FileReader()
        reader.onload = () => setPreviewDataUrl(reader.result as string)
        reader.readAsDataURL(file)
    }

    const handleSubmit = async () => {
        setLoading(true)
        try {
            await apiFetch(`${SERVICES.USER}/user/me`, {
                method: "PATCH",
                headers: {"Content-Type": "application/json"},
                body: JSON.stringify({bio, location, name}),
            })

            if (avatar) {
                const formData = new FormData()
                formData.append("file", avatar)
                await apiFetch(`${SERVICES.USER}/user/me/avatar`, {
                    method: "PUT",
                    body: formData,
                    credentials: "include",
                })
            }
        }
        finally {
            setLoading(false)
            onSave()
            onClose()
        }
    }

    const bgClass =
        resolvedTheme === "dark"
            ? "bg-zinc-900 text-zinc-100 border-zinc-800"
            : "bg-white text-zinc-900 border-zinc-200"

    const inputBg =
        resolvedTheme === "dark" ? "bg-zinc-800 text-zinc-100" : "bg-zinc-50 text-zinc-900"

    return (
        <Dialog open={open} onOpenChange={onClose}>
            <DialogContent className="sm:max-w-[600px] p-6 bg-background text-foreground border border-border">
                {loading && (
                    <div className="absolute inset-0 bg-black/40 flex items-center justify-center z-50 rounded-md">
                        <Loader2 className="h-8 w-8 animate-spin text-white" />
                    </div>
                )}
                <DialogHeader>
                    <DialogTitle className="text-base-custom leading-6">Edit Profile</DialogTitle>
                </DialogHeader>

                <div className="grid grid-cols-3 gap-4 mt-4">
                    {/* Avatar */}
                    <div className="col-span-1 flex flex-col items-center justify-between">
                        <div className="w-24 h-24 rounded-full overflow-hidden border border-border bg-card">
                            <Avatar className="w-24 h-24">
                                {previewDataUrl || user?.avatarUrl ? (
                                    <AvatarImage src={previewDataUrl ?? user.avatarUrl!} />
                                ) : (
                                    <AvatarFallback className="bg-spred-yellow text-black text-xl">
                                        {user?.username?.[0]?.toUpperCase() ?? "U"}
                                    </AvatarFallback>
                                )}
                            </Avatar>
                        </div>
                        <UploadButton
                            onFilesSelect={handleFileSelect}
                            accept="image/*"
                            variant="compact"
                            multiple={false}
                            maxSize={5}
                        />
                    </div>

                    {/* Bio + Location */}
                    <div className="col-span-2 flex flex-col space-y-4">
                        <div>
                            <p className="text-xs-custom text-muted-foreground mb-1">Name</p>
                            <Input
                                value={name}
                                onChange={(e) => setName(e.target.value)}
                                className="text-sm w-full bg-muted text-foreground border border-border"
                            />
                        </div>
                        <div>
                            <p className="text-xs-custom text-muted-foreground mb-1">Bio</p>
                            <Textarea
                                value={bio}
                                onChange={(e) => setBio(e.target.value)}
                                placeholder="Your bio..."
                                className="text-sm w-full bg-muted text-foreground border border-border"
                            />
                        </div>
                        <div>
                            <p className="text-xs-custom text-muted-foreground mb-1">Location</p>
                            <LocationSearch
                                value={location}
                                onChange={(e) => setLocation(e)}
                                className="text-sm w-full bg-muted text-foreground border border-border"
                            />
                        </div>
                    </div>
                </div>

                <DialogFooter className="mt-6">
                    <Button
                        onClick={handleSubmit}
                        className="w-full bg-primary text-primary-foreground hover:bg-primary/90"
                        disabled={loading}
                    >
                        Save
                    </Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    )
}

export default EditProfileDialog
