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
        return EnqueueAsync(chat, user, [songId], store, replyParameters, isBulk: false, cancellationToken);
    }

    public Task StartBulkAsync(Chat chat, User user,
        IReadOnlyCollection<string> songIds,
        string store,
        ReplyParameters? replyParameters,
        CancellationToken cancellationToken)
    {
        return EnqueueAsync(chat, user, songIds, store, replyParameters, isBulk: true, cancellationToken);
    }

    public async Task<CancelDownloadResult> CancelAsync(string downloadId, long userId, CancellationToken cancellationToken)
    {
        var outcome = _queue.TryCancel(downloadId, userId, out var request);

        switch (outcome)
        {
            case CancelDownloadOutcome.CancelledQueued:
                if (request is not null)
                {
                    await _statusReporter.DeleteAsync(request, CancellationToken.None);
                }

                _logger.LogInformation("Queued download {DownloadId} was cancelled by user {UserId}", downloadId, userId);

                return new CancelDownloadResult(true, "Cancelled");

            case CancelDownloadOutcome.CancellingProcessing:
                _logger.LogInformation("Processing download {DownloadId} is being cancelled by user {UserId}", downloadId, userId);

                return new CancelDownloadResult(true, "Cancelling...");

            case CancelDownloadOutcome.NotOwner:
                return new CancelDownloadResult(false, "Not your download");

            case CancelDownloadOutcome.NotFound:
            case CancelDownloadOutcome.AlreadyFinished:
            default:
                return new CancelDownloadResult(false, "Already finished");
        }
    }

    private async Task EnqueueAsync(Chat chat, User user, IReadOnlyCollection<string> songIds, string store, ReplyParameters? replyParameters, bool isBulk, CancellationToken cancellationToken)
    {
        var songIdArray = songIds.ToArray();

        if (songIdArray.Length == 0)
            return;

        var maxPendingBulkPerUser = _appSettings.Telegram.Downloads.MaxPendingBulkPerUser;

        if (isBulk && !TryReserveBulk(user.Id, maxPendingBulkPerUser))
        {
            _logger.LogWarning("Bulk download limit reached for user {UserId} ({Username}) in chat {ChatId}", user.Id, user.Username, chat.Id);

            await _client.SendMessage(
                chat.Id,
                TelegramMessages.PendingDownload,
                replyParameters: replyParameters,
                cancellationToken: cancellationToken);

            return;
        }

        if (!_queue.TryReserveSongs(songIdArray.Length))
        {
            if (isBulk)
                ReleaseBulk(user.Id);

            _logger.LogWarning("Pending song limit reached for user {UserId} ({Username}) in chat {ChatId}; pending songs: {PendingSongs}",
                user.Id, user.Username, chat.Id, _queue.PendingSongs);

            await SendQueueLimitMessageAsync(chat, replyParameters, cancellationToken);

            return;
        }

        var downloadId = Guid.NewGuid().ToString("N");
        var startsImmediately = _queue.CanStartImmediately();
        var statusKeyboard = new InlineKeyboardMarkup()
            .AddButton(new InlineKeyboardButton("Cancel")
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
            var statusText = startsImmediately
                ? "⬇️ Downloading…"
                : $"⏳ Queued: position {request.Position} of {_queue.QueuedCount}";

            statusMessageId = await _statusReporter.SendStatusAsync(
                request,
                statusText,
                statusKeyboard,
                cancellationToken);
        }
        finally
        {
            request.StatusMessageId = statusMessageId;
            request.StatusMessageReady.TrySetResult(statusMessageId);
        }

        _logger.LogInformation(
            "Enqueued {Kind} download {DownloadId} for user {UserId} ({Username}) in chat {ChatId}: {Count} songs, queued={WasQueued}, position={Position}, pendingSongs={PendingSongs}",
            isBulk ? "bulk" : "single", downloadId, user.Id, user.Username, chat.Id, songIdArray.Length, request.WasQueued, request.Position, _queue.PendingSongs);
    }

    private async Task SendQueueLimitMessageAsync(Chat chat, ReplyParameters? replyParameters, CancellationToken cancellationToken)
    {
        try
        {
            await _client.SendMessage(
                chat.Id,
                "Too many songs are already queued. Please try again later.",
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
