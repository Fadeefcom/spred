import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { ExternalLink, CheckCircle, AlertCircle, Clock, Link2Off, ChevronRight } from 'lucide-react';
import { cn } from '@/lib/utils';
import {
    ConnectedAccountType,
    getPlatformInfo,
    AccountStatus,
    PlatformLabels,
    getPlatformProfileUrl,
    platforms
} from '@/types/Platforms';

interface ConnectedAccountProps {
    account: ConnectedAccountType;
    onVerify?: (accountId: string) => void;
    onRemove?: (accountId: string) => void;
    onContinue?: (account: ConnectedAccountType) => void;
    loading?: boolean;
}

const STATUS_LABEL: Record<AccountStatus, string> = {
    Pending: 'Pending',
    TokenIssued: 'Token issued',
    ProofSubmitted: 'Proof submitted',
    Verified: 'Verified',
    Error: 'Error',
    Deleted: 'Deleted',
    PlatformSelect: ''
};

function statusIcon(status?: AccountStatus) {
    switch (status) {
        case 'Verified': return <CheckCircle className="h-4 w-4" />;
        case 'Pending':
        case 'TokenIssued':
        case 'ProofSubmitted': return <Clock className="h-4 w-4" />;
        case 'Error':
        case 'Deleted':
        default: return <AlertCircle className="h-4 w-4" />;
    }
}

function statusColor(status?: AccountStatus) {
    switch (status) {
        case 'Verified': return 'bg-emerald-600 text-white';
        case 'Pending':
        case 'TokenIssued':
        case 'ProofSubmitted': return 'bg-amber-600 text-white';
        case 'Error': return 'bg-sky-700 text-white';
        case 'Deleted': return 'bg-muted text-muted-foreground';
        default: return 'bg-muted text-muted-foreground';
    }
}

export const ConnectedAccount = ({ account, onVerify, onRemove, onContinue, loading = false }: ConnectedAccountProps) => {
    const platform = getPlatformInfo(account.platform);
    const label = PlatformLabels[account.platform] ?? 'Platform';
    const profileUrl = account.profileUrl;
    const platformInfo = platforms.find(
        p => p.id.toLowerCase() === account.platform.toLowerCase()
    );
    const Icon = platformInfo?.icon;

    console.log(profileUrl)
    const isVerified = account.status === 'Verified';
    const canRestart = account.status === 'Error';
    const isWaiting = account.status === 'Pending' || account.status === 'TokenIssued' || account.status === 'ProofSubmitted';


    const handleVerifyClick = () => {
        window.gtag?.('event', 'verify_account_clicked', { platform: account.platform, account_id: account.accountId });
        onVerify?.(account.accountId);
    };

    const handleRemoveClick = () => {
        window.gtag?.('event', 'remove_account_clicked', { platform: account.platform, account_id: account.accountId });
        onRemove?.(account.accountId);
    };

    const handleContinueClick = () => {
        window.gtag?.('event', 'continue_account_clicked', { platform: account.platform, account_id: account.accountId });
        onContinue?.(account);
    }

    return (
        <Card className="bg-card border border-border shadow-sm hover:shadow-lg hover:shadow-spred-yellowdark/30 transition-shadow">
            <CardContent className="p-6">
                <div className="flex items-start justify-between mb-4">
                    <div className="flex items-center gap-3">
                        <div className={cn('w-10 h-10 rounded-full flex items-center justify-center text-white', platform?.bgClass)}>
                            {Icon ? <Icon className="h-5 w-5" /> : <span className="text-sm font-semibold">{platform?.name.charAt(0)}</span>}
                        </div>
                        <div>
                            <h3 className="font-semibold text-foreground leading-none">{platform?.name}</h3>
                            <div className="flex items-center gap-2 mt-1">
                                <a
                                    href={profileUrl}
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    className="text-sm text-spred-white/40 hover:text-spred-yellowdark transition-colors animated-underline inline-flex items-center justify-center gap-1"
                                    onClick={() => window.gtag?.('event', 'external_profile_clicked', { platform: account.platform, account_id: account.accountId })}
                                >
                                    {label}
                                    <ExternalLink className="h-3 w-3" />
                                </a>
                            </div>
                        </div>
                    </div>

                    <Button
                        variant="ghost"
                        size="sm"
                        onClick={handleRemoveClick}
                        className="text-muted-foreground hover:text-destructive h-8 w-8 p-0"
                    >
                        <Link2Off className="h-4 w-4" />
                    </Button>
                </div>

                <div className="flex items-center justify-between">
                    <Badge variant="outline" className={cn('flex items-center gap-1.5', statusColor(account.status))}>
                        {statusIcon(account.status)}
                        {STATUS_LABEL[account.status ?? 'Pending']}
                    </Badge>

                    {canRestart ? (
                        <Button
                            size="sm"
                            onClick={handleVerifyClick}
                            disabled={loading}
                            className="gap-1"
                        >
                            {loading ? 'Loading…' : <span className="inline-flex items-center gap-1">Restart verification <ChevronRight className="h-4 w-4" /></span>}
                        </Button>
                    ) : null}

                    {isWaiting ? (
                        <Button
                            size="sm"
                            onClick={handleContinueClick}
                            disabled={loading}
                            className="gap-1"
                        >
                            {loading
                                ? 'Loading…'
                                : (
                                    <span className="inline-flex items-center gap-1">
                                      Continue verification
                                      <ChevronRight className="h-4 w-4" />
                                    </span>
                                )}
                        </Button>
                    ) : null}
                </div>

                <div className="mt-2 text-xs text-muted-foreground">
                    Connected {new Date(account.connectedAt).toLocaleDateString()}
                </div>
            </CardContent>
        </Card>
    );
};
