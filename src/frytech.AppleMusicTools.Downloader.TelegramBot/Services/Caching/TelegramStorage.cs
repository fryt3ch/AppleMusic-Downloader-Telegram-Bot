using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Database;
using frytech.AppleMusicTools.Downloader.TelegramBot.Telegram;
using KeyedSemaphores;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Caching;

public sealed class TelegramStorage : ISongStorage
{
    private readonly ITelegramBotClient _client;
    private readonly AppDbContext _appDbContext;
    private readonly ChatId _storageChatId;
    private readonly ILogger<TelegramStorage> _logger;

    public TelegramStorage(
        ITelegramBotClient client,
        IOptions<AppSettings> appSettings,
        AppDbContext appDbContext,
        ILogger<TelegramStorage> logger)
    {
        _client = client;
        _appDbContext = appDbContext;
        _logger = logger;

        _storageChatId = new ChatId(appSettings.Value.Telegram.StorageChatId);
    }

    public async Task<CachedSong> GetOrAddSong(
        string songId,
        string store,
        Func<CancellationToken, Task<SongFile>> getSongFile,
        CancellationToken cancellationToken)
    {
        using (await KeyedSemaphore.LockAsync($"{store}:{songId}", cancellationToken))
        {
            var cachedSong = await _appDbContext.CachedSongs.FindAsync([songId, store], cancellationToken);

            if (cachedSong is not null)
            {
                _logger.LogDebug("Cache hit for song {SongId} in store {Store}", songId, store);

                return cachedSong;
            }

            _logger.LogDebug("Cache miss for song {SongId} in store {Store}", songId, store);

            var songFile = await getSongFile(cancellationToken);
            var song = songFile.Song;

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
                            title: song.Attributes.Name,
                            performer: song.Attributes.ArtistName,
                            duration: (int)song.Attributes.Duration.TotalSeconds,
                            cancellationToken: requestToken);
                    },
                    _logger,
                    cancellationToken);

                var uploadedFile = uploadedFileMessage.Audio
                    ?? throw new InvalidOperationException($"Telegram did not return an audio file for song '{song.Id}'.");

                cachedSong = new CachedSong()
                {
                    SongId = song.Id,
                    Store = store,
                    Name = song.Attributes.Name,
                    ArtistName = song.Attributes.ArtistName,
                    Duration = song.Attributes.Duration,
                    FileId = uploadedFile.FileId,
                    FileUniqueId = uploadedFile.FileUniqueId,
                    Provider = songFile.ProviderName,
                };

                await _appDbContext.AddAsync(cachedSong, cancellationToken);
                await _appDbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Cached song {SongId} from provider {Provider} in store {Store}",
                    song.Id,
                    songFile.ProviderName,
                    store);

                return cachedSong;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to cache song {SongId} in store {Store}", songId, store);

                throw;
            }
            finally
            {
                await songFile.Stream.DisposeAsync();
            }
        }
    }
}
