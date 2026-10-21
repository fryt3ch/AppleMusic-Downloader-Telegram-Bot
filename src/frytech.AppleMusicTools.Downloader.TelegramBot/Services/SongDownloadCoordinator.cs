using System.Collections.Concurrent;
using frytech.AppleMusicTools.Downloader.TelegramBot.Resources;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services;

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

        RunAsync(chat, songIds, store, replyParameters, downloadCts, onCompleted: null);

        return Task.CompletedTask;
    }

    public async Task StartBulkAsync(Chat chat, User user, IReadOnlyCollection<string> songIds, string store, ReplyParameters? replyParameters, CancellationToken cancellationToken)
    {
        var downloadCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.ApplicationStopping);

        if (!_pendingBulkDownloads.TryAdd(user.Id, downloadCts))
        {
            downloadCts.Dispose();

            await _client.SendMessage(
                chat.Id,
                TelegramMessages.PendingDownload,
                replyParameters: replyParameters,
                cancellationToken: cancellationToken);

            return;
        }

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
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var songSender = scope.ServiceProvider.GetRequiredService<SongSender>();

                await songSender.SendSongs(chat, songIds, store, replyParameters, downloadCts.Token);
            }
            catch (OperationCanceledException) when (downloadCts.IsCancellationRequested)
            {
                // Application is stopping; nothing to report.
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
            }
        });
    }
}
