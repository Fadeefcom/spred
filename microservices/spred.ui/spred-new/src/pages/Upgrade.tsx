import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Check, Sparkles, Zap, Crown } from "lucide-react";
import { cn } from "@/lib/utils";
import {useAuth} from "@/components/authorization/AuthProvider.tsx";
import {SERVICES} from "@/constants/services.tsx";
import {apiFetch} from "@/hooks/apiFetch.tsx";

const Upgrade = () => {
    const { user } = useAuth();

    const plans = [
        {
            id: "free",
            name: "Free",
            price: "$0",
            period: "forever",
            description: "Perfect for getting started",
            features: [
                "5 uploads per month",
                "Basic opportunity matching",
                "3 free opportunities",
                "Standard support",
                "Basic analytics",
            ],
            notIncluded: [
                "Unlimited uploads",
                "Premium opportunities",
                "Advanced analytics",
                "Priority support",
            ],
            current: !user?.subscription?.isActive,
            buttonText: !user?.subscription?.isActive ? "Current Plan" : "Downgrade to Free",
            buttonVariant: "outline" as const,
            icon: Sparkles,
        },
        {
            id: "premium-monthly",
            name: "Pro",
            price: "$6",
            period: "per month",
            description: "For serious artists ready to scale",
            features: [
                "Unlimited uploads",
                "Advanced opportunity matching",
                "50+ premium opportunities",
                "Priority support",
                "Advanced analytics & insights",
                "A&R and industry contacts",
                "Festival booking access",
                "Radio station features",
                "Custom branding",
            ],
            notIncluded: [],
            current: user?.subscription?.isActive,
            buttonText: user?.subscription?.isActive ? "Current Plan" : "Upgrade to Pro",
            buttonVariant: "default" as const,
            icon: Crown,
            popular: true,
        },
    ];

    const handleCheckout = async (planItem) => {
        const res = await apiFetch(`${SERVICES.SUBSCRIPTION}/checkout`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                plan: planItem.id
            })
        });

        if (!res.ok) return;

        const data = await res.json();

        window.location.href = `https://checkout.stripe.com/pay/${data.sessionId}`;
    };


    return (
        <div className="h-[calc(100vh-128px)] overflow-y-auto bg-background">
            <main className="container mx-auto px-4 py-8 space-y-12 animate-fade-in">
                {/* Hero Section */}
                <div className="text-center space-y-4 max-w-3xl mx-auto">
                    <h1 className="text-4xl md:text-5xl font-bold text-foreground">
                        Choose Your <span className="text-gradient">Path to Success</span>
                    </h1>
                    <p className="text-lg text-muted-foreground">
                        Get the tools you need to take your music career to the next level
                    </p>
                </div>

                {/* Current Plan Indicator */}
                {!user?.subscription?.isActive && (
                    <div className="max-w-4xl mx-auto">
                        <Card className="p-4 bg-muted border-muted-foreground/20">
                            <div className="flex items-center justify-between">
                                <div className="flex items-center gap-3">
                                    <div className="h-2 w-2 rounded-full bg-muted-foreground animate-pulse" />
                                    <p className="text-sm text-muted-foreground">
                                        You're currently on the <span className="font-semibold text-foreground">Free Plan</span>
                                    </p>
                                </div>
                            </div>
                        </Card>
                    </div>
                )}

                {/* Pricing Cards */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-8 max-w-5xl mx-auto">
                    {plans.map((planItem, i) => (
                        <Card
                            key={planItem.name}
                            className={cn(
                                "relative p-8 border border-border shadow-card rounded-xl bg-card/70 backdrop-blur-xs",
                                "transform-gpu will-change-transform",
                                "duration-300 ease-out",
                                "hover:scale-[1.04] hover:shadow-glass-strong hover:border-primary/40",
                                "overflow-hidden"
                            )}
                            style={{ animationDelay: `${i * 100}ms` }}
                        >
                            <div className="space-y-6">
                                {/* Header */}
                                <div className="space-y-4">
                                    <div className="flex items-center justify-between">
                                        <planItem.icon className={cn(
                                            "h-8 w-8",
                                            planItem.popular ? "text-primary" : "text-muted-foreground"
                                        )} />
                                        {planItem.current && (
                                            <Badge variant="secondary" className="text-xs">
                                                Active
                                            </Badge>
                                        )}
                                    </div>
                                    <div>
                                        <h3 className="text-2xl font-bold text-foreground">{planItem.name}</h3>
                                        <p className="text-sm text-muted-foreground mt-1">{planItem.description}</p>
                                    </div>
                                    <div className="flex items-baseline gap-2">
                                        <span className="text-4xl font-bold text-foreground">{planItem.price}</span>
                                        <span className="text-muted-foreground">/ {planItem.period}</span>
                                    </div>
                                </div>

                                {/* Features */}
                                <div className="space-y-3">
                                    {planItem.features.map((feature) => (
                                        <div key={feature} className="flex items-start gap-3">
                                            <Check className="h-5 w-5 text-success flex-shrink-0 mt-0.5" />
                                            <span className="text-sm text-foreground">{feature}</span>
                                        </div>
                                    ))}
                                    {planItem.notIncluded.map((feature) => (
                                        <div key={feature} className="flex items-start gap-3 opacity-40">
                                            <div className="h-5 w-5 flex-shrink-0 mt-0.5">
                                                <div className="h-0.5 w-3 bg-muted-foreground rounded" />
                                            </div>
                                            <span className="text-sm text-muted-foreground line-through">{feature}</span>
                                        </div>
                                    ))}
                                </div>

                                {/* CTA Button */}
                                <Button
                                    variant={planItem.buttonVariant}
                                    className="w-full"
                                    size="lg"
                                    onClick={() => handleCheckout(planItem)}
                                    disabled={planItem.current}
                                >
                                    {planItem.buttonText}
                                </Button>
                            </div>
                        </Card>
                    ))}
                </div>

                {/* Feature Comparison Table */}
                <div className="max-w-4xl mx-auto space-y-4">
                    <h2 className="text-2xl font-bold text-center text-foreground">
                        Full Feature Comparison
                    </h2>

                    <Card className="p-6 shadow-card border-border overflow-x-auto">
                        <table className="w-full">
                            <thead>
                            <tr className="border-b border-border">
                                <th className="text-left py-4 px-4 text-foreground font-semibold">Feature</th>
                                <th className="text-center py-4 px-4 text-foreground font-semibold">Free</th>
                                <th className="text-center py-4 px-4 text-foreground font-semibold">Pro</th>
                            </tr>
                            </thead>
                            <tbody className="divide-y divide-border">
                            {[
                                { feature: "Monthly uploads", free: "5", pro: "Unlimited" },
                                { feature: "Opportunities", free: "3 basic", pro: "50+ premium" },
                                { feature: "Analytics", free: "Basic", pro: "Advanced" },
                                { feature: "Support", free: "Standard", pro: "Priority" },
                                { feature: "Industry contacts", free: "—", pro: "✓" },
                                { feature: "Festival bookings", free: "—", pro: "✓" },
                                { feature: "Radio features", free: "—", pro: "✓" },
                                { feature: "Custom branding", free: "—", pro: "✓" },
                            ].map((row) => (
                                <tr key={row.feature} className="hover:bg-secondary/50 transition-smooth">
                                    <td className="py-4 px-4 text-foreground">{row.feature}</td>
                                    <td className="py-4 px-4 text-center text-muted-foreground">{row.free}</td>
                                    <td className="py-4 px-4 text-center text-primary font-medium">{row.pro}</td>
                                </tr>
                            ))}
                            </tbody>
                        </table>
                    </Card>
                </div>

                {/* FAQ or Additional Info */}
                <div className="max-w-3xl mx-auto text-center space-y-4">
                    <h3 className="text-xl font-semibold text-foreground">Questions?</h3>
                    <p className="text-muted-foreground">
                        Upgrade or downgrade anytime. No hidden fees. Cancel whenever you want.
                    </p>
                </div>
            </main>
        </div>
    );
};

export default Upgrade;