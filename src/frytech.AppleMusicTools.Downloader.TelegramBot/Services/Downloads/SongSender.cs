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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SongSender> _logger;
    private readonly TimeSpan _progressUpdateInterval;
    private readonly int _providerConcurrency;
    private readonly SemaphoreSlim _progressGate = new(1, 1);

    public SongSender(
        ITelegramBotClient client,
        IServiceScopeFactory scopeFactory,
        IOptions<AppSettings> appSettings,
        ILogger<SongSender> logger)
    {
        _client = client;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _progressUpdateInterval = TimeSpan.FromSeconds(appSettings.Value.Telegram.ProgressUpdateIntervalSeconds);
        _providerConcurrency = Math.Max(1, appSettings.Value.Downloader.MaxConcurrency);
    }

    public async Task SendSongs(
        Chat chat,
        IReadOnlyCollection<string> songIds,
        string store,
        ReplyParameters? replyParameters,
        int? statusMessageId,
        InlineKeyboardMarkup? statusKeyboard,
        Action<int?>? statusMessageIdChanged,
        IReadOnlyList<int>? songOffsets,
        Action<int>? resumeOffsetChanged,
        CancellationToken downloadToken,
        CancellationToken sendToken)
    {
        var songIdsArray = songIds.ToArray();

        if (songIdsArray.Length == 0)
            return;

        var offsets = songOffsets is not null && songOffsets.Count == songIdsArray.Length
            ? songOffsets
            : null;

        var pendingBatch = new List<CachedSong>();
        var failedSongs = new List<SongDownloadFailedException>();
        int? partialResumeOffset = null;
        var progress = new DownloadProgressReporter(_client, chat, replyParameters, statusMessageId, statusKeyboard, statusMessageIdChanged, _progressUpdateInterval, _logger);

        _logger.LogInformation("Starting download of {Count} songs in chat {ChatId}", songIdsArray.Length, chat.Id);

        void UpdateResumeOffset(int index)
        {
            if (offsets is not null)
                resumeOffsetChanged?.Invoke(offsets[index]);
        }

        try
        {
            await progress.StartAsync(songIdsArray.Length, failed: 0, sendToken);

            var processed = 0;
            var failedCount = 0;

            async Task ReportProgressAsync(int total, CancellationToken cancellationToken)
            {
                await _progressGate.WaitAsync(cancellationToken);

                try
                {
                    await progress.ReportAsync(Volatile.Read(ref processed), total, Volatile.Read(ref failedCount), cancellationToken);
                }
                finally
                {
                    _progressGate.Release();
                }
            }

            for (var offset = 0; offset < songIdsArray.Length; offset += MEDIA_BATCH_SIZE)
            {
                var chunkLength = Math.Min(MEDIA_BATCH_SIZE, songIdsArray.Length - offset);
                var chunk = new string[chunkLength];

                Array.Copy(songIdsArray, offset, chunk, 0, chunkLength);

                var cachedResults = new CachedSong?[chunkLength];
                var failures = new SongDownloadFailedException?[chunkLength];

                try
                {
                    await Parallel.ForEachAsync(
                        Enumerable.Range(0, chunkLength),
                        new ParallelOptions
                        {
                            MaxDegreeOfParallelism = _providerConcurrency,
                            CancellationToken = downloadToken,
                        },
                        async (index, cancellationToken) =>
                        {
                            var songId = chunk[index];

                            _logger.LogDebug("Processing song {SongId}", songId);

                            try
                            {
                                cachedResults[index] = await DownloadAndCacheAsync(songId, store, cancellationToken);
                            }
                            catch (SongDownloadFailedException ex)
                            {
                                _logger.LogWarning(ex, "Skipping song {SongId} because it could not be downloaded.", songId);

                                failures[index] = ex;
                            }

                            Interlocked.Increment(ref processed);

                            if (failures[index] is not null)
                                Interlocked.Increment(ref failedCount);

                            await ReportProgressAsync(songIdsArray.Length, sendToken);
                        });
                }
                catch (OperationCanceledException)
                {
                    var lastCachedIndex = -1;

                    for (var i = 0; i < cachedResults.Length; i++)
                    {
                        if (cachedResults[i] is { } cachedSong)
                        {
                            pendingBatch.Add(cachedSong);
                            lastCachedIndex = i;
                        }
                    }

                    if (lastCachedIndex >= 0 && offsets is not null)
                        partialResumeOffset = offsets[offset + lastCachedIndex];

                    throw;
                }

                var lastSentIndex = -1;

                for (var i = 0; i < cachedResults.Length; i++)
                {
                    if (cachedResults[i] is { } cachedSong)
                    {
                        pendingBatch.Add(cachedSong);
                        lastSentIndex = i;
                    }

                    if (failures[i] is { } failure)
                        failedSongs.Add(failure);
                }

                await SendBatchAsync(chat, pendingBatch, replyParameters, sendToken);
                pendingBatch.Clear();

                if (lastSentIndex >= 0)
                    UpdateResumeOffset(offset + lastSentIndex);

                if (offset + chunkLength < songIdsArray.Length)
                {
                    await _progressGate.WaitAsync(sendToken);

                    try
                    {
                        await progress.MoveToBottomAsync(sendToken);
                    }
                    finally
                    {
                        _progressGate.Release();
                    }
                }
            }

            await progress.CompleteAsync(deleteMessage: true);

            _logger.LogInformation("Finished download in chat {ChatId}: {Failed} failed", chat.Id, failedSongs.Count);

            if (failedSongs.Count > 0)
                await SendFailedSongsReport(chat, failedSongs, replyParameters, sendToken);
        }
        catch (OperationCanceledException) when (!sendToken.IsCancellationRequested)
        {
            _logger.LogInformation("Download in chat {ChatId} was cancelled: {Pending} downloaded, {Failed} failed; sending {Pending} already downloaded songs",
                chat.Id,
                pendingBatch.Count,
                failedSongs.Count,
                pendingBatch.Count);

            var pendingSent = await TrySendBatchAsync(chat, pendingBatch, replyParameters, sendToken);
            pendingBatch.Clear();

            if (pendingSent && partialResumeOffset is { } resumeOffset)
                resumeOffsetChanged?.Invoke(resumeOffset);

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

    private async Task<CachedSong> DownloadAndCacheAsync(string songId, string store, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var songStorage = scope.ServiceProvider.GetRequiredService<ISongStorage>();
        var songFileProvider = scope.ServiceProvider.GetRequiredService<ISongFileProvider>();

        return await songStorage.GetOrAddSong(songId, store, ct => songFileProvider.GetSongFileAsync(songId, store, ct), cancellationToken);
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

    private async Task<bool> TrySendBatchAsync(Chat chat, IReadOnlyCollection<CachedSong> batch, ReplyParameters? replyParameters, CancellationToken sendToken)
    {
        if (batch.Count == 0)
        {
            _logger.LogDebug("No already-downloaded songs to send in chat {ChatId}", chat.Id);

            return true;
        }

        try
        {
            await SendBatchAsync(chat, batch, replyParameters, sendToken);

            return true;
        }
        catch (OperationCanceledException)
        {
            // Host is stopping; best-effort.
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send partially downloaded songs in chat {ChatId}", chat.Id);

            return false;
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
