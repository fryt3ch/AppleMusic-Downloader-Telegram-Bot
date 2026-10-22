using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Downloads;
using frytech.AppleMusicTools.Downloader.TelegramBot.Resources;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Downloads;

public sealed class SongDownloadWorker : BackgroundService
{
    private static readonly TimeSpan StatusMessageTimeout = TimeSpan.FromSeconds(30);

    private readonly DownloadQueue _queue;
    private readonly DownloadStatusReporter _statusReporter;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITelegramBotClient _client;
    private readonly ILogger<SongDownloadWorker> _logger;
    private readonly int _maxConcurrency;

    public SongDownloadWorker(
        DownloadQueue queue,
        DownloadStatusReporter statusReporter,
        IServiceScopeFactory scopeFactory,
        ITelegramBotClient client,
        IOptions<AppSettings> appSettings,
        ILogger<SongDownloadWorker> logger)
    {
        _queue = queue;
        _statusReporter = statusReporter;
        _scopeFactory = scopeFactory;
        _client = client;
        _logger = logger;
        _maxConcurrency = appSettings.Value.Telegram.Downloads.MaxConcurrency;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workers = Enumerable.Range(0, _maxConcurrency)
            .Select(_ => RunWorkerAsync(stoppingToken))
            .ToArray();

        await Task.WhenAll(workers);
    }

    private async Task RunWorkerAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            DownloadRequest request;

            try
            {
                request = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await ProcessAsync(request, stoppingToken);
        }
    }

    private async Task ProcessAsync(DownloadRequest request, CancellationToken stoppingToken)
    {
        using var loggingScope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["DownloadId"] = request.DownloadId,
            ["UserId"] = request.User.Id,
            ["Username"] = request.User.Username,
            ["ChatId"] = request.Chat.Id,
            ["SongCount"] = request.SongCount,
            ["Store"] = request.Store,
            ["IsBulk"] = request.IsBulk,
        });

        int? statusMessageId = null;

        try
        {
            statusMessageId = await request.StatusMessageReady.Task.WaitAsync(StatusMessageTimeout, stoppingToken);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timed out waiting for the download status message for {DownloadId}", request.DownloadId);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _queue.MarkCompleted(request);
            request.UserCancellation.Dispose();

            return;
        }

        if (statusMessageId is not null && request.WasQueued)
        {
            await _statusReporter.EditStatusAsync(request, "⬇️ Downloading…", request.StatusKeyboard, CancellationToken.None);
        }

        using var downloadCts = CancellationTokenSource.CreateLinkedTokenSource(
            stoppingToken,
            request.UserCancellation.Token);

        try
        {
            await using var serviceScope = _scopeFactory.CreateAsyncScope();

            var songSender = serviceScope.ServiceProvider.GetRequiredService<SongSender>();

            await songSender.SendSongs(
                request.Chat,
                request.SongIds,
                request.Store,
                request.ReplyParameters,
                statusMessageId,
                request.StatusKeyboard,
                downloadCts.Token,
                stoppingToken);

            request.Status = DownloadStatus.Completed;
        }
        catch (OperationCanceledException) when (request.UserCancellation.IsCancellationRequested)
        {
            _logger.LogInformation("Download {DownloadId} was cancelled by the user", request.DownloadId);

            request.Status = DownloadStatus.Cancelled;

            await _statusReporter.FinalizeAsync(request, "❌ Cancelled", CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Download {DownloadId} was cancelled because the host is stopping", request.DownloadId);

            request.Status = DownloadStatus.Cancelled;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to process download {DownloadId}", request.DownloadId);

            request.Status = DownloadStatus.Failed;

            await _statusReporter.FinalizeAsync(request, "❌ Failed", CancellationToken.None);

            try
            {
                await _client.SendMessage(
                    request.Chat.Id,
                    TelegramMessages.DownloadFailed,
                    cancellationToken: CancellationToken.None);
            }
            catch (Exception sendException)
            {
                _logger.LogWarning(sendException, "Failed to report a download error for {DownloadId}", request.DownloadId);
            }
        }
        finally
        {
            _queue.MarkCompleted(request);
            request.UserCancellation.Dispose();
        }
    }
}
