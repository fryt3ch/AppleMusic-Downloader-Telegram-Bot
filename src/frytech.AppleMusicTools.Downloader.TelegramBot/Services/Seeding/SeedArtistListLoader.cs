using System.Text.RegularExpressions;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using Microsoft.Extensions.Options;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Seeding;

public sealed record SeedArtistRecord(int Index, string Name, string NormalizedName, string? AppleMusicId);

public sealed class SeedArtistListLoader
{
    private readonly IOptions<AppSettings> _appSettings;
    private readonly ILogger<SeedArtistListLoader> _logger;

    public SeedArtistListLoader(IOptions<AppSettings> appSettings, ILogger<SeedArtistListLoader> logger)
    {
        _appSettings = appSettings;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SeedArtistRecord>> LoadAsync(CancellationToken cancellationToken)
    {
        var settings = _appSettings.Value.Seed;
        var artistsPath = SeedPathResolver.ResolveReadPath(settings.ArtistsFile);

        if (!File.Exists(artistsPath))
            throw new FileNotFoundException($"Seed artist list was not found: {artistsPath}", artistsPath);

        var lines = await File.ReadAllLinesAsync(artistsPath, cancellationToken);
        var result = new List<SeedArtistRecord>(Math.Min(lines.Length, 5000));
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var mappedCount = 0;

        foreach (var rawLine in lines)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var separator = line.IndexOf('|');
            var name = separator < 0 ? line : line[..separator].Trim();
            var value = separator < 0 ? null : line[(separator + 1)..].Trim();
            var normalized = ArtistNameMatcher.Normalize(name);

            if (normalized.Length == 0 || !seen.Add(normalized))
                continue;

            string? appleMusicId = null;

            if (!string.IsNullOrEmpty(value))
            {
                appleMusicId = ParseAppleMusicArtistId(value);

                if (appleMusicId is null)
                {
                    _logger.LogWarning("Could not parse Apple Music artist ID from '{Name}' -> '{Value}'", name, value);
                }
                else
                {
                    mappedCount++;
                }
            }

            result.Add(new SeedArtistRecord(result.Count, name, normalized, appleMusicId));
        }

        _logger.LogInformation("Loaded {Count} seed artists from {Path} ({Mapped} with Apple Music ID)", result.Count, artistsPath, mappedCount);

        return result;
    }

    private static string? ParseAppleMusicArtistId(string value)
    {
        if (value.All(char.IsDigit))
            return value;

        var matches = Regex.Matches(value, @"\d{3,}");

        return matches.Count == 0 ? null : matches[^1].Value;
    }
}