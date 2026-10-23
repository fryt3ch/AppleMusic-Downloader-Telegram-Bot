using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Downloads;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Requests;
using frytech.AppleMusicTools.Downloader.TelegramBot.Resources;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Downloads;

public sealed class SongDownloadCoordinator
{
    private readonly DownloadQueue _queue;
    private readonly DownloadStatusReporter _statusReporter;
    private readonly ITelegramBotClient _client;
    private readonly AppSettings _appSettings;
    private readonly ILogger<SongDownloadCoordinator> _logger;

    private readonly object _bulkGate = new();
    private readonly Dictionary<long, int> _bulkCounts = new();

    public SongDownloadCoordinator(
        DownloadQueue queue,
        DownloadStatusReporter statusReporter,
        ITelegramBotClient client,
        IOptions<AppSettings> appSettings,
        ILogger<SongDownloadCoordinator> logger)
    {
        _queue = queue;
        _statusReporter = statusReporter;
        _client = client;
        _appSettings = appSettings.Value;
        _logger = logger;
    }

    public Task StartSingleAsync(Chat chat, User user, string songId, string store, ReplyParameters? replyParameters, CancellationToken cancellationToken)
    {
        return EnqueueAsync(chat, user, [songId], songOffsets: [], store, replyParameters, isBulk: false, batchNotice: null, restoreMessage: null, initialResumeOffset: 0, existingStatusMessageId: null, cancellationToken);
    }

    public Task<bool> StartBulkAsync(Chat chat, User user, IReadOnlyCollection<string> songIds, IReadOnlyList<int> songOffsets, string store, ReplyParameters? replyParameters, DownloadBatchNotice? batchNotice, DownloadBatchMessage? restoreMessage, int initialResumeOffset, int? existingStatusMessageId, CancellationToken cancellationToken)
    {
        var maxSongsPerBatch = _appSettings.Telegram.Downloads.MaxSongsPerBatch;

        var limitedSongIds = songIds.Count > maxSongsPerBatch
            ? songIds.Take(maxSongsPerBatch).ToArray()
            : songIds;

        var limitedSongOffsets = songOffsets.Count > maxSongsPerBatch
            ? songOffsets.Take(maxSongsPerBatch).ToArray()
            : songOffsets;

        return EnqueueAsync(chat, user, limitedSongIds, limitedSongOffsets, store, replyParameters, isBulk: true, batchNotice, restoreMessage, initialResumeOffset, existingStatusMessageId, cancellationToken);
    }

    public async Task<CancelDownloadResult> CancelAsync(string downloadId, long userId, CancellationToken cancellationToken)
    {
        var outcome = _queue.TryCancel(downloadId, userId, out var request);

        switch (outcome)
        {
            case CancelDownloadOutcome.CancelledQueued:
                if (request is not null)
                {
                    if (request.RestoreMessage is { } restoreMessage)
                        await _statusReporter.RestoreBatchMessageAsync(request, restoreMessage, request.ResumeOffset, CancellationToken.None);
                    else
                        await _statusReporter.DeleteAsync(request, CancellationToken.None);
                }

                _logger.LogInformation("Queued download {DownloadId} was cancelled by user {UserId}", downloadId, userId);

                return new CancelDownloadResult(true, TelegramMessages.CancelResultCancelled);

            case CancelDownloadOutcome.CancellingProcessing:
                _logger.LogInformation("Processing download {DownloadId} is being cancelled by user {UserId}", downloadId, userId);

                return new CancelDownloadResult(true, TelegramMessages.CancelResultCancelling);

            case CancelDownloadOutcome.NotOwner:
                return new CancelDownloadResult(false, TelegramMessages.CancelResultNotOwner);

            case CancelDownloadOutcome.NotFound:
            case CancelDownloadOutcome.AlreadyFinished:
            default:
                return new CancelDownloadResult(false, TelegramMessages.CancelResultAlreadyFinished);
        }
    }

