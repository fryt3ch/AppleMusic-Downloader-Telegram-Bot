using System.Text;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Exceptions;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Database;
using frytech.AppleMusicTools.Downloader.TelegramBot.Resources;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Caching;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.SongFiles;
using frytech.AppleMusicTools.Downloader.TelegramBot.Telegram;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

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

    public async Task SendSongs(
        Chat chat,
        IReadOnlyCollection<string> songIds,
        string store,
        ReplyParameters? replyParameters,
        int? statusMessageId,
        InlineKeyboardMarkup? statusKeyboard,
        CancellationToken downloadToken,
        CancellationToken sendToken)
    {
        var songIdsArray = songIds.ToArray();

        if (songIdsArray.Length == 0)
            return;

        var pendingBatch = new List<CachedSong>();
        var failedSongs = new List<SongDownloadFailedException>();
        var progress = new DownloadProgressReporter(
            _client,
            chat,
            replyParameters,
            statusMessageId,
            statusKeyboard,
            _progressUpdateInterval,
            _logger);

        _logger.LogInformation("Starting download of {Count} songs in chat {ChatId}", songIdsArray.Length, chat.Id);

        try
        {
            await progress.StartAsync(songIdsArray.Length, sendToken);

            for (var i = 0; i < songIdsArray.Length; i++)
            {
                var songId = songIdsArray[i];

                _logger.LogDebug("Processing song {SongId}", songId);

                try
                {
                    var cachedSong = await _songStorage.GetOrAddSong(
                        songId,
                        store,
                        ct => _songFileProvider.GetSongFileAsync(songId, store, ct),
                        downloadToken);

                    pendingBatch.Add(cachedSong);

                    if (pendingBatch.Count >= MEDIA_BATCH_SIZE)
                    {
                        await SendBatchAsync(chat, pendingBatch, replyParameters, sendToken);
                        pendingBatch.Clear();
                    }
                }
                catch (SongDownloadFailedException ex)
                {
                    _logger.LogWarning(ex, "Skipping song {SongId} because it could not be downloaded.", songId);

                    failedSongs.Add(ex);
                }

                await progress.ReportAsync(i + 1, songIdsArray.Length, sendToken);
            }

            await SendBatchAsync(chat, pendingBatch, replyParameters, sendToken);
            pendingBatch.Clear();

            await progress.CompleteAsync(deleteMessage: true);

            _logger.LogInformation("Finished download in chat {ChatId}: {Failed} failed", chat.Id, failedSongs.Count);

            if (failedSongs.Count > 0)
                await SendFailedSongsReport(chat, failedSongs, replyParameters, sendToken);
        }
        catch (OperationCanceledException) when (!sendToken.IsCancellationRequested)
        {
            _logger.LogInformation("Download in chat {ChatId} was cancelled; sending already downloaded songs", chat.Id);

            await TrySendBatchAsync(chat, pendingBatch, replyParameters, sendToken);
            pendingBatch.Clear();

            await progress.CompleteAsync(deleteMessage: false);

            throw;
        }
        catch (OperationCanceledException)
        {
            await progress.CompleteAsync(deleteMessage: false);

            throw;
        }
        catch
        {
            await progress.CompleteAsync(deleteMessage: false);

            throw;
        }
    }

    private async Task SendBatchAsync(Chat chat, IReadOnlyCollection<CachedSong> batch, ReplyParameters? replyParameters, CancellationToken sendToken)
    {
        if (batch.Count == 0)
            return;

        await TelegramApiRetry.ExecuteAsync(
            requestToken => _client.SendMediaGroup(
                chat.Id,
                batch.Select(CreateInputMediaAudio),
                disableNotification: true,
                replyParameters: replyParameters,
                cancellationToken: requestToken),
            _logger,
            sendToken);
    }

    private async Task TrySendBatchAsync(Chat chat, IReadOnlyCollection<CachedSong> batch, ReplyParameters? replyParameters, CancellationToken sendToken)
    {
        try
        {
            await SendBatchAsync(chat, batch, replyParameters, sendToken);
        }
        catch (OperationCanceledException)
        {
            // Host is stopping; best-effort.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send partially downloaded songs in chat {ChatId}", chat.Id);
        }
    }

    private async Task SendFailedSongsReport(Chat chat, IReadOnlyCollection<SongDownloadFailedException> failedSongs, ReplyParameters? replyParameters, CancellationToken sendToken)
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
            sendToken);
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
