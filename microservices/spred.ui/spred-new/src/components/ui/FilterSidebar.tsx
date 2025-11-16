import { useState, useEffect, useRef } from "react";
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "@/components/ui/accordion";
import { Button } from "@/components/ui/button";
import {getStableColor} from "@/components/ui/TagsBlock.tsx";

interface FilterSidebarProps {
    onFiltersChange?: (filters: FilterState) => void;
    tags?: string[];
}

interface FilterState {
    type: string[];
    tags: {
        include: string[];
        exclude: string[];
    };
}

export function FilterSidebar({ onFiltersChange, tags }: FilterSidebarProps) {
    const prevTagsRef = useRef<string[]>([]);
    const [filters, setFilters] = useState<FilterState>({
        type: [],
        tags: { include: [], exclude: [] }
    });

    const typeOptions = [
        { value: "playlist", label: "Playlist" },
        { value: "label", label: "Label" }
    ];

    const toggleType = (value: string) => {
        const exists = filters.type.includes(value);
        const next = exists ? filters.type.filter(v => v !== value) : [...filters.type, value];
        const nextFilters: FilterState = { ...filters, type: next };
        setFilters(nextFilters);
        onFiltersChange?.(nextFilters);
    };

    useEffect(() => {
        if (!tags || tags.length === 0) return;
        if (filters.tags.include.length === 0 && filters.tags.exclude.length === 0) {
            const next: FilterState = { type: filters.type, tags: { include: [...tags], exclude: [] } };
            setFilters(next);
            onFiltersChange?.(next);
        }
    }, [tags]);

    useEffect(() => {
        if (!tags) return;

        if (filters.tags.include.length === 0 && filters.tags.exclude.length === 0) {
            prevTagsRef.current = tags;
            return;
        }

        const prevTags = prevTagsRef.current;
        const prevSet = new Set(prevTags);
        const currSet = new Set(tags);

        const added = tags.filter(t => !prevSet.has(t));

        setFilters(prev => {
            const nextExclude = prev.tags.exclude.filter(t => currSet.has(t));
            const excludeSet = new Set(nextExclude);

            const nextIncludeBase = prev.tags.include.filter(t => currSet.has(t));

            const nextInclude = Array.from(
                new Set([
                    ...nextIncludeBase,
                    ...added.filter(t => !excludeSet.has(t))
                ])
            );

            const next: FilterState = {
                type: prev.type,
                tags: { include: nextInclude, exclude: nextExclude }
            };

            onFiltersChange?.(next);
            return next;
        });

        prevTagsRef.current = tags;
    }, [tags]);

    const nextTagState = (tag: string) => {
        const { include, exclude } = filters.tags;
        if (include.includes(tag)) {
            const next = { include: include.filter(t => t !== tag), exclude: [...exclude, tag] };
            const nextFilters: FilterState = { ...filters, tags: next };
            setFilters(nextFilters);
            onFiltersChange?.(nextFilters);
            return;
        }
        if (exclude.includes(tag)) {
            const next = { include: [...include, tag], exclude: exclude.filter(t => t !== tag) };
            const nextFilters: FilterState = { ...filters, tags: next };
            setFilters(nextFilters);
            onFiltersChange?.(nextFilters);
            return;
        }
        const next = { include: [...include, tag], exclude: [...exclude] };
        const nextFilters: FilterState = { ...filters, tags: next };
        setFilters(nextFilters);
        onFiltersChange?.(nextFilters);
    };

    const typeActive = (value: string) => filters.type.includes(value);

    const tagState = (tag: string) => {
        if (filters.tags.include.includes(tag)) return "include" as const;
        if (filters.tags.exclude.includes(tag)) return "exclude" as const;
        return "none" as const;
    };

    const TypeButton = ({ value, label }: { value: string; label: string }) => {
        const active = typeActive(value);
        return (
            <Button
                variant={active ? "secondary" : "ghost"}
                size="sm"
                className="w-full justify-start text-left h-8 px-3"
                onClick={() => toggleType(value)}
            >
                {label}
            </Button>
        );
    };

    const TagButton = ({ tag }: { tag: string }) => {
        const state = tagState(tag);
        const all = tags ?? [];
        const idx = Math.max(0, all.indexOf(tag));
        const base = getStableColor(tag, idx, all);
        const includeStyle = {
            backgroundColor: base.replace(')', ', 0.2)'),
            border: `1px solid ${base.replace(')', ', 0.3)')}`,
        };
        const isInclude = state === "include";

        return (
            <button
                type="button"
                onClick={() => nextTagState(tag)}
                title={isInclude ? "Include" : "Exclude"}
                className={
                    isInclude
                        ? "w-full justify-start text-left h-8 px-[9px] py-[4px] rounded-[4px] text-sm text-foreground transition-colors duration-200"
                        : "w-full justify-start text-left h-8 px-0 py-0 rounded-none text-sm text-foreground"
                }
                style={isInclude ? includeStyle : undefined}
            >
                {tag}
            </button>
        );
    };

    const hasAnyTags = filters.tags.include.length > 0 || filters.tags.exclude.length > 0;
    const excCount = filters.tags.exclude.length;
    const showExcludeBadge = excCount > 0;

    return (
        <div className="w-[12rem] bg-sidebar flex flex-col h-full">
            <div className="shrink-0 p-4 border-b border-sidebar-border">
                <h2 className="text-lg font-semibold text-sidebar-foreground">Filters</h2>
            </div>

            <div className="flex-1 overflow-y-auto p-4">
                <Accordion type="multiple" className="w-full">
                    <AccordionItem value="type" className="border-sidebar-border">
                        <AccordionTrigger className="text-base font-bold hover:text-sidebar-primary py-3 px-0">
                            <div className="flex items-center gap-2">
                                <span>Type</span>
                                {filters.type.length > 0 && (
                                    <span className="bg-primary/50 text-sidebar-primary-foreground text-xs px-2 py-0.5 rounded-full">
                    {filters.type.length}
                  </span>
                                )}
                            </div>
                        </AccordionTrigger>
                        <AccordionContent className="pt-2 pb-4">
                            <div className="max-h-60 overflow-y-auto pr-1 space-y-1">
                                {typeOptions.map((option) => (
                                    <TypeButton key={option.value} value={option.value} label={option.label} />
                                ))}
                            </div>
                        </AccordionContent>
                    </AccordionItem>

                    <AccordionItem value="tags" className="border-sidebar-border">
                        <AccordionTrigger className="text-base font-bold hover:text-sidebar-primary py-3 px-0">
                            <div className="flex items-center gap-2">
                                <span>Tags</span>
                                {showExcludeBadge && (
                                    <span className="bg-red-500/50 text-white/80 text-xs px-2 py-0.5 rounded-full">
                                        −{excCount}
                                    </span>
                                )}
                            </div>
                        </AccordionTrigger>
                        <AccordionContent className="pt-2 pb-4">
                            <div className="max-h-80 overflow-y-auto pr-1 space-y-1">
                                {(tags ?? []).map((tag) => (
                                    <TagButton key={tag} tag={tag} />
                                ))}
                            </div>
                        </AccordionContent>
                    </AccordionItem>
                </Accordion>
            </div>

            {(filters.type.length > 0 || hasAnyTags) && (
                <div className="shrink-0 border-t border-sidebar-border p-3">
                    <Button
                        variant="outline"
                        size="sm"
                        className="w-full border-sidebar-border text-sidebar-foreground hover:bg-sidebar-accent"
                        onClick={() => {
                            const next: FilterState = { type: [], tags: { include: [...(tags ?? [])], exclude: [] } };
                            setFilters(next);
                            onFiltersChange?.(next);
                        }}
                    >
                        Clear All Filters
                    </Button>
                </div>
            )}
        </div>
    );
}
