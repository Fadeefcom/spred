namespace TrackService.Models.DTOs
{
    /// <summary>
    /// Represents the metadata information of a track.
    /// </summary>
    public sealed record AnalyzeTrackDto
    {
        /// <summary>
        /// Gets the duration of the track.
        /// </summary>
        public required TimeSpan Duration { get; init; }

        /// <summary>
        /// Gets the bitrate of the track in bits per second.
        /// </summary>
        public required uint Bitrate { get; init; }

        /// <summary>
        /// Gets the sample rate of the track in samples per second.
        /// </summary>
        public required uint SampleRate { get; init; }

        /// <summary>
        /// Gets the number of audio channels in the track.
        /// </summary>
        public required ushort Channels { get; init; }

        /// <summary>
        /// Gets the codec used for encoding the track.
        /// </summary>
        public required string Codec { get; init; }

        /// <summary>
        /// Gets the beats per minute (BPM) of the track.
        /// </summary>
        public required ushort Bpm { get; init; }
        
        /// <summary>
        /// File path for audio file
        /// </summary>
        public required string FilePath { get; init; }
    }
}
