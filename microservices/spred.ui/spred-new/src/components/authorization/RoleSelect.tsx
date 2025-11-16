import React from "react";
import {ToggleGroup, ToggleGroupItem} from "@/components/ui/toggle-group";

export type Role = "artist" | "curator" | "label";

interface RoleSelectProps {
    value: Role;
    onChange: (value: Role) => void;
}

const roles: Role[] = ["artist", "curator"];

const RoleSelect: React.FC<RoleSelectProps> = ({value, onChange}) => {
    return (
        <div className="space-y-3">
            <p className="text-sm text-zinc-300 text-center">Select your role</p>
            <ToggleGroup
                type="single"
                value={value}
                onValueChange={(val) => {
                    if (val) onChange(val as Role);
                }}
                className="justify-center gap-2"
            >
                {roles.map((r) => (
                    <ToggleGroupItem
                        key={r}
                        value={r}
                        aria-label={r}
                        className={[
                            "capitalize w-28 h-10",
                            "border border-white/10 bg-white/5 text-zinc-200",
                            "data-[state=on]:bg-white/15 data-[state=on]:text-white data-[state=on]:border-white/20",
                            "hover:bg-white/10 hover:text-white",
                            "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white/30",
                            "transition-colors"
                        ].join(" ")}
                    >
                        {r}
                    </ToggleGroupItem>
                ))}
            </ToggleGroup>
        </div>
    );
};

export default RoleSelect;