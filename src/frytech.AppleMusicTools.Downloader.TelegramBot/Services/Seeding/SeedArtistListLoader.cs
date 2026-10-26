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

        var overrides = await LoadOverridesAsync(settings.OverridesFile, cancellationToken);

        var lines = await File.ReadAllLinesAsync(artistsPath, cancellationToken);
        var result = new List<SeedArtistRecord>(Math.Min(lines.Length, 5000));
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rawLine in lines)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var normalized = ArtistNameMatcher.Normalize(line);

            if (normalized.Length == 0 || !seen.Add(normalized))
                continue;

            overrides.TryGetValue(normalized, out var appleMusicId);

            result.Add(new SeedArtistRecord(result.Count, line, normalized, appleMusicId));
        }

        _logger.LogInformation("Loaded {Count} seed artists from {Path} ({Overrides} overrides)", result.Count, artistsPath, overrides.Count);

        return result;
    }

    private async Task<IReadOnlyDictionary<string, string>> LoadOverridesAsync(string overridesFile, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(overridesFile))
            return result;

        var path = SeedPathResolver.ResolveReadPath(overridesFile);

        if (!File.Exists(path))
        {
            _logger.LogDebug("Seed artist overrides file was not found: {Path}", path);

            return result;
        }

        var lines = await File.ReadAllLinesAsync(path, cancellationToken);

        foreach (var rawLine in lines)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var separator = line.IndexOf('\t');

            if (separator <= 0 || separator >= line.Length - 1)
            {
                _logger.LogWarning("Ignoring malformed seed artist override: {Line}", line);

                continue;
            }

            var name = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            var normalized = ArtistNameMatcher.Normalize(name);

            if (normalized.Length == 0 || value.Length == 0)
                continue;

            var appleMusicId = ParseAppleMusicArtistId(value);

            if (appleMusicId is null)
            {
                _logger.LogWarning("Could not parse Apple Music artist ID from override '{Name}' -> '{Value}'", name, value);

                continue;
            }

            result[normalized] = appleMusicId;
        }

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