using System.Text.RegularExpressions;
using frytech.AppleMusic.API.Models.Resources;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Seeding;

public sealed class SeedFilters
{
    private static readonly Regex MarkerRegex = new(
        @"[\(\[\{]([^\)\]\}]+)[\)\]\}]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly SeedFilterSettings _settings;
    private readonly IReadOnlyList<Regex> _albumPatterns;
    private readonly IReadOnlyList<Regex> _trackPatterns;

    public SeedFilters(AppSettings.SeedSection settings)
    {
        _settings = new SeedFilterSettings(
            settings.IncludeSingles,
            settings.MinTrackSeconds);

        _albumPatterns = BuildPatterns(settings.ExcludeAlbumPatterns);
        _trackPatterns = BuildPatterns(settings.ExcludeTrackPatterns);
    }

    public bool ShouldIncludeAlbum(Album album)
    {
        var attributes = album.Attributes;

        if (attributes is null)
            return false;

        if (attributes.TrackCount <= 0)
            return false;

        if (!_settings.IncludeSingles && attributes.IsSingle)
            return false;

        return !Matches(attributes.Name, _albumPatterns);
    }

    public bool ShouldIncludeSong(Song song)
    {
        var attributes = song.Attributes;

        if (attributes is null)
            return false;

        if (attributes.Duration.TotalSeconds < _settings.MinTrackSeconds)
            return false;

        var markers = GetTitleMarkers(attributes.Name);

        return markers.Count == 0 || !markers.Any(marker => Matches(marker, _trackPatterns));
    }

    private static bool Matches(string? value, IReadOnlyList<Regex> patterns)
    {
        if (string.IsNullOrWhiteSpace(value) || patterns.Count == 0)
            return false;

        return patterns.Any(pattern => pattern.IsMatch(value));
    }

    private static IReadOnlyList<string> GetTitleMarkers(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return [];

        var markers = new List<string>();

        foreach (Match match in MarkerRegex.Matches(title))
        {
            var marker = match.Groups[1].Value.Trim();

            if (marker.Length > 0)
                markers.Add(marker);
        }

        var separator = title.LastIndexOf(" - ", StringComparison.Ordinal);

        if (separator > 0 && separator + 3 < title.Length)
            markers.Add(title[(separator + 3)..].Trim());

        return markers;
    }

    private static IReadOnlyList<Regex> BuildPatterns(string? patterns)
    {
        if (string.IsNullOrWhiteSpace(patterns))
            return [];

        return patterns
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(pattern => pattern.Length > 0)
            .Select(pattern => new Regex(
                $@"\b{Regex.Escape(pattern)}",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled))
            .ToArray();
    }

    private sealed record SeedFilterSettings(bool IncludeSingles, int MinTrackSeconds);
}
