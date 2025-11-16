
export interface Track {
  id?: string;
  spredUserId: string;
  primaryId: string;
  title: string;
  description?: string;
  duration: string;
  bitrate: number;
  sampleRate: number;
  channels: number;
  codec: string;
  bpm: number;
  playCount: number;
  commentsCount: number;
  likesCount: number;
  genre?: string;
  imageUrl?: string;
  trackUrl?: string;
  published: string;
  addedAt: string;
  updateAt: string;
  sourceType: SourceType;
  artists: Artist[];
  popularity?: number;
  album?: Album;
  hash?: string;
}

export interface Artist {
  primaryId: string;
  name: string;
}

export interface Album {
  id: string;
  title: string;
}

export enum SourceType {
  Local = 0,
  Spotify = 1,
  AppleMusic = 2,
  YouTube = 3
}

export interface TrackCreate
{
  Title: string;
  Description: string;
  TrackUrl: string;
}


export interface TrackAnalysis {
  genres: string[];
  moods: string[];
  tempo: number;
  key: string;
  vibe: string;
  similarArtists: string[];
}
