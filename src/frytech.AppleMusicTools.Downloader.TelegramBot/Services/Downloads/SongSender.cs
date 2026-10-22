using System.Text;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Exceptions;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Database;
using frytech.AppleMusicTools.Downloader.TelegramBot.Resources;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Caching;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.SongFiles;
using frytech.AppleMusicTools.Downloader.TelegramBot.Telegram;
using KeyedSemaphores;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Downloads;

public class SongSender
{
    private const int MEDIA_BATCH_SIZE = 10;
    
    private readonly ITelegramBotClient _client;
    private readonly ISongStorage _songStorage;
    private readonly ISongFileProvider _songFileProvider;
    private readonly ILogger<SongSender> _logger;
    private readonly TimeSpan _progressUpdateInterval;

    public SongSender(
        ITelegramBotClient client,
        ISongStorage songStorage,
        ISongFileProvider songFileProvider,
        IOptions<AppSettings> appSettings,
        ILogger<SongSender> logger)
    {
        _client = client;
        _songStorage = songStorage;
        _songFileProvider = songFileProvider;
        _logger = logger;
        _progressUpdateInterval = TimeSpan.FromSeconds(appSettings.Value.Telegram.ProgressUpdateIntervalSeconds);
    }

    public async Task SendSongs(Chat chat, IEnumerable<string> songIds, string store, ReplyParameters? replyParameters, CancellationToken cancellationToken)
    {
        var songIdsArray = songIds.ToArray();

        if (songIdsArray.Length == 0)
            return;

        var cachedSongs = new List<CachedSong>();
        var failedSongs = new List<SongDownloadFailedException>();
        var progress = new DownloadProgressReporter(_client, chat, replyParameters, _progressUpdateInterval, _logger);

        _logger.LogInformation("Starting download of {Count} songs in chat {ChatId}", songIdsArray.Length, chat.Id);

        try
        {
            await progress.StartAsync(songIdsArray.Length, cancellationToken);

            for (var i = 0; i < songIdsArray.Length; i++)
            {
                var songId = songIdsArray[i];

                _logger.LogDebug("Processing song {SongId}", songId);

                using (await KeyedSemaphore.LockAsync(songId, cancellationToken))
                {
                    try
                    {
                        var cachedSong = await _songStorage.GetOrAddSong(
                            songId,
                            store,
                            ct => _songFileProvider.GetSongFileAsync(songId, store, ct),
                            cancellationToken);

                        cachedSongs.Add(cachedSong);
                    }
                    catch (SongDownloadFailedException exception)
                    {
                        _logger.LogWarning(exception, "Skipping song {SongId} because it could not be downloaded.", songId);

                        failedSongs.Add(exception);
                    }
                }

                await progress.ReportAsync(i + 1, songIdsArray.Length, cancellationToken);
            }
            
            var batches = cachedSongs
                .Select((dataElement, index) => new { dataElement, index })
                .GroupBy(x => x.index / MEDIA_BATCH_SIZE, x => x.dataElement)
                .ToArray();

            var sentSongsAmount = 0;

            foreach (var batch in batches)
            {
                var batchArray = batch.ToArray();

                if (batches.Length > 1)
                    await progress.ReportSendingAsync(sentSongsAmount, songIdsArray.Length, cancellationToken);

                await TelegramApiRetry.ExecuteAsync(
                    requestToken => _client.SendMediaGroup(
                        chat.Id,
                        batchArray.Select(CreateInputMediaAudio),
                        disableNotification: true,
                        replyParameters: replyParameters,
                        cancellationToken: requestToken),
                    _logger,
                    cancellationToken);

                sentSongsAmount += batchArray.Length;
            }

            await progress.CompleteAsync();

            _logger.LogInformation(
                "Finished download in chat {ChatId}: {Sent} sent, {Failed} failed",
                chat.Id,
                sentSongsAmount,
                failedSongs.Count);

            if (failedSongs.Count > 0)
                await SendFailedSongsReport(chat, failedSongs, replyParameters, cancellationToken);
        }
        finally
        {
            await progress.CompleteAsync();
        }
    }

    private async Task SendFailedSongsReport(Chat chat, IReadOnlyCollection<SongDownloadFailedException> failedSongs, ReplyParameters? replyParameters, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Format(TelegramMessages.FailedSongsHeader, failedSongs.Count));

        foreach (var failedSong in failedSongs)
            builder.AppendLine(string.Format(TelegramMessages.FailedSongLine, failedSong.ArtistName, failedSong.Name));

        await TelegramApiRetry.ExecuteAsync(
            requestToken => _client.SendMessage(
                chat.Id,
                builder.ToString(),
                replyParameters: replyParameters,
                disableNotification: true,
                cancellationToken: requestToken),
            _logger,
            cancellationToken);
    }

    private static InputMediaAudio CreateInputMediaAudio(CachedSong cachedSong)
    {
        return new InputMediaAudio(InputFile.FromFileId(cachedSong.FileId))
        {
            Title = cachedSong.Name,
            Performer = cachedSong.ArtistName,
            Duration = (int)cachedSong.Duration.TotalSeconds,
        };
    }
}
