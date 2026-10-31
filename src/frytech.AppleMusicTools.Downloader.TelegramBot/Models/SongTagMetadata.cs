using System.Text.Json.Serialization;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Models;

public sealed record SongTagMetadata
{
    [JsonPropertyName("artist")]
    public string? Artist { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("duration_seconds")]
    public double? DurationSeconds { get; init; }

    [JsonPropertyName("cover_url")]
    public string? CoverUrl { get; init; }

    [JsonPropertyName("album")]
    public string? Album { get; init; }

    [JsonPropertyName("album_artist")]
    public string? AlbumArtist { get; init; }

    [JsonPropertyName("album_id")]
    public long? AlbumId { get; init; }

    [JsonPropertyName("album_sort")]
    public string? AlbumSort { get; init; }

    [JsonPropertyName("artist_id")]
    public long? ArtistId { get; init; }

    [JsonPropertyName("artist_sort")]
    public string? ArtistSort { get; init; }

    [JsonPropertyName("comment")]
    public string? Comment { get; init; }

    [JsonPropertyName("compilation")]
    public bool? Compilation { get; init; }

    [JsonPropertyName("composer")]
    public string? Composer { get; init; }

    [JsonPropertyName("composer_id")]
    public long? ComposerId { get; init; }

    [JsonPropertyName("composer_sort")]
    public string? ComposerSort { get; init; }

    [JsonPropertyName("copyright")]
    public string? Copyright { get; init; }

    [JsonPropertyName("date")]
    public string? Date { get; init; }

    [JsonPropertyName("disc")]
    public int? Disc { get; init; }

    [JsonPropertyName("disc_total")]
    public int? DiscTotal { get; init; }

    [JsonPropertyName("gapless")]
    public bool? Gapless { get; init; }

    [JsonPropertyName("genre")]
    public string? Genre { get; init; }

    [JsonPropertyName("genre_id")]
    public long? GenreId { get; init; }

    [JsonPropertyName("lyrics")]
    public string? Lyrics { get; init; }

    [JsonPropertyName("media_type")]
    public int? MediaType { get; init; }

    [JsonPropertyName("rating")]
    public int? Rating { get; init; }

    [JsonPropertyName("storefront")]
    public int? Storefront { get; init; }

    [JsonPropertyName("title_id")]
    public long? TitleId { get; init; }

    [JsonPropertyName("title_sort")]
    public string? TitleSort { get; init; }

    [JsonPropertyName("track")]
    public int? Track { get; init; }

    [JsonPropertyName("track_total")]
    public int? TrackTotal { get; init; }

    [JsonPropertyName("xid")]
    public string? Xid { get; init; }
}
