namespace frytech.AppleMusicTools.Downloader.TelegramBot.Models.Database;

public sealed class CachedSong
{
    public long Id { get; set; }

    public string ArtistName { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string NormalizedArtistName { get; set; } = null!;

    public string NormalizedName { get; set; } = null!;

    public TimeSpan Duration { get; set; }

    public string Provider { get; set; } = null!;

    public string? SourceUrl { get; set; }

    public string FileId { get; set; } = null!;

    public string FileUniqueId { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
}
