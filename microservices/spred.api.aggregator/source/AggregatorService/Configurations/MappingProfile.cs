using System.Globalization;
using System.Text.Json;
using AggregatorService.Extensions;
using AggregatorService.Models;
using AutoMapper;
using Spred.Bus.Contracts;
using Spred.Bus.DTOs;

namespace AggregatorService.Configurations;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Mapping configurations for Spotify json to dto
        CreateMap<JsonElement, MetadataTracksDto>()
            .ForMember(d => d.Name, s =>
                s.MapFrom(mph => mph.TryGetValue("name").GetStringOrNull()))
            .ForMember(d => d.Description, s =>
                s.MapFrom(mph => mph.TryGetValue("description").GetStringOrNull()))
            .ForMember(d => d.Href, s => s.MapFrom(mph => mph.TryGetValue("href").GetStringOrNull()))
            .ForMember(d => d.PrimaryId, s =>
                s.MapFrom(mph => "spotify:playlist:" + mph.TryGetValue("id").GetStringOrNull()))
            .ForMember(d => d.TracksTotal, s =>
                s.MapFrom(mph => Convert.ToUInt32(mph.TryGetValue("tracks").TryGetValue("total").GetStringOrNull(),
                    CultureInfo.InvariantCulture)))
            .ForMember(d => d.Collaborative, s =>
                s.MapFrom(mph => mph.TryGetValue("collaborative").GetStringOrNull()))
            .ForMember(d => d.IsPublic, s =>
                s.MapFrom(mph => mph.TryGetValue("public").GetStringOrNull()))
            .ForMember(d => d.Tracks, opt =>
                opt.MapFrom((src, _, _, ctx) =>
                {
                    var items = src.TryGetValue("tracks")
                        .TryGetValue("items")
                        .EnumerateArraySafe();

                    var trackDtos = items
                        .Select(item =>
                        {
                            var wrapper = new SpotifyTrackWrapper { Data = item };
                            return ctx.Mapper.Map<TrackDto>(wrapper);
                        })
                        .Where(track => !string.IsNullOrWhiteSpace(track.PrimaryId))
                        .ToList();

                    return trackDtos;
                }))
            .ForMember(d => d.ImageUrl, s => s.MapFrom(mph => GetImageUrl(mph)))
            .ForMember(d => d.Status, s => s.MapFrom(_ => FetchStatus.FetchedPlaylist))
            .ForMember(d => d.ListenUrls, s => s.MapFrom(mph => MapDictUrls(mph.TryGetValue("external_urls"))))
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.SpredUserId, opt => opt.Ignore())
            .ForMember(d => d.SubmitUrls, opt => opt.Ignore())
            .ForMember(d => d.Followers, opt => opt.Ignore())
            .ForMember(d => d.SubmitEmail, opt => opt.Ignore())
            .ForMember(d => d.Type, opt => opt.Ignore())
            .ForMember(d => d.ChartmetricsId, opt => opt.Ignore())
            .ForMember(d => d.Tags, opt => opt.Ignore())
            .ForMember(d => d.OwnerPlatformName, opt => opt.Ignore())
            .ForMember(d => d.OwnerPlatformId, opt => opt.Ignore())
            .ForMember(d => d.UserPlatformId, opt => opt.Ignore())
            .ForMember(d => d.CatalogType, opt => opt.Ignore())
            .ForMember(d => d.ActiveRatio, opt => opt.Ignore())
            .ForMember(d => d.SuspicionScore, opt => opt.Ignore())
            .ForMember(d => d.Moods, opt => opt.Ignore())
            .ForMember(d => d.Activities, opt => opt.Ignore())
            .ForMember(d => d.SoundChartsId, opt => opt.Ignore())
            .ForMember(d => d.Country, opt => opt.Ignore())
            .ForMember(d => d.CityName, opt => opt.Ignore())
            .ForMember(d => d.CountryCode, opt => opt.Ignore())
            .ForMember(d => d.TimeZone, opt => opt.Ignore())
            .ForMember(d => d.SubmissionFormUrl, opt => opt.Ignore())
            .ForMember(d => d.SubmissionInstructions, opt => opt.Ignore())
            .ForMember(d => d.MusicRequirements, opt => opt.Ignore())
            .ForMember(d => d.SubmissionInfoUrl, opt => opt.Ignore())
            .ForMember(d => d.SubmissionFriendly, opt => opt.Ignore())
            .ForMember(d => d.PaymentType, opt => opt.Ignore())
            .ForMember(d => d.PaymentPrice, opt => opt.Ignore())
            .ForMember(d => d.PaymentDetails, opt => opt.Ignore())
            .ForMember(d => d.LocalizationScope, opt => opt.Ignore())
            .ForMember(d => d.LocalizationRegion, opt => opt.Ignore())
            .ForMember(d => d.LocalizationPriority, opt => opt.Ignore())
            .ForMember(d => d.AudienceScope, opt => opt.Ignore())
            .ForMember(d => d.BroadcastFormat, opt => opt.Ignore())
            .ForMember(d => d.CurationFrequency, opt => opt.Ignore())
            .ForMember(d => d.FeedbackRate, opt => opt.Ignore())
            .ForMember(d => d.Reach, opt => opt.Ignore());

        // Mapping configurations for JsonElement and ArtistDto
        var artistMap = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<JsonElement, ArtistDto>()
                .ForMember(d => d.PrimaryId, s =>
                    s.MapFrom(mph => "spotify:artist:" + mph.TryGetValue("id")))
                .ForMember(d => d.Name, s =>
                    s.MapFrom(mph => mph.TryGetValue("name")));
        });
        var mapper = new Mapper(artistMap);

        // Custom type converters
        CreateMap<string, DateTime>().ConvertUsing<DateTimeTypeConverter>();
        CreateMap<string, uint>().ConvertUsing<UIntTypeConverter>();

        // Mapping configurations for JsonElement and TrackDto
        CreateMap<SpotifyTrackWrapper, TrackDto>()
            .ForMember(d => d.Title, s =>
                s.MapFrom(mph => mph.Data.TryGetValue("track").TryGetValue("name").GetStringOrNull()))
            .ForPath(d => d.Album!.AlbumName, s =>
                s.MapFrom(mph =>
                    mph.Data.TryGetValue("track").TryGetValue("album").TryGetValue("name").GetStringOrNull()))
            .ForPath(d => d.Album!.AlbumReleaseDate, s =>
                s.MapFrom(mph =>
                    mph.Data.TryGetValue("track").TryGetValue("album").TryGetValue("release_date").GetStringOrNull()))
            .ForPath(d => d.Album!.ImageUrl, s => s.MapFrom(mph =>
                GetImageUrl(mph.Data.TryGetValue("track").TryGetValue("album"))))
            .ForMember(d => d.Artists, s =>
                s.MapFrom(mph =>
                    mapper.Map<List<ArtistDto>>(mph.Data.TryGetValue("track").TryGetValue("artists")
                        .EnumerateArraySafe())))
            .ForMember(d => d.PrimaryId, s =>
                s.MapFrom(mph => "spotify:track:" + mph.Data.TryGetValue("track").TryGetValue("id").GetStringOrNull()))
            .ForMember(d => d.Published, s =>
                s.MapFrom(mph =>
                    Convert.ToDateTime(mph.Data.TryGetValue("added_at").GetStringOrNull(), new DateTimeFormatInfo())))
            .ForPath(d => d.Album!.PrimaryId, s =>
                s.MapFrom(mph =>
                    "spotify:album:" + mph.Data.TryGetValue("track").TryGetValue("album").TryGetValue("id")
                        .GetStringOrNull()))
            .ForMember(d => d.Popularity, s =>
                s.MapFrom(mph =>
                    Convert.ToUInt32(mph.Data.TryGetValue("track").TryGetValue("popularity").GetStringOrNull(),
                        CultureInfo.InvariantCulture)))
            .ForMember(d => d.SourceType, s => s.MapFrom(_ => SourceType.Spotify))
            .ForPath(d => d.TrackUrl, s =>
                s.MapFrom(mph =>
                    mph.Data.TryGetValue("track").TryGetValue("external_urls").TryGetValue("spotify")
                        .GetStringOrNull()))
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.OwnerId, opt => opt.Ignore())
            .ForMember(d => d.Description, opt => opt.Ignore())
            .ForMember(d => d.AddedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdateAt, opt => opt.MapFrom(mph => DateTime.UtcNow))
            .ForMember(d => d.ImageUrl,
                opt => opt.MapFrom(mph => GetImageUrl(mph.Data.TryGetValue("track").TryGetValue("album"))))
            .ForMember(d => d.SoundChartsId, opt => opt.Ignore())
            .ForMember(d => d.ChartmetricsId, opt => opt.Ignore())
            .ForMember(d => d.Audio, opt => opt.Ignore())
            .ForMember(d => d.TrackUrl, opt => opt.Ignore())
            .ForMember(d => d.LanguageCode, opt => opt.Ignore());

        //From chartmetrics to dto
        CreateMap<JsonElement, MetadataDto>()
            .ForMember(d => d.ChartmetricsId, s =>
                s.MapFrom(j => j.TryGetValue("id").GetStringOrNull()))
            .ForMember(d => d.Description, s =>
                s.MapFrom(j => j.TryGetValue("description").GetStringOrNull()))
            .ForMember(d => d.ImageUrl, s =>
                s.MapFrom(j => j.TryGetValue("image_url").GetStringOrNull()))
            .ForMember(d => d.IsPublic, s =>
                s.MapFrom(j => !j.TryGetValue("personalized").GetBoolOrFalse()))
            .ForMember(d => d.OwnerPlatformName, s =>
                s.MapFrom(j => j.TryGetValue("owner_name").GetStringOrNull()))
            .ForMember(d => d.OwnerPlatformId, s =>
                s.MapFrom(j => j.TryGetValue("owner_id").GetStringOrNull()))
            .ForMember(d => d.UserPlatformId, s =>
                s.MapFrom(j => j.TryGetValue("user_id").GetStringOrNull()))
            .ForMember(d => d.Followers, s =>
                s.MapFrom(j => j.TryGetValue("followers").GetUIntOrDefault()))
            .ForMember(d => d.TracksTotal, s =>
                s.MapFrom(j => j.TryGetValue("num_track").GetUIntOrDefault()))
            .ForMember(d => d.CatalogType, s =>
                s.MapFrom(j => (j.TryGetValue("catalog").GetStringOrNull() ?? string.Empty)
                    .Equals("catalog", StringComparison.InvariantCultureIgnoreCase)
                        ? 1
                        : 0))
            .ForMember(d => d.ActiveRatio, s =>
                s.MapFrom(j => j.TryGetValue("active_ratio").GetDoubleOrDefault()))
            .ForMember(d => d.SuspicionScore, s =>
                s.MapFrom(j => j.TryGetValue("suspicion_score").GetDoubleOrDefault()))
            .ForMember(d => d.Tags, s =>
                s.MapFrom(j => j.TryGetValue("tags").EnumerateArraySafe()
                    .Select(t => t.TryGetValue("name").GetStringOrNull())
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToList()))
            .ForMember(d => d.Moods, s =>
                s.MapFrom(j => j.TryGetValue("moods").EnumerateArraySafe()
                    .Where(t => !string.IsNullOrWhiteSpace(t.TryGetValue("rank").GetStringOrNull()) &&
                                !string.IsNullOrWhiteSpace(t.TryGetValue("name").GetStringOrNull()))
                    .ToDictionary(
                        t => t.TryGetValue("rank").GetIntOrDefault(),
                        t => t.TryGetValue("name").GetStringOrNull()!
                    )))
            .ForMember(d => d.Activities, s =>
                s.MapFrom(j => j.TryGetValue("activities").EnumerateArraySafe()
                    .Where(t => !string.IsNullOrWhiteSpace(t.TryGetValue("rank").GetStringOrNull()) &&
                                !string.IsNullOrWhiteSpace(t.TryGetValue("name").GetStringOrNull()))
                    .ToDictionary(
                        t => t.TryGetValue("rank").GetIntOrDefault(),
                        t => t.TryGetValue("name").GetStringOrNull()!
                    )))
            .ForMember(d => d.Tracks, s =>
                s.MapFrom(_ => new List<Guid>()))
            .ForMember(d => d.Id, s =>
                s.MapFrom(_ => Guid.Empty))
            .ForMember(d => d.SpredUserId, opt => opt.Ignore())
            .ForMember(d => d.PrimaryId,
                opt => opt.MapFrom(s => "playlist:" + s.TryGetValue("playlist_id").GetStringOrNull()))
            .ForMember(d => d.Name, opt => opt.MapFrom(s => s.TryGetValue("name").GetStringOrNull()))
            .ForMember(d => d.ListenUrls, opt =>
                opt.MapFrom(s => new Dictionary<string, string>
                {
                    { "spotify", "https://open.spotify.com/playlist/" + s.TryGetValue("playlist_id").GetStringOrNull() }
                }))
            .ForMember(d => d.SubmitUrls, opt => opt.Ignore())
            .ForMember(d => d.Collaborative, opt => opt.Ignore())
            .ForMember(d => d.SubmitEmail, opt => opt.Ignore())
            .ForMember(d => d.Type, opt => opt.MapFrom(s => "playlist"))
            .ForMember(d => d.Href, opt =>
                opt.MapFrom(s => "https://open.spotify.com/playlist/" + s.TryGetValue("playlist_id").GetStringOrNull()))
            .ForMember(d => d.Status, opt => opt.Ignore())
            .ForMember(d => d.SoundChartsId, opt => opt.Ignore())
            .ForMember(d => d.Country, opt => opt.Ignore())
            .ForMember(d => d.CityName, opt => opt.Ignore())
            .ForMember(d => d.CountryCode, opt => opt.Ignore())
            .ForMember(d => d.TimeZone, opt => opt.Ignore())
            .ForMember(d => d.SubmissionFormUrl, opt => opt.Ignore())
            .ForMember(d => d.SubmissionInstructions, opt => opt.Ignore())
            .ForMember(d => d.MusicRequirements, opt => opt.Ignore())
            .ForMember(d => d.SubmissionInfoUrl, opt => opt.Ignore())
            .ForMember(d => d.SubmissionFriendly, opt => opt.Ignore())
            .ForMember(d => d.PaymentType, opt => opt.Ignore())
            .ForMember(d => d.PaymentPrice, opt => opt.Ignore())
            .ForMember(d => d.PaymentDetails, opt => opt.Ignore())
            .ForMember(d => d.LocalizationScope, opt => opt.Ignore())
            .ForMember(d => d.LocalizationRegion, opt => opt.Ignore())
            .ForMember(d => d.LocalizationPriority, opt => opt.Ignore())
            .ForMember(d => d.AudienceScope, opt => opt.Ignore())
            .ForMember(d => d.BroadcastFormat, opt => opt.Ignore())
            .ForMember(d => d.CurationFrequency, opt => opt.Ignore())
            .ForMember(d => d.FeedbackRate, opt => opt.Ignore())
            .ForMember(d => d.Reach, opt => opt.Ignore());

        CreateMap<ChartmetricsTrackWrapper, TrackDto>()
            .ForMember(d => d.PrimaryId, s =>
                s.MapFrom(j => "track:" + j.Data.TryGetValue("spotify_track_id").GetStringOrNull()))
            .ForMember(d => d.Title, s =>
                s.MapFrom(j => j.Data.TryGetValue("name").GetStringOrNull()))
            .ForMember(d => d.TrackUrl, s =>
                s.MapFrom(j =>
                    $"https://open.spotify.com/track/{j.Data.TryGetValue("spotify_track_id").GetStringOrNull()}"))
            .ForMember(d => d.Popularity, s =>
                s.MapFrom(j => j.Data.TryGetValue("spotify_popularity").GetUIntOrDefault()))
            .ForMember(d => d.Published, s =>
                s.MapFrom(j => j.Data.TryGetValue("release_dates")
                    .EnumerateArraySafe()
                    .FirstOrDefault()
                    .GetDateTimeOrDefault(DateTime.MinValue).Date))
            .ForMember(d => d.AddedAt, s =>
                s.MapFrom(j => j.Data.TryGetValue("added_at").GetDateTimeOrDefault(DateTime.UtcNow).Date))
            .ForMember(d => d.UpdateAt, s => s.MapFrom(_ => DateTime.Now))
            .ForPath(d => d.Album!.AlbumName, s =>
                s.MapFrom(j => j.Data.TryGetValue("album_names")
                    .EnumerateArraySafe()
                    .FirstOrDefault()
                    .GetStringOrNull()))
            .ForPath(d => d.Album!.AlbumLabel, s =>
                s.MapFrom(j => j.Data.TryGetValue("album_label")
                    .EnumerateArraySafe()
                    .FirstOrDefault()
                    .GetStringOrNull()))
            .ForPath(d => d.Album!.AlbumReleaseDate, s =>
                s.MapFrom(j => j.Data.TryGetValue("release_dates")
                    .EnumerateArraySafe()
                    .FirstOrDefault()
                    .GetDateTimeOrDefault(DateTime.MinValue).Date.ToShortDateString()))
            .ForPath(d => d.Album!.ImageUrl, s =>
                s.MapFrom(j => j.Data.TryGetValue("image_url").GetStringOrNull()))
            .ForMember(d => d.Artists, s =>
                s.MapFrom(j => j.Data.TryGetValue("artists").EnumerateArraySafe()
                    .Select(a => new ArtistDto
                    {
                        Name = a.TryGetValue("name").GetStringOrNull() ?? string.Empty,
                        ImageUrl = a.TryGetValue("image_url").GetStringOrNull() ?? string.Empty,
                    }).ToList()))
            .ForMember(d => d.SourceType, s => s.MapFrom(_ => SourceType.ChartMetrics))
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.OwnerId, opt => opt.Ignore())
            .ForMember(d => d.Description, opt => opt.Ignore())
            .ForPath(d => d.ImageUrl, s =>
                s.MapFrom(j => j.Data.TryGetValue("image_url").GetStringOrNull()))
            .ForMember(d => d.SoundChartsId, opt => opt.Ignore())
            .ForMember(d => d.ChartmetricsId, opt => opt.Ignore())
            .ForMember(d => d.Audio, opt => opt.Ignore())
            .ForMember(d => d.TrackUrl, opt => opt.Ignore())
            .ForMember(d => d.LanguageCode, opt => opt.Ignore());
         
         CreateMap<JsonElement, StatInfo>()
             .ForMember(d => d.Timestamp, s =>
                 s.MapFrom(j => j.TryGetValue("timestp").GetDateTimeOrDefault(DateTime.MinValue)))
             .ForMember(d => d.Value, s =>
                 s.MapFrom(j => j.TryGetValue("value").GetUIntOrDefault()))
             .ForMember(d => d.DailyDiff, s =>
                 s.MapFrom(j => j.TryGetValue("daily_diff").GetIntOrDefault()));
         
         CreateMap<SoundchartsPlaylistWrapper, MetadataDto>()
            .ForMember(d => d.SoundChartsId, s =>
                s.MapFrom(w => w.Data.TryGetValue("object").TryGetValue("uuid").GetStringOrNull()))
            .ForMember(d => d.PrimaryId, s =>
                s.MapFrom(w => w.Data.TryGetValue("object").TryGetValue("platform").GetStringOrNull() + ":playlist:" +
                              w.Data.TryGetValue("object").TryGetValue("identifier").GetStringOrNull()))
            .ForMember(d => d.Name, s =>
                s.MapFrom(w => w.Data.TryGetValue("object").TryGetValue("name").GetStringOrNull()))
            .ForMember(d => d.Description, s =>
                s.MapFrom(_ => string.Empty))
            .ForMember(d => d.OwnerPlatformName, s =>
                s.MapFrom(w => w.Data.TryGetValue("object").TryGetValue("owner").TryGetValue("name").GetStringOrNull()))
            
            .ForMember(d => d.OwnerPlatformId, s =>
                s.MapFrom(w => w.Data.TryGetValue("object").TryGetValue("owner").TryGetValue("identifier").GetStringOrNull()))
            
            .ForMember(d => d.UserPlatformId, s =>
                s.MapFrom(w => w.Data.TryGetValue("object").TryGetValue("owner").TryGetValue("identifier").GetStringOrNull()))
            
            .ForMember(d => d.ImageUrl, s =>
                s.MapFrom(w => w.Data.TryGetValue("object").TryGetValue("imageUrl").GetStringOrNull()))
            .ForMember(d => d.ListenUrls, s =>
                s.MapFrom(w => new Dictionary<string, string>
                {
                    {
                        w.Data.TryGetValue("object").TryGetValue("platform").GetStringOrNull() ?? "unknown",
                        BuildPlatformPlaylistUrl(
                            w.Data.TryGetValue("object").TryGetValue("platform").GetStringOrNull(),
                            w.Data.TryGetValue("object").TryGetValue("identifier").GetStringOrNull()
                        )
                    }
                }))
            .ForMember(d => d.Followers, s =>
                s.MapFrom(w => (uint)w.Data.TryGetValue("object").TryGetValue("latestSubscriberCount").GetIntOrDefault()))
            .ForMember(d => d.TracksTotal, s =>
                s.MapFrom(w => (uint)w.Data.TryGetValue("object").TryGetValue("latestTrackCount").GetIntOrDefault()))
            .ForMember(d => d.Type, s =>
                s.MapFrom(_ => "playlist"))
            .ForMember(d => d.Status, s =>
                s.MapFrom(_ => FetchStatus.FetchedPlaylist))
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.SpredUserId, opt => opt.Ignore())
            .ForMember(d => d.SubmitUrls, opt => opt.Ignore())
            .ForMember(d => d.SubmitEmail, opt => opt.Ignore())
            .ForMember(d => d.Collaborative, opt => opt.Ignore())
            .ForMember(d => d.ChartmetricsId, opt => opt.Ignore())
            .ForMember(d => d.Tags, opt => opt.Ignore())
            .ForMember(d => d.CatalogType, opt => opt.Ignore())
            .ForMember(d => d.ActiveRatio, opt => opt.Ignore())
            .ForMember(d => d.SuspicionScore, opt => opt.Ignore())
            .ForMember(d => d.Moods, opt => opt.Ignore())
            .ForMember(d => d.Activities, opt => opt.Ignore())
            .ForMember(d => d.CatalogType, opt => 
                opt.MapFrom(s => s.Data.TryGetValue("object").TryGetValue("type").GetStringOrNull()))
            .ForMember(d => d.IsPublic, opt => opt.MapFrom(_ => true))
            .ForMember(d => d.Collaborative, opt => opt.MapFrom(_ => false))
            .ForMember(d => d.Href, s =>
                s.MapFrom(w => BuildPlatformPlaylistUrl(
                    w.Data.TryGetValue("object").TryGetValue("platform").GetStringOrNull(),
                    w.Data.TryGetValue("object").TryGetValue("identifier").GetStringOrNull()
                )))
            .ForMember(d => d.Tracks, opt => opt.MapFrom(_ => new List<Guid>()))
            .ForMember(d => d.Country, opt => opt.Ignore())
            .ForMember(d => d.CityName, opt => opt.Ignore())
            .ForMember(d => d.CountryCode, opt => opt.Ignore())
            .ForMember(d => d.TimeZone, opt => opt.Ignore())
            .ForMember(d => d.SubmissionFormUrl, opt => opt.Ignore())
            .ForMember(d => d.SubmissionInstructions, opt => opt.Ignore())
            .ForMember(d => d.MusicRequirements, opt => opt.Ignore())
            .ForMember(d => d.SubmissionInfoUrl, opt => opt.Ignore())
            .ForMember(d => d.SubmissionFriendly, opt => opt.Ignore())
            .ForMember(d => d.PaymentType, opt => opt.Ignore())
            .ForMember(d => d.PaymentPrice, opt => opt.Ignore())
            .ForMember(d => d.PaymentDetails, opt => opt.Ignore())
            .ForMember(d => d.LocalizationScope, opt => opt.Ignore())
            .ForMember(d => d.LocalizationRegion, opt => opt.Ignore())
            .ForMember(d => d.LocalizationPriority, opt => opt.Ignore())
            .ForMember(d => d.AudienceScope, opt => opt.Ignore())
            .ForMember(d => d.BroadcastFormat, opt => opt.Ignore())
            .ForMember(d => d.CurationFrequency, opt => opt.Ignore())
            .ForMember(d => d.FeedbackRate, opt => opt.Ignore())
            .ForMember(d => d.Reach, opt => opt.Ignore());

         CreateMap<SoundchartsTrackWrapper, TrackDtoWithPlatformIds>()
             .ForMember(d => d.PrimaryId, opt => opt.Ignore())
             .ForMember(d => d.Title, opt => opt.MapFrom(s =>
                 s.Data.TryGetValue("object").TryGetValue("name").GetStringOrNull()))
             .ForMember(d => d.Artists, opt => opt.MapFrom(s =>
                 s.Data.TryGetValue("object").TryGetValue("artists").EnumerateArraySafe()
                     .Select(a => new ArtistDto
                     {
                         PrimaryId = a.TryGetValue("uuid").GetStringOrNull() ?? string.Empty,
                         Name = a.TryGetValue("name").GetStringOrNull() ?? string.Empty,
                         ImageUrl = a.TryGetValue("imageUrl").GetStringOrNull() ?? string.Empty
                     })
                     .ToList()))
             .ForPath(d => d.Album!.ImageUrl, opt => opt.MapFrom(s =>
                 s.Data.TryGetValue("object").TryGetValue("imageUrl").GetStringOrNull()))
             .ForMember(d => d.SourceType, opt => opt.MapFrom(_ => SourceType.SoundCharts))
             .ForMember(d => d.TrackUrl, opt => opt.Ignore())
             .ForMember(d => d.Published, opt => opt.MapFrom(s =>
                 s.Data.TryGetValue("object").TryGetValue("releaseDate").GetDateTimeOrDefault(DateTime.MinValue)))
             .ForMember(d => d.UpdateAt, opt => opt.MapFrom(_ => DateTime.UtcNow))

             .ForPath(d => d.Audio.Duration, opt => opt.MapFrom(s =>
                 TimeSpan.FromSeconds(s.Data.TryGetValue("object").TryGetValue("duration").GetDoubleOrDefault())))
             .ForMember(d => d.Id, opt => opt.Ignore())
             .ForMember(d => d.OwnerId, opt => opt.Ignore())
             .ForMember(d => d.Description, opt =>
                 opt.MapFrom(s => s.Data.TryGetValue("object").TryGetValue("creditName").GetStringOrNull()))
             .ForMember(d => d.AddedAt, opt => opt.Ignore())
             .ForMember(d => d.SoundChartsId, opt =>
                 opt.MapFrom(s => s.Data.TryGetValue("object").TryGetValue("uuid").GetStringOrNull()))
             .ForMember(d => d.ChartmetricsId, opt => opt.Ignore())
             .ForMember(d => d.PrimaryIds, opt => opt.Ignore())
             .ForMember(d => d.ImageUrl, opt => opt.MapFrom(s =>
                 s.Data.TryGetValue("object").TryGetValue("imageUrl").GetStringOrNull()))
             .ForMember(d => d.Popularity, opt => opt.Ignore())
             .ForMember(d => d.LanguageCode, opt => opt.MapFrom(s =>
                     s.Data.TryGetValue("object").TryGetValue("languageCode").GetStringOrNull()))

             .ForPath(d => d.Audio.Acousticness, opt => opt.MapFrom(s =>
                 s.Data.TryGetValue("object").TryGetValue("audio").TryGetValue("acousticness").GetDoubleOrDefault()))
             .ForPath(d => d.Audio.Danceability, opt => opt.MapFrom(s =>
                 s.Data.TryGetValue("object").TryGetValue("audio").TryGetValue("danceability").GetDoubleOrDefault()))
             .ForPath(d => d.Audio.Energy, opt => opt.MapFrom(s =>
                 s.Data.TryGetValue("object").TryGetValue("audio").TryGetValue("energy").GetDoubleOrDefault()))
             .ForPath(d => d.Audio.Instrumentalness, opt => opt.MapFrom(s =>
                 s.Data.TryGetValue("object").TryGetValue("audio").TryGetValue("instrumentalness")
                     .GetDoubleOrDefault()))
             .ForPath(d => d.Audio.Key, opt => opt.MapFrom(s =>
                 s.Data.TryGetValue("object").TryGetValue("audio").TryGetValue("key").GetIntOrDefault()))
             .ForPath(d => d.Audio.Liveness, opt => opt.MapFrom(s =>
                 s.Data.TryGetValue("object").TryGetValue("audio").TryGetValue("liveness").GetDoubleOrDefault()))
             .ForPath(d => d.Audio.Loudness, opt => opt.MapFrom(s =>
                 s.Data.TryGetValue("object").TryGetValue("audio").TryGetValue("loudness").GetDoubleOrDefault()))
             .ForPath(d => d.Audio.Mode, opt => opt.MapFrom(s =>
                 s.Data.TryGetValue("object").TryGetValue("audio").TryGetValue("mode").GetIntOrDefault()))
             .ForPath(d => d.Audio.Speechiness, opt => opt.MapFrom(s =>
                 s.Data.TryGetValue("object").TryGetValue("audio").TryGetValue("speechiness").GetDoubleOrDefault()))
             .ForPath(d => d.Audio.Tempo, opt => opt.MapFrom(s =>
                 s.Data.TryGetValue("object").TryGetValue("audio").TryGetValue("tempo").GetDoubleOrDefault()))
             .ForPath(d => d.Audio.TimeSignature, opt => opt.MapFrom(s =>
                 s.Data.TryGetValue("object").TryGetValue("audio").TryGetValue("timeSignature").GetIntOrDefault()))
             .ForPath(d => d.Audio.Valence, opt => opt.MapFrom(s =>
                 s.Data.TryGetValue("object").TryGetValue("audio").TryGetValue("valence").GetDoubleOrDefault()))
             .ForPath(d => d.Audio.Bitrate, opt => opt.MapFrom(_ => 0))
             .ForPath(d => d.Audio.SampleRate, opt => opt.MapFrom(_ => 0))
             .ForPath(d => d.Audio.Channels, opt => opt.MapFrom(_ => 0))
             .ForPath(d => d.Audio.Codec, opt => opt.Ignore())
             .ForPath(d => d.Audio.Bpm, opt => opt.MapFrom(s =>
                 s.Data.TryGetValue("object").TryGetValue("audio").TryGetValue("tempo").GetIntOrDefault()))
             .ForPath(d => d.Audio.Genre, opt => opt.MapFrom(s =>
                 string.Join(", ", s.Data.TryGetValue("object").TryGetValue("genres").EnumerateArraySafe()
                     .Select(g => g.TryGetValue("root").GetStringOrNull())
                     .Where(x => !string.IsNullOrWhiteSpace(x)))));
    }

    /// <summary>
    /// Custom type converter for converting string to DateTime.
    /// </summary>
    private abstract class DateTimeTypeConverter : ITypeConverter<string, DateTime>
    {
        public DateTime Convert(string source, DateTime destination, ResolutionContext context)
        {
            if (string.IsNullOrWhiteSpace(source))
                return DateTime.MinValue;
            else
            {
                return DateTime.TryParse(s: source, result: out var dateTime) ? dateTime : DateTime.MinValue;
            }
        }
    }

    /// <summary>
    /// Custom type converter for converting string to uint.
    /// </summary>
    private abstract class UIntTypeConverter : ITypeConverter<string, uint>
    {
        public uint Convert(string source, uint destination, ResolutionContext context)
        {
            if (uint.TryParse(source, out var result))
                return result;
            else
                return 0;
        }
    }

    internal static Dictionary<string, string> MapDictUrls(JsonElement? mph)
    {
        if (mph is { ValueKind: JsonValueKind.Object })
        {
            var result = mph.Value.EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.GetString() ?? "");
            return result;
        }

        return new Dictionary<string, string>();
    }

    internal static string GetImageUrl(JsonElement? mph)
    {
        var images = mph.TryGetValue("images");

        if (images is { ValueKind: JsonValueKind.Array })
        {
            var validImages = images.Value.EnumerateArray()
                .Where(img => img.TryGetProperty("url", out var url) && !string.IsNullOrWhiteSpace(url.GetString()));

            var jsonElements = validImages as JsonElement[] ?? validImages.ToArray();
            var imageWithHeight = jsonElements
                .Where(img => img.TryGetProperty("height", out var heightEl) && heightEl.ValueKind == JsonValueKind.Number)
                .OrderByDescending(img => img.GetProperty("height").GetInt32())
                .FirstOrDefault();

            if (imageWithHeight.ValueKind != JsonValueKind.Undefined &&
                imageWithHeight.TryGetProperty("url", out var maxUrl))
            {
                return maxUrl.GetString() ?? string.Empty;
            }

            var fallback = jsonElements.FirstOrDefault();
            if (fallback.ValueKind != JsonValueKind.Undefined && fallback.TryGetProperty("url", out var fallbackUrl))
            {
                return fallbackUrl.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    internal static string BuildPlatformPlaylistUrl(string? platform, string? identifier)
    {
        if (string.IsNullOrWhiteSpace(platform) || string.IsNullOrWhiteSpace(identifier))
            return string.Empty;

        return platform.ToLowerInvariant() switch
        {
            "spotify"   => $"https://open.spotify.com/playlist/{identifier}",
            "applemusic" or "apple" or "itunes" 
                => $"https://music.apple.com/playlist/{identifier}",
            "deezer"    => $"https://www.deezer.com/playlist/{identifier}",
            "youtube" or "youtubemusic" 
                => $"https://music.youtube.com/playlist?list={identifier}",
            "soundcloud" or "sc" 
                => $"https://soundcloud.com/{identifier}",
            _           => string.Empty
        };
    }
    
    internal static string BuildPlatformTrackUrl(string platform, string? identifier)
    {
        if (string.IsNullOrWhiteSpace(platform) || string.IsNullOrWhiteSpace(identifier))
            return string.Empty;

        return platform.ToLowerInvariant() switch
        {
            "spotify"   => $"https://open.spotify.com/track/{identifier}",
            "applemusic" or "apple" or "itunes"
                => $"https://music.apple.com/track/{identifier}",
            "deezer"    => $"https://www.deezer.com/track/{identifier}",
            "youtube" or "youtubemusic"
                => $"https://music.youtube.com/watch?v={identifier}",
            "soundcloud" or "sc"
                => $"https://soundcloud.com/{identifier}",
            _           => string.Empty
        };
    }
}
