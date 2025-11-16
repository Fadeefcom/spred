// Accounts.tsx
import { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { useToast } from '@/hooks/use-toast';
import { Plus, Loader2 } from 'lucide-react';
import { apiFetch } from '@/hooks/apiFetch.tsx';
import { ConnectedAccount } from '@/components/profile/ConnectedAccount';
import { AccountStatus, ConnectedAccountType } from '@/types/Platforms';
import { SERVICES } from '@/constants/services.tsx';
import PipelineValidation from '@/components/PipelineValidation/PipelineValidation';

const Accounts = () => {
    const [loading, setLoading] = useState(true);
    const [actionLoading, setActionLoading] = useState<string | null>(null);
    const [accounts, setAccounts] = useState<ConnectedAccountType[]>([]);
    const [openPipeline, setOpenPipeline] = useState(false);
    const [selectedAccount, setSelectedAccount] = useState<ConnectedAccountType | null>(null);
    const { toast } = useToast();

    useEffect(() => {
        loadAccounts();
    }, []);

    const loadAccounts = async () => {
        try {
            setLoading(true);
            const res = await apiFetch(`${SERVICES.USER}/user/accounts`, { method: 'GET' });
            const base = (await res.json()) as ConnectedAccountType[];
            console.log(base);
            setAccounts(base);
        } finally {
            setLoading(false);
        }
    };

    const handleRemoveAccount = async (accountId: string) => {
        const account = accounts.find(acc => acc.accountId === accountId);
        if (!account) return;

        try {
            const res = await apiFetch(`${SERVICES.USER}/user/accounts/${accountId}`, { method: 'DELETE' });
            if (!res.ok) throw new Error('Failed to remove account');

            setAccounts(prev => prev.filter(acc => acc.accountId !== accountId));
            toast({ title: 'Account removed', description: 'Your account has been disconnected.' });
            window.gtag?.('event', 'account_removed', {
                platform: account.platform,
                account_id: accountId,
            });
        } catch (error) {
            toast({
                title: 'Error removing account',
                description: error instanceof Error ? error.message : 'Failed to remove account. Please try again.',
                variant: 'destructive',
            });
        }
    };

    const openNewAccountPipeline = () => {
        setSelectedAccount(null);
        setOpenPipeline(true);
    };

    const openExistingAccountPipeline = (acc: ConnectedAccountType) => {
        setSelectedAccount(acc);
        setOpenPipeline(true);
    };

    return (
        <div className="container mx-auto p-6 space-y-8">
            <div className="flex items-center justify-between mb-8">
                <div>
                    <h1 className="text-3xl font-bold text-foreground">Accounts</h1>
                    <p className="text-muted-foreground mt-2">Manage your connected music platform accounts and verification status.</p>
                </div>

                <Button onClick={openNewAccountPipeline} disabled={actionLoading === 'add'} className="transition-opacity gap-2">
                    <Plus className="h-4 w-4" />
                    Add New Account
                </Button>
            </div>

            {loading ? (
                <div className="flex items-center justify-center py-12">
                    <Loader2 className="h-8 w-8 animate-spin text-primary" />
                </div>
            ) : accounts.length === 0 ? (
                <div className="text-center py-12">
                    <div className="bg-gradient-card rounded-2xl shadow-soft p-8 max-w-md mx-auto">
                        <h3 className="text-xl font-semibold mb-2">No accounts connected</h3>
                        <p className="text-muted-foreground mb-6">Connect your music platform accounts to start managing your curator presence.</p>
                        <Button onClick={openNewAccountPipeline} disabled={actionLoading === 'add'} className="transition-opacity gap-2">
                            <Plus className="h-4 w-4" />
                            Connect Your First Account
                        </Button>
                    </div>
                </div>
            ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                    {accounts.map(account => (
                        <ConnectedAccount
                            key={account.accountId}
                            account={account}
                            onVerify={() => openExistingAccountPipeline(account)}
                            onRemove={handleRemoveAccount}
                            onContinue={() => openExistingAccountPipeline(account)}
                            loading={actionLoading === account.accountId}
                        />
                    ))}
                </div>
            )}

            <PipelineValidation
                open={openPipeline}
                onClose={async () => { try {  loadAccounts(); } finally { setOpenPipeline(false); } }}
                onCompleted={async () => { await loadAccounts(); }}
                status={selectedAccount?.status ?? "PlatformSelect"}
                initialAccount={selectedAccount ? { platform: selectedAccount.platform, accountId: selectedAccount.accountId, status: selectedAccount.status } : undefined}
            />
        </div>
    );
};

export default Accounts;
