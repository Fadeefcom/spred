interface FitBadgeProps {
    score?: string;
    className?: string;
    size?: 'xs' | 'sm' | 'md';
}

export const FitBadge = ({ score, className = '', size = 'xs' }: FitBadgeProps) => {
    let color = "bg-gray-500 text-white";

    if (score == "Strong fit") {
        color = "bg-green-500 text-white";
    } else if (score == "Moderate fit") {
        color = "bg-spred-yellow text-black";
    }

    const sizeMap = {
        xs: "text-xs px-2 py-1",
        sm: "text-sm px-3 py-1.5",
        md: "text-base px-4 py-2"
    };

    return (
        <span className={`tracking-wide rounded-md font-bold ${color} ${sizeMap[size]} ${className}`}>
          {score}
        </span>
    );
};