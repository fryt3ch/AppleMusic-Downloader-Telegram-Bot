using System.Globalization;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusic.API.Models.Resources;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.SongFiles;

public sealed class AppleMusicTagMetadataBuilder
{
    private const int SongMediaType = 1;

    public SongTagMetadata Build(Song song)
    {
        var relationships = song.Relationships;
        var album = relationships?.Albums?.Data?.FirstOrDefault();
        var artist = relationships?.Artists?.Data?.FirstOrDefault();
        var composer = relationships?.Composers?.Data?.FirstOrDefault();
        var genre = relationships?.Genres?.Data?.FirstOrDefault();

        var releaseDate = song.Attributes.ReleaseDate;

        if (releaseDate == default)
            releaseDate = album?.Attributes?.ReleaseDate ?? default;

        return new SongTagMetadata
        {
            Artist = song.Attributes.ArtistName,
            Title = song.Attributes.Name,
            DurationSeconds = song.Attributes.Duration > TimeSpan.Zero
                ? song.Attributes.Duration.TotalSeconds
                : null,
            CoverUrl = song.Attributes.Artwork?.Url?.ToString(),
            Album = FirstNonEmpty(song.Attributes.AlbumName, album?.Attributes?.Name),
            AlbumArtist = FirstNonEmpty(song.Attributes.AlbumArtistName, album?.Attributes?.ArtistName),
            AlbumId = ParseId(album?.Id),
            ArtistId = ParseId(artist?.Id),
            Composer = song.Attributes.ComposerName,
            ComposerId = ParseId(composer?.Id),
            Copyright = album?.Attributes?.Copyright,
            Date = releaseDate == default
                ? null
                : releaseDate.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            Disc = Positive(song.Attributes.DiskNumber),
            Genre = FirstNonEmpty(song.Attributes.GenreNames?.FirstOrDefault(), genre?.Attributes?.Name),
            GenreId = ParseId(genre?.Id),
            TitleId = ParseId(song.Id),
            Track = Positive(song.Attributes.TrackNumber),
            TrackTotal = Positive(album?.Attributes?.TrackCount),
            Compilation = album?.Attributes?.IsCompilation,
            MediaType = SongMediaType,
            Rating = MapRating(song.Attributes.ContentRating)
                     ?? MapRating(album?.Attributes?.ContentRating),
        };
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static long? ParseId(string? value)
    {
        return long.TryParse(value, out var id) ? id : null;
    }

    private static int? Positive(int? value)
    {
        return value is > 0 ? value : null;
    }

    private static int? MapRating(ContentRating? rating)
    {
        return rating switch
        {
            ContentRating.Explicit => 1,
            ContentRating.Clean => 2,
            _ => null,
        };
    }
}
