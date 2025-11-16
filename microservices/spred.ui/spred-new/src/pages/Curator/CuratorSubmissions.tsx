import {useEffect, useState} from "react";
import { Check, X, Play, ExternalLink, Clock, User, MessageSquare } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card.tsx";
import { Button } from "@/components/ui/button.tsx";
import { Badge } from "@/components/ui/badge.tsx";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog.tsx";
import { Textarea } from "@/components/ui/textarea.tsx";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar.tsx";
import { useToast } from "@/hooks/use-toast.ts";
import { PlaylistSubmission } from "@/types/PlaylistSubmission.ts";
import { apiFetch } from "@/hooks/apiFetch";
import {SERVICES} from "@/constants/services.tsx";

const CuratorSubmissions = () => {
    const { toast } = useToast();
    const [reviewText, setReviewText] = useState<string>("");
    const [selectedSubmission, setSelectedSubmission] = useState<PlaylistSubmission | null>(null);
    const [showReviewDialog, setShowReviewDialog] = useState<boolean>(false);
    const [pendingAction, setPendingAction] = useState<'accepted' | 'declined' | null>(null);
    const [submissions, setSubmissions] = useState<PlaylistSubmission[]>([]);
    const [loading, setLoading] = useState<boolean>(true);

    useEffect(() => {
        const fetchSubmissions = async () => {
            try {
                const response = await apiFetch(`${SERVICES.SUBMISSIONS}`, { method: "GET" });
                setSubmissions(await response.json());
            } catch (err) {
                toast({ title: "Error", description: "Failed to load submissions" });
            } finally {
                setLoading(false);
            }
        };

        fetchSubmissions();
    }, [toast]);

    const handleSubmissionAction = async (
        submissionId: string,
        action: "accepted" | "declined",
        review?: string
    ) => {
        try {
            const submission = submissions.find(s => s.id === submissionId);
            if (!submission) return;

            await apiFetch(
                `${SERVICES.SUBMISSIONS}/${submission.playlistId}/${submission.id}/status`,
                {
                    method: "PATCH",
                    body: JSON.stringify({
                        newStatus: action,
                        artistId: submission.artistId
                    })
                }
            );

            setSubmissions(submissions.map(sub =>
                sub.id === submissionId ? { ...sub, status: action, review } : sub
            ));

            toast({
                title: "Success",
                description: `Submission ${action}${review ? " with review" : ""}`
            });
        } catch {
            toast({ title: "Error", description: "Failed to update submission" });
        } finally {
            setShowReviewDialog(false);
            setReviewText("");
            setSelectedSubmission(null);
            setPendingAction(null);
        }
    };

    const openReviewDialog = (submission: PlaylistSubmission, action: 'accepted' | 'declined') => {
        setSelectedSubmission(submission);
        setPendingAction(action);
        setShowReviewDialog(true);
    };

    const submitWithReview = () => {
        if (selectedSubmission && pendingAction) {
            handleSubmissionAction(selectedSubmission.id, pendingAction, reviewText);
        }
    };

    const getSubmissionsByPlaylist = () => {
        const submissionsByPlaylist = submissions.reduce((acc, submission) => {
            if (!acc[submission.playlistId]) {
                acc[submission.playlistId] = [];
            }
            acc[submission.playlistId].push(submission);
            return acc;
        }, {} as Record<string, PlaylistSubmission[]>);

        // Sort submissions within each playlist by date
        Object.keys(submissionsByPlaylist).forEach(playlistId => {
            submissionsByPlaylist[playlistId].sort((a, b) => b.submittedAt.getTime() - a.submittedAt.getTime());
        });

        return submissionsByPlaylist;
    };

    const getStatusBadge = (status: string) => {
        switch (status) {
            case 'pending':
                return <Badge variant="outline" className="text-yellow-600 border-yellow-600">Pending</Badge>;
            case 'accepted':
                return <Badge variant="outline" className="text-green-600 border-green-600">Accepted</Badge>;
            case 'declined':
                return <Badge variant="outline" className="text-red-600 border-red-600">Declined</Badge>;
            default:
                return <Badge variant="outline">{status}</Badge>;
        }
    };

    const getStatusCounts = (submissions: PlaylistSubmission[]) => {
        return submissions.reduce((acc, sub) => {
            acc[sub.status] = (acc[sub.status] || 0) + 1;
            return acc;
        }, {} as Record<string, number>);
    };

    const renderSubmissionCard = (submission: PlaylistSubmission) => (
        <Card key={submission.id} className="group hover:shadow-lg transition-shadow">
            <CardHeader>
                <div className="flex justify-between items-start">
                    <div className="flex items-center gap-4 flex-1">
                        <Avatar className="h-12 w-12">
                            <AvatarImage src={submission.artistImage} alt={submission.artistName} />
                            <AvatarFallback>{submission.artistName.charAt(0)}</AvatarFallback>
                        </Avatar>
                        <div className="flex-1">
                            <div className="flex items-center gap-3">
                                <CardTitle className="text-lg">{submission.trackTitle}</CardTitle>
                                {getStatusBadge(submission.status)}
                            </div>
                            <CardDescription className="mt-1">
                                by {submission.artistName} • {submission.submittedAt.toLocaleDateString()}
                            </CardDescription>
                            <div className="flex items-center gap-4 mt-2 text-sm text-muted-foreground">
                                <span>{submission.genre}</span>
                                <span>{submission.followers?.toLocaleString()} followers</span>
                            </div>
                            {submission.message && (
                                <p className="text-sm text-muted-foreground mt-2 italic">"{submission.message}"</p>
                            )}
                        </div>
                    </div>
                </div>
            </CardHeader>

            <CardContent>
                <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                        <Button
                            variant="outline"
                            size="sm"
                            onClick={() => window.open(submission.trackUrl, '_blank')}
                        >
                            <Play className="h-4 w-4 mr-2" />
                            Listen
                        </Button>
                        <Button
                            variant="outline"
                            size="sm"
                            onClick={() => window.open(`/artist/${submission.artistId}`, '_blank')}
                        >
                            <User className="h-4 w-4 mr-2" />
                            View Artist
                        </Button>
                        <Button
                            variant="outline"
                            size="sm"
                            onClick={() => window.open(submission.trackUrl, '_blank')}
                        >
                            <ExternalLink className="h-4 w-4 mr-2" />
                            View Track
                        </Button>
                    </div>

                    {submission.status === 'pending' && (
                        <div className="flex items-center gap-2">
                            <Button
                                variant="outline"
                                size="sm"
                                onClick={() => openReviewDialog(submission, 'declined')}
                                className="text-red-600 hover:text-red-700 border-red-200 hover:border-red-300"
                            >
                                <X className="h-4 w-4 mr-2" />
                                Decline
                            </Button>
                            <Button
                                size="sm"
                                onClick={() => openReviewDialog(submission, 'accepted')}
                                className="text-green-600 hover:text-green-700 border-green-200 hover:border-green-300"
                            >
                                <Check className="h-4 w-4 mr-2" />
                                Accept
                            </Button>
                        </div>
                    )}
                </div>
            </CardContent>
        </Card>
    );

    const submissionsByPlaylist = getSubmissionsByPlaylist();

    return (
        <div className="container mx-auto p-6 space-y-8">
            <div className="flex justify-between items-center">
                <div>
                    <h1 className="text-3xl font-bold text-foreground">Submissions</h1>
                    <p className="text-muted-foreground mt-2">Review and manage playlist submissions from artists</p>
                </div>
            </div>

            {/* Submissions Grouped by Playlist */}
            {loading ? (
                <Card>
                    <CardContent className="flex items-center justify-center h-32">
                        <p className="text-muted-foreground">Loading submissions...</p>
                    </CardContent>
                </Card>
            ) : Object.keys(submissionsByPlaylist).length === 0 ? (
                <Card>
                    <CardContent className="flex items-center justify-center h-32">
                        <p className="text-muted-foreground">No submissions found</p>
                    </CardContent>
                </Card>
            ) : (
                Object.entries(submissionsByPlaylist).map(([playlistId, playlistSubmissions]) => {
                    const statusCounts = getStatusCounts(playlistSubmissions);

                    return (
                        <div key={playlistId} className="space-y-4">
                            <div className="flex items-center justify-between">
                                <div>
                                    <h2 className="text-2xl font-semibold text-foreground">
                                        Playlist {playlistId}
                                    </h2>
                                    <div className="flex items-center gap-4 mt-2">
                                        <div className="flex items-center gap-2 text-sm text-muted-foreground">
                                            <Clock className="h-4 w-4" />
                                            <span>Pending: {statusCounts.pending || 0}</span>
                                        </div>
                                        <div className="flex items-center gap-2 text-sm text-muted-foreground">
                                            <Check className="h-4 w-4" />
                                            <span>Accepted: {statusCounts.accepted || 0}</span>
                                        </div>
                                        <div className="flex items-center gap-2 text-sm text-muted-foreground">
                                            <X className="h-4 w-4" />
                                            <span>Declined: {statusCounts.declined || 0}</span>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <div className="space-y-4">
                                {playlistSubmissions.map((submission) => renderSubmissionCard(submission))}
                            </div>
                        </div>
                    );
                })
            )}

            {/* Review Dialog */}
            <Dialog open={showReviewDialog} onOpenChange={setShowReviewDialog}>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>
                            {pendingAction === 'accepted' ? 'Accept' : 'Decline'} Submission
                        </DialogTitle>
                        <DialogDescription>
                            {selectedSubmission && (
                                <>
                                    {pendingAction === 'accepted' ? 'Accept' : 'Decline'} "{selectedSubmission.trackTitle}" by {selectedSubmission.artistName}
                                </>
                            )}
                        </DialogDescription>
                    </DialogHeader>
                    <div className="space-y-4">
                        <div>
                            <label className="text-sm font-medium">Review (optional)</label>
                            <Textarea
                                placeholder={`Leave a ${pendingAction === 'accepted' ? 'positive' : ''} review for the artist...`}
                                value={reviewText}
                                onChange={(e) => setReviewText(e.target.value)}
                                className="mt-2"
                            />
                        </div>
                        <div className="flex justify-end gap-2">
                            <Button variant="outline" onClick={() => setShowReviewDialog(false)}>
                                Cancel
                            </Button>
                            <Button
                                onClick={submitWithReview}
                                className={pendingAction === 'accepted' ? 'text-green-600 hover:text-green-700' : 'text-red-600 hover:text-red-700'}
                            >
                                <MessageSquare className="h-4 w-4 mr-2" />
                                {pendingAction === 'accepted' ? 'Accept' : 'Decline'}
                            </Button>
                        </div>
                    </div>
                </DialogContent>
            </Dialog>
        </div>
    );
};

export default CuratorSubmissions;