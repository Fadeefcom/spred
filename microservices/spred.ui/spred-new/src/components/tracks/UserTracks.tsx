import React from 'react';
import TrackCard from './TrackCard';
import { Track } from '@/types/Track';
import { Music } from 'lucide-react';

interface UserTracksProps {
    tracks?: Track[];
    onDeleted?: (id: string) => void;
}

const UserTracks: React.FC<UserTracksProps> = ({ tracks, onDeleted }) => {
  if (!tracks || tracks.length === 0) {
    return (
      <div className="text-center py-20">
        <Music className="mx-auto mb-4 text-muted-foreground" size={48} />
        <h3 className="text-xl font-medium mb-2">No tracks uploaded yet</h3>
        <p className="text-muted-foreground">Upload your first track to get recommendations</p>
      </div>
    );
  }

  return (
    <div>
      <h2 className="text-2xl font-bold mb-6 flex items-center">
        <Music className="mr-2" size={24} />
        Your Tracks
      </h2>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {tracks.map((track) => (
          <TrackCard key={track.id} track={track} onDeleted={onDeleted} />
        ))}
      </div>
    </div>
  );
};

export default UserTracks;