    private async Task<bool> EnqueueAsync(Chat chat, User user, IReadOnlyCollection<string> songIds, IReadOnlyList<int> songOffsets, string store, ReplyParameters? replyParameters, bool isBulk, DownloadBatchNotice? batchNotice, DownloadBatchMessage? restoreMessage, int initialResumeOffset, int? existingStatusMessageId, CancellationToken cancellationToken)
    {
        var songIdArray = songIds.ToArray();

        if (songIdArray.Length == 0)
            return false;

        var maxPendingBulkPerUser = _appSettings.Telegram.Downloads.MaxPendingBulkPerUser;

        if (isBulk && !TryReserveBulk(user.Id, maxPendingBulkPerUser))
        {
            _logger.LogWarning("Bulk download limit reached for user {UserId} ({Username}) in chat {ChatId}", user.Id, user.Username, chat.Id);

            await _client.SendMessage(
                chat.Id,
                TelegramMessages.PendingDownload,
                replyParameters: replyParameters,
                cancellationToken: cancellationToken);

            return false;
        }

        if (!_queue.TryReserveSongs(songIdArray.Length))
        {
            if (isBulk)
                ReleaseBulk(user.Id);

            _logger.LogWarning("Pending song limit reached for user {UserId} ({Username}) in chat {ChatId}; pending songs: {PendingSongs}",
                user.Id, user.Username, chat.Id, _queue.PendingSongs);

            await SendQueueLimitMessageAsync(chat, replyParameters, cancellationToken);

            return false;
        }

        var downloadId = Guid.NewGuid().ToString("N");
        var startsImmediately = _queue.CanStartImmediately();
        var statusKeyboard = new InlineKeyboardMarkup()
            .AddButton(new InlineKeyboardButton(TelegramButtons.Cancel)
            {
                CallbackData = CallbackData.ForCancelDownload(downloadId),
            });

        var request = new DownloadRequest
        {
            DownloadId = downloadId,
            Chat = chat,
            User = user,
            SongIds = songIdArray,
            Store = store,
            ReplyParameters = replyParameters,
            IsBulk = isBulk,
            BatchNotice = batchNotice,
            RestoreMessage = restoreMessage,
            SongOffsets = songOffsets,
            ResumeOffset = initialResumeOffset,
            WasQueued = !startsImmediately,
            StatusKeyboard = statusKeyboard,
            OnCompleted = isBulk ? () => ReleaseBulk(user.Id) : null,
        };

        try
        {
            _queue.Enqueue(request);
        }
        catch (Exception ex)
        {
            _queue.Abandon(request);

            _logger.LogError(ex, "Failed to enqueue download {DownloadId}", downloadId);
            throw;
        }

        int? statusMessageId = null;

        try
        {
            if (!startsImmediately)
            {
                var statusText = string.Format(TelegramMessages.QueuePosition, request.Position, _queue.QueuedCount);

                statusMessageId = await _statusReporter.SendStatusAsync(request, statusText, statusKeyboard, existingStatusMessageId, cancellationToken);
            }
            else if (existingStatusMessageId is { } existingId)
            {
                statusMessageId = existingId;
            }
        }
        finally
        {
            request.StatusMessageId = statusMessageId;
            request.StatusMessageReady.TrySetResult(statusMessageId);
        }

        _logger.LogInformation(
            "Enqueued {Kind} download {DownloadId} for user {UserId} ({Username}) in chat {ChatId}: {Count} songs, queued={WasQueued}, position={Position}, pendingSongs={PendingSongs}",
            isBulk ? "bulk" : "single", downloadId, user.Id, user.Username, chat.Id, songIdArray.Length, request.WasQueued, request.Position, _queue.PendingSongs);

        return true;
    }

    private async Task SendQueueLimitMessageAsync(Chat chat, ReplyParameters? replyParameters, CancellationToken cancellationToken)
    {
        try
        {
            await _client.SendMessage(
                chat.Id,
                TelegramMessages.QueueLimitReached,
                replyParameters: replyParameters,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send queue limit message to chat {ChatId}", chat.Id);
        }
    }

    private bool TryReserveBulk(long userId, int maxPendingBulkPerUser)
    {
        lock (_bulkGate)
        {
            _bulkCounts.TryGetValue(userId, out var count);

            if (count >= maxPendingBulkPerUser)
                return false;

            _bulkCounts[userId] = count + 1;

            return true;
        }
    }

    private void ReleaseBulk(long userId)
    {
        lock (_bulkGate)
        {
            if (!_bulkCounts.TryGetValue(userId, out var count))
                return;

            if (count <= 1)
                _bulkCounts.Remove(userId);
            else
                _bulkCounts[userId] = count - 1;
        }
    }
}
