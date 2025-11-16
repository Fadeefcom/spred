import { useMemo } from 'react';

// Stable color palette for tags using semantic tokens
const TAG_COLORS = [
  'hsl(220, 70%, 50%)', // Blue
  'hsl(280, 60%, 50%)', // Purple
  'hsl(340, 70%, 50%)', // Pink
  'hsl(25, 70%, 50%)',  // Orange
  'hsl(160, 60%, 40%)', // Teal
  'hsl(45, 70%, 50%)',  // Yellow
  'hsl(120, 50%, 45%)', // Green
];

// Generate a stable hash from string
const hashString = (str: string): number => {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    const char = str.charCodeAt(i);
    hash = ((hash << 5) - hash) + char;
    hash = hash & hash; // Convert to 32-bit integer
  }
  return Math.abs(hash);
};

// Get stable color based on tag content
export const getStableColor = (tag: string, index: number, allTags: string[]): string => {
  const hash = hashString(tag.toLowerCase().trim());
  let colorIndex = hash % TAG_COLORS.length;

  // Avoid same color as previous tag if possible
  if (index > 0 && allTags.length > 1) {
    const prevHash = hashString(allTags[index - 1].toLowerCase().trim());
    const prevColorIndex = prevHash % TAG_COLORS.length;

    if (colorIndex === prevColorIndex && TAG_COLORS.length > 1) {
      colorIndex = (colorIndex + 1) % TAG_COLORS.length;
    }
  }

  return TAG_COLORS[colorIndex];
};

interface TagsBlockProps {
  tags: string[];
}

export const TagsBlock = ({ tags }: TagsBlockProps) => {
  // Stabilize the tags array to prevent unnecessary recalculations
  const stableTags = useMemo(() =>
          Array.isArray(tags) ? tags.filter(tag => tag && tag.trim()) : []
      , [tags]);

  const coloredTags = useMemo(() => {
    return stableTags.map((tag, index) => ({
      tag: tag.trim(),
      color: getStableColor(tag.trim(), index, stableTags)
    }));
  }, [stableTags]);

  if (!coloredTags.length) return null;

  return (
      <div className="flex flex-wrap gap-[9px] max-w-[300px] mt-3">
        {coloredTags.map(({ tag, color }) => (
            <span
                key={tag}
                className="text-sm text-foreground px-[9px] py-[4px] rounded-[4px] cursor-default select-none transition-colors duration-200"
                style={{
                  backgroundColor: `${color.replace(')', ', 0.2)')}`,
                  border: `1px solid ${color.replace(')', ', 0.3)')}`
                }}
            >
          {tag}
        </span>
        ))}
      </div>
  );
};