export interface PlaylistSubmission {
  id: string;
  playlistId: string;
  artistId: string;
  trackId: string;
  artistName: string;
  trackTitle: string;
  trackUrl: string;
  submittedAt: Date;
  status: 'pending' | 'accepted' | 'declined';
  message?: string;
  artistImage?: string;
  genre?: string;
  followers?: number;
  review?: string;
}