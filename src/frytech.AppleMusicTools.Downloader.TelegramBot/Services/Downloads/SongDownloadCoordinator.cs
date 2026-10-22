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

    public Task StartSingleAsync(Chat chat, IReadOnlyCollection<string> songIds, string store, ReplyParameters? replyParameters)
    {
        var downloadCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.ApplicationStopping);

        _logger.LogInformation("Enqueued single download of {Count} songs in chat {ChatId}", songIds.Count, chat.Id);

        RunAsync(chat, songIds, store, replyParameters, downloadCts, onCompleted: null);

        return Task.CompletedTask;
    }

    public async Task StartBulkAsync(Chat chat, User user, IReadOnlyCollection<string> songIds, string store, ReplyParameters? replyParameters, CancellationToken cancellationToken)
    {
        var downloadCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.ApplicationStopping);

        if (!_pendingBulkDownloads.TryAdd(user.Id, downloadCts))
        {
            downloadCts.Dispose();

            _logger.LogWarning("Bulk download already pending for user {UserId} in chat {ChatId}", user.Id, chat.Id);

            await _client.SendMessage(
                chat.Id,
                TelegramMessages.PendingDownload,
                replyParameters: replyParameters,
                cancellationToken: cancellationToken);

            return;
        }

        _logger.LogInformation(
            "Enqueued bulk download of {Count} songs for user {UserId} in chat {ChatId}",
            songIds.Count,
            user.Id,
            chat.Id);

        RunAsync(chat, songIds, store, replyParameters, downloadCts,
            onCompleted: () => _pendingBulkDownloads.TryRemove(user.Id, out _));
    }

    private void RunAsync(
        Chat chat,
        IReadOnlyCollection<string> songIds,
        string store,
        ReplyParameters? replyParameters,
        CancellationTokenSource downloadCts,
        Action? onCompleted)
    {
        _ = Task.Run(async () =>
        {
            _logger.LogDebug("Starting background download in chat {ChatId}", chat.Id);

            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var songSender = scope.ServiceProvider.GetRequiredService<SongSender>();

                await songSender.SendSongs(chat, songIds, store, replyParameters, downloadCts.Token);
            }
            catch (OperationCanceledException) when (downloadCts.IsCancellationRequested)
            {
                _logger.LogInformation("Download in chat {ChatId} was cancelled", chat.Id);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to send songs to chat {ChatId}", chat.Id);

                try
                {
                    await _client.SendMessage(chat.Id, TelegramMessages.DownloadFailed);
                }
                catch (Exception sendException)
                {
                    _logger.LogError(sendException, "Failed to report a download error to chat {ChatId}", chat.Id);
                }
            }
            finally
            {
                onCompleted?.Invoke();

                downloadCts.Dispose();

                _logger.LogDebug("Background download in chat {ChatId} completed", chat.Id);
            }
        });
    }
}
