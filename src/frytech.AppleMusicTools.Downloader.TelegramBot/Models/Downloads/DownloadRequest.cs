using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Models.Downloads;

public sealed class DownloadRequest
{
    public required string DownloadId { get; init; }

    public required Chat Chat { get; init; }

    public required User User { get; init; }

    public required IReadOnlyCollection<string> SongIds { get; init; }

    public required string Store { get; init; }

    public ReplyParameters? ReplyParameters { get; init; }

    public DownloadBatchNotice? BatchNotice { get; init; }

    public DownloadBatchMessage? RestoreMessage { get; init; }

    public bool IsBulk { get; init; }

    public Action? OnCompleted { get; init; }

    public int SongCount => SongIds.Count;

    public CancellationTokenSource UserCancellation { get; } = new();

    public TaskCompletionSource<int?> StatusMessageReady { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public DownloadStatus Status { get; set; } = DownloadStatus.Queued;

    public int Position { get; set; }

    public int LastReportedPosition { get; set; }

    public int LastReportedTotal { get; set; }

    public bool WasQueued { get; set; }

    public int? StatusMessageId { get; set; }

    public InlineKeyboardMarkup? StatusKeyboard { get; set; }

    private int _completed;

    public bool TryMarkCompleted()
    {
        return Interlocked.Exchange(ref _completed, 1) == 0;
    }
}
