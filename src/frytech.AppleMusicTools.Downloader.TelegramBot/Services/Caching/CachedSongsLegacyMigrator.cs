using System.Data;
using System.Globalization;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Database;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Catalog;
using Microsoft.EntityFrameworkCore;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Caching;

public sealed class CachedSongsLegacyMigrator
{
    private static readonly TimeSpan DurationTolerance = TimeSpan.FromSeconds(5);

    private readonly AppDbContext _dbContext;
    private readonly ILogger<CachedSongsLegacyMigrator> _logger;

    public CachedSongsLegacyMigrator(
        AppDbContext dbContext,
        ILogger<CachedSongsLegacyMigrator> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        if (!await LegacyTableExistsAsync(cancellationToken))
            return;

        _logger.LogInformation("Migrating legacy CachedSongs records...");

        var legacySongs = await ReadLegacySongsAsync(cancellationToken);
        var migrated = 0;
        var duplicates = 0;
        var skipped = 0;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var keptDurations = new Dictionary<string, List<TimeSpan>>(StringComparer.Ordinal);

            foreach (var legacySong in legacySongs)
            {
                var normalizedArtist = SongIdentityNormalizer.Normalize(legacySong.ArtistName);
                var normalizedTitle = SongIdentityNormalizer.Normalize(legacySong.Name);

                if (normalizedArtist.Length == 0 || normalizedTitle.Length == 0)
                {
                    skipped++;

                    continue;
                }

                var matchKey = $"{normalizedArtist}{SongIdentityNormalizer.MatchKeySeparator}{normalizedTitle}";

                if (keptDurations.TryGetValue(matchKey, out var durations)
                    && durations.Any(duration => IsDurationClose(duration, legacySong.Duration)))
                {
                    duplicates++;

                    continue;
                }

                if (durations is null)
                {
                    durations = [];
                    keptDurations[matchKey] = durations;
                }

                durations.Add(legacySong.Duration);

                _dbContext.CachedSongs.Add(new CachedSong
                {
                    ArtistName = legacySong.ArtistName,
                    Name = legacySong.Name,
                    NormalizedArtistName = normalizedArtist,
                    NormalizedName = normalizedTitle,
                    Duration = legacySong.Duration,
                    Provider = legacySong.Provider,
                    SourceUrl = AppleMusicUrl.Create(ResourceType.Songs, legacySong.SongId, legacySong.Store).AbsoluteUri,
                    FileId = legacySong.FileId,
                    FileUniqueId = legacySong.FileUniqueId,
                    CreatedAt = DateTimeOffset.UtcNow,
                });

                migrated++;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await _dbContext.Database.ExecuteSqlRawAsync("DROP TABLE \"CachedSongs_legacy\";", cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Legacy CachedSongs migration completed: migrated {Migrated}, duplicates {Duplicates}, skipped {Skipped}",
                migrated,
                duplicates,
                skipped);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(ex, "Legacy CachedSongs migration failed; the legacy table will be retried on the next startup");
        }
    }

    private async Task<bool> LegacyTableExistsAsync(CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'CachedSongs_legacy';";

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return Convert.ToInt64(result, CultureInfo.InvariantCulture) > 0;
    }

    private async Task<IReadOnlyList<LegacyCachedSong>> ReadLegacySongsAsync(CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT SongId, Store, FileId, FileUniqueId, Name, ArtistName, Duration, Provider
            FROM CachedSongs_legacy
            ORDER BY rowid DESC;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var songs = new List<LegacyCachedSong>();

        while (await reader.ReadAsync(cancellationToken))
        {
            songs.Add(new LegacyCachedSong(
                SongId: reader.GetString(0),
                Store: reader.GetString(1),
                FileId: reader.GetString(2),
                FileUniqueId: reader.GetString(3),
                Name: reader.GetString(4),
                ArtistName: reader.GetString(5),
                Duration: ParseDuration(reader.GetValue(6)),
                Provider: reader.IsDBNull(7) ? "gamdl" : reader.GetString(7)));
        }

        return songs;
    }

    private static TimeSpan ParseDuration(object value)
    {
        return value switch
        {
            TimeSpan duration => duration,
            string text => TimeSpan.Parse(text, CultureInfo.InvariantCulture),
            _ => TimeSpan.FromTicks(Convert.ToInt64(value, CultureInfo.InvariantCulture)),
        };
    }

    private static bool IsDurationClose(TimeSpan first, TimeSpan second)
    {
        return Math.Abs((first - second).TotalSeconds) <= DurationTolerance.TotalSeconds;
    }

    private sealed record LegacyCachedSong(
        string SongId,
        string Store,
        string FileId,
        string FileUniqueId,
        string Name,
        string ArtistName,
        TimeSpan Duration,
        string Provider);
}
