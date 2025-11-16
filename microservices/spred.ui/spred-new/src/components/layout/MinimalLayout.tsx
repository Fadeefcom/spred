import React, { ReactElement, useEffect, useRef, useState } from "react";
import { cn } from "@/lib/utils.ts";
import { Link, useLocation } from "react-router-dom";
import MinimalFooter from "@/components/layout/MinimalFooter.tsx";

export interface ILayoutProps {
    children: ReactElement;
}

const MinimalLayout = (props: ILayoutProps) => {
    const [isScrolled, setIsScrolled] = useState(false);
    const location = useLocation();
    const hideFooter = location.pathname === "/login";
    const mainRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        const main = mainRef.current;
        if (!main) return;

        const handleScroll = () => {
            setIsScrolled(main.scrollTop > 20);
        };

        main.addEventListener("scroll", handleScroll);
        return () => main.removeEventListener("scroll", handleScroll);
    }, []);

    return (
        <div
            className={cn(
                "flex flex-col w-full h-screen overflow-hidden",
                hideFooter && "bg-spred-black text-spred-white"
            )}
        >
            <header
                className={cn(
                    "fixed top-0 left-0 right-0 z-50 transition-all duration-300 px-4 md:px-8 py-6",
                    isScrolled
                        ? "bg-spred-white text-spred-black shadow-md"
                        : "bg-spred-black text-spred-white"
                )}
            >
                <div className="max-w-7xl mx-auto flex items-center justify-between">
                    <Link to="/" className="text-2xl font-bold">
                        Spred
                    </Link>
                </div>
            </header>

            <main ref={mainRef} className="flex-1 overflow-y-auto pt-[88px]">
                <div className="flex flex-col min-h-full">
                    <section className="flex-1">{props.children}</section>
                    {!hideFooter && <MinimalFooter />}
                </div>
            </main>
        </div>
    );
};

export default MinimalLayout;
