using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Database;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Catalog;
using frytech.AppleMusicTools.Downloader.TelegramBot.Telegram;
using KeyedSemaphores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Caching;

public sealed class TelegramStorage : ISongStorage
{
    private static readonly TimeSpan DurationTolerance = TimeSpan.FromSeconds(5);

    private readonly ITelegramBotClient _client;
    private readonly AppDbContext _appDbContext;
    private readonly AppleMusicCatalog _catalog;
    private readonly CachedSongDbLock _dbLock;
    private readonly ChatId _storageChatId;
    private readonly ILogger<TelegramStorage> _logger;

    public TelegramStorage(
        ITelegramBotClient client,
        IOptions<AppSettings> appSettings,
        AppDbContext appDbContext,
        AppleMusicCatalog catalog,
        CachedSongDbLock dbLock,
        ILogger<TelegramStorage> logger)
    {
        _client = client;
        _appDbContext = appDbContext;
        _catalog = catalog;
        _dbLock = dbLock;
        _logger = logger;

        _storageChatId = new ChatId(appSettings.Value.Telegram.StorageChatId);
    }

    public async Task<CachedSong> GetOrAddSong(string songId, string store, Func<CancellationToken, Task<SongFile>> getSongFile, CancellationToken cancellationToken)
    {
        var sourceUrl = AppleMusicUrl.Create(ResourceType.Songs, songId, store).AbsoluteUri;

        var cachedSong = await FindBySourceUrlAsync(sourceUrl, cancellationToken);

        if (cachedSong is not null)
        {
            _logger.LogDebug("Cache hit by source URL for song {SongId} in store {Store}", songId, store);

            return cachedSong;
        }

        var song = await _catalog.GetSongAsync(songId, store);
        var normalizedArtist = SongIdentityNormalizer.Normalize(song.Attributes.ArtistName);
        var normalizedTitle = SongIdentityNormalizer.Normalize(song.Attributes.Name);
        var matchKey = BuildMatchKey(normalizedArtist, normalizedTitle);

        using (await KeyedSemaphore.LockAsync(matchKey, cancellationToken))
        {
            cachedSong = await FindBySourceUrlAsync(sourceUrl, cancellationToken);

            if (cachedSong is not null)
            {
                _logger.LogDebug("Cache hit by source URL for song {SongId} in store {Store}", songId, store);

                return cachedSong;
            }

            cachedSong = await FindByNormalizedIdentityAsync(normalizedArtist, normalizedTitle, song.Attributes.Duration, cancellationToken);

            if (cachedSong is not null)
            {
                _logger.LogDebug("Cache hit for '{Artist} - {Title}' ({Duration})", song.Attributes.ArtistName, song.Attributes.Name, song.Attributes.Duration);

                return cachedSong;
            }

            _logger.LogDebug("Cache miss for '{Artist} - {Title}' ({Duration})", song.Attributes.ArtistName, song.Attributes.Name, song.Attributes.Duration);

            var songFile = await getSongFile(cancellationToken);
            var downloadedSong = songFile.Song;

            try
            {
                var uploadedFileMessage = await TelegramApiRetry.ExecuteAsync(
                    requestToken =>
                    {
                        if (songFile.Stream.CanSeek)
                            songFile.Stream.Position = 0;

                        return _client.SendAudio(
                            _storageChatId,
                            InputFile.FromStream(songFile.Stream, songFile.FileName),
                            title: downloadedSong.Attributes.Name,
                            performer: downloadedSong.Attributes.ArtistName,
                            duration: (int)downloadedSong.Attributes.Duration.TotalSeconds,
                            cancellationToken: requestToken);
                    },
                    _logger,
                    cancellationToken);

                var uploadedFile = uploadedFileMessage.Audio ?? throw new InvalidOperationException($"Telegram did not return an audio file for song '{downloadedSong.Id}'.");

                cachedSong = new CachedSong
                {
                    ArtistName = downloadedSong.Attributes.ArtistName,
                    Name = downloadedSong.Attributes.Name,
                    NormalizedArtistName = SongIdentityNormalizer.Normalize(downloadedSong.Attributes.ArtistName),
                    NormalizedName = SongIdentityNormalizer.Normalize(downloadedSong.Attributes.Name),
                    Duration = downloadedSong.Attributes.Duration,
                    Provider = songFile.ProviderName,
                    SourceUrl = sourceUrl,
                    FileId = uploadedFile.FileId,
                    FileUniqueId = uploadedFile.FileUniqueId,
                    CreatedAt = DateTimeOffset.UtcNow,
                };

                await _dbLock.Semaphore.WaitAsync(cancellationToken);

                try
                {
                    await _appDbContext.AddAsync(cachedSong, cancellationToken);
                    await _appDbContext.SaveChangesAsync(cancellationToken);
                }
                finally
                {
                    _dbLock.Semaphore.Release();
                }

                _logger.LogInformation("Cached '{Artist} - {Title}' from provider {Provider}", downloadedSong.Attributes.ArtistName, downloadedSong.Attributes.Name, songFile.ProviderName);

                return cachedSong;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cache '{Artist} - {Title}' from provider {Provider}", downloadedSong.Attributes.ArtistName, downloadedSong.Attributes.Name, songFile.ProviderName);

                throw;
            }
            finally
            {
                await songFile.Stream.DisposeAsync();
            }
        }
    }

    private async Task<CachedSong?> FindBySourceUrlAsync(string sourceUrl, CancellationToken cancellationToken)
    {
        await _dbLock.Semaphore.WaitAsync(cancellationToken);

        try
        {
            return await _appDbContext.CachedSongs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SourceUrl == sourceUrl, cancellationToken);
        }
        finally
        {
            _dbLock.Semaphore.Release();
        }
    }

    private async Task<CachedSong?> FindByNormalizedIdentityAsync(string normalizedArtist, string normalizedTitle, TimeSpan duration, CancellationToken cancellationToken)
    {
        await _dbLock.Semaphore.WaitAsync(cancellationToken);

        try
        {
            var candidates = await _appDbContext.CachedSongs
                .AsNoTracking()
                .Where(x => x.NormalizedArtistName == normalizedArtist && x.NormalizedName == normalizedTitle)
                .ToListAsync(cancellationToken);

            return candidates
                .Select(x => new
                {
                    Song = x,
                    Difference = Math.Abs((x.Duration - duration).TotalSeconds),
                })
                .Where(x => x.Difference <= DurationTolerance.TotalSeconds)
                .OrderBy(x => x.Difference)
                .Select(x => x.Song)
                .FirstOrDefault();
        }
        finally
        {
            _dbLock.Semaphore.Release();
        }
    }

    private static string BuildMatchKey(string normalizedArtist, string normalizedTitle)
    {
        return $"{normalizedArtist}{SongIdentityNormalizer.MatchKeySeparator}{normalizedTitle}";
    }
}
