using System.Collections.Concurrent;
using frytech.AppleMusicTools.Downloader.TelegramBot.Resources;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Downloads;

public sealed class SongDownloadCoordinator
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITelegramBotClient _client;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<SongDownloadCoordinator> _logger;

    private readonly ConcurrentDictionary<long, CancellationTokenSource> _pendingBulkDownloads = new();

    public SongDownloadCoordinator(
        IServiceScopeFactory scopeFactory,
        ITelegramBotClient client,
        IHostApplicationLifetime lifetime,
        ILogger<SongDownloadCoordinator> logger)
    {
        _scopeFactory = scopeFactory;
        _client = client;
        _lifetime = lifetime;
        _logger = logger;
    }

    public Task StartSingleAsync(Chat chat, User user, string songId, string store, ReplyParameters? replyParameters, CancellationToken cancellationToken)
    {
        var downloadCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.ApplicationStopping);
        var downloadId = Guid.NewGuid().ToString("N");

        _logger.LogInformation(
            "Enqueued single download {DownloadId} for user {UserId} ({Username}) in chat {ChatId}",
            downloadId,
            user.Id,
            user.Username,
            chat.Id);

        RunAsync(chat, user, downloadId, [songId], store, replyParameters, downloadCts, onCompleted: null);

        return Task.CompletedTask;
    }

    public async Task StartBulkAsync(Chat chat, User user, IReadOnlyCollection<string> songIds, string store, ReplyParameters? replyParameters, CancellationToken cancellationToken)
    {
        var downloadCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.ApplicationStopping);
        var downloadId = Guid.NewGuid().ToString("N");

        if (!_pendingBulkDownloads.TryAdd(user.Id, downloadCts))
        {
            downloadCts.Dispose();

            _logger.LogWarning(
                "Bulk download already pending for user {UserId} ({Username}) in chat {ChatId}",
                user.Id,
                user.Username,
                chat.Id);

            await _client.SendMessage(
                chat.Id,
                TelegramMessages.PendingDownload,
                replyParameters: replyParameters,
                cancellationToken: cancellationToken);

            return;
        }

        _logger.LogInformation(
            "Enqueued bulk download {DownloadId} of {Count} songs for user {UserId} ({Username}) in chat {ChatId}",
            downloadId,
            songIds.Count,
            user.Id,
            user.Username,
            chat.Id);

        RunAsync(chat, user, downloadId, songIds, store, replyParameters, downloadCts, onCompleted: () => _pendingBulkDownloads.TryRemove(user.Id, out _));
    }

    private void RunAsync(
        Chat chat,
        User user,
        string downloadId,
        IReadOnlyCollection<string> songIds,
        string store,
        ReplyParameters? replyParameters,
        CancellationTokenSource downloadCts,
        Action? onCompleted)
    {
        _ = Task.Run(async () =>
        {
            using var loggingScope = _logger.BeginScope(new Dictionary<string, object?>
            {
                ["DownloadId"] = downloadId,
                ["UserId"] = user.Id,
                ["Username"] = user.Username,
                ["ChatId"] = chat.Id,
                ["SongCount"] = songIds.Count,
                ["Store"] = store,
            });

            _logger.LogDebug("Starting background download {DownloadId}", downloadId);

            try
            {
                await using var serviceScope = _scopeFactory.CreateAsyncScope();

                var songSender = serviceScope.ServiceProvider.GetRequiredService<SongSender>();

                await songSender.SendSongs(chat, songIds, store, replyParameters, downloadCts.Token);
            }
            catch (OperationCanceledException) when (downloadCts.IsCancellationRequested)
            {
                _logger.LogInformation("Download {DownloadId} was cancelled", downloadId);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to send songs for download {DownloadId}", downloadId);

                try
                {
                    await _client.SendMessage(chat.Id, TelegramMessages.DownloadFailed);
                }
                catch (Exception sendException)
                {
                    _logger.LogError(sendException, "Failed to report a download error for {DownloadId}", downloadId);
                }
            }
            finally
            {
                onCompleted?.Invoke();

                downloadCts.Dispose();

                _logger.LogDebug("Background download {DownloadId} completed", downloadId);
            }
        });
    }
}
