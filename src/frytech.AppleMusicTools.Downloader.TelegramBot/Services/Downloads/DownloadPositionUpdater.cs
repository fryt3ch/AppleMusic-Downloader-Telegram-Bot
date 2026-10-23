using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Downloads;
using frytech.AppleMusicTools.Downloader.TelegramBot.Resources;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Downloads;

public sealed class DownloadPositionUpdater : BackgroundService
{
    private const int MaxEditsPerTick = 20;
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(3);

    private readonly DownloadQueue _queue;
    private readonly DownloadStatusReporter _statusReporter;
    private readonly ILogger<DownloadPositionUpdater> _logger;

    public DownloadPositionUpdater(
        DownloadQueue queue,
        DownloadStatusReporter statusReporter,
        ILogger<DownloadPositionUpdater> logger)
    {
        _queue = queue;
        _statusReporter = statusReporter;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(UpdateInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var queued = _queue.GetQueuedSnapshot();

                if (queued.Count == 0)
                    continue;

                var total = queued.Count;
                var edits = 0;

                foreach (var request in queued)
                {
                    if (request.Status != DownloadStatus.Queued)
                        continue;

                    if (request.StatusMessageId is null)
                        continue;

                    if (request.Position == request.LastReportedPosition &&
                        total == request.LastReportedTotal)
                    {
                        continue;
                    }

                    var text = string.Format(TelegramMessages.QueuePosition, request.Position, total);

                    await _statusReporter.EditStatusAsync(request, text, request.StatusKeyboard, stoppingToken);

                    request.LastReportedPosition = request.Position;
                    request.LastReportedTotal = total;

                    if (++edits >= MaxEditsPerTick)
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }
}
