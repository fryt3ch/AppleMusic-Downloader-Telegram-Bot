using System.Collections.Concurrent;
using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Downloads;
using Microsoft.Extensions.Options;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Downloads;

public enum CancelDownloadOutcome
{
    CancelledQueued,
    CancellingProcessing,
    NotFound,
    NotOwner,
    AlreadyFinished,
}

public sealed class DownloadQueue
{
    private readonly Lock _gate = new();
    private readonly LinkedList<DownloadRequest> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly ConcurrentDictionary<string, DownloadRequest> _requests = new();
    private readonly int _maxPendingSongs;
    private readonly int _maxConcurrency;

    private int _pendingSongs;
    private int _activeWorkers;

    public DownloadQueue(IOptions<AppSettings> appSettings)
    {
        _maxPendingSongs = appSettings.Value.Telegram.Downloads.MaxPendingSongs;
        _maxConcurrency = appSettings.Value.Telegram.Downloads.MaxConcurrency;
    }

    public int PendingSongs => Volatile.Read(ref _pendingSongs);

    public int QueuedCount
    {
        get
        {
            lock (_gate)
                return _queue.Count;
        }
    }

    public bool TryReserveSongs(int count)
    {
        while (true)
        {
            var current = Volatile.Read(ref _pendingSongs);

            if (current >= _maxPendingSongs)
                return false;

            if (Interlocked.CompareExchange(ref _pendingSongs, current + count, current) == current)
                return true;
        }
    }

    public void ReleaseSongs(int count)
    {
        Interlocked.Add(ref _pendingSongs, -count);
    }

    public bool CanStartImmediately()
    {
        lock (_gate)
            return _activeWorkers < _maxConcurrency && _queue.Count == 0;
    }

    public void Enqueue(DownloadRequest request)
    {
        lock (_gate)
        {
            _queue.AddLast(request);
            request.Position = _queue.Count;
            request.LastReportedPosition = request.Position;
            request.LastReportedTotal = _queue.Count;
            request.Status = DownloadStatus.Queued;
            _requests[request.DownloadId] = request;
        }

        _signal.Release();
    }

    public async Task<DownloadRequest> DequeueAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            await _signal.WaitAsync(cancellationToken);

            lock (_gate)
            {
                while (_queue.First is { } node)
                {
                    var request = node.Value;
                    _queue.RemoveFirst();

                    if (request.Status == DownloadStatus.Cancelled)
                        continue;

                    request.Status = DownloadStatus.Processing;
                    _activeWorkers++;
                    RecalculatePositionsLocked();

                    return request;
                }
            }
        }
    }

    public CancelDownloadOutcome TryCancel(string downloadId, long userId, out DownloadRequest? request)
    {
        request = null;

        if (!_requests.TryGetValue(downloadId, out var found))
            return CancelDownloadOutcome.NotFound;

        if (found.User.Id != userId)
        {
            request = found;
            return CancelDownloadOutcome.NotOwner;
        }

        var outcome = CancelDownloadOutcome.AlreadyFinished;

        lock (_gate)
        {
            if (found.Status == DownloadStatus.Queued)
            {
                _queue.Remove(found);
                found.Status = DownloadStatus.Cancelled;
                _requests.TryRemove(downloadId, out _);
                RecalculatePositionsLocked();
                ReleaseSongs(found.SongCount);
                outcome = CancelDownloadOutcome.CancelledQueued;
            }
            else if (found.Status == DownloadStatus.Processing)
            {
                outcome = CancelDownloadOutcome.CancellingProcessing;
            }
        }

        request = found;

        switch (outcome)
        {
            case CancelDownloadOutcome.CancelledQueued:
                Complete(found);
                found.UserCancellation.Cancel();
                found.UserCancellation.Dispose();
                break;

            case CancelDownloadOutcome.CancellingProcessing:
                found.UserCancellation.Cancel();
                break;
        }

        return outcome;
    }

    public void MarkCompleted(DownloadRequest request)
    {
        lock (_gate)
        {
            if (_activeWorkers > 0)
                _activeWorkers--;

            _requests.TryRemove(request.DownloadId, out _);
            ReleaseSongs(request.SongCount);
            Complete(request);
        }
    }

    public void Abandon(DownloadRequest request)
    {
        lock (_gate)
        {
            _queue.Remove(request);
            _requests.TryRemove(request.DownloadId, out _);
            ReleaseSongs(request.SongCount);
            Complete(request);
        }

        request.UserCancellation.Dispose();
    }

    public IReadOnlyList<DownloadRequest> GetQueuedSnapshot()
    {
        lock (_gate)
            return _queue.ToArray();
    }

    private void Complete(DownloadRequest request)
    {
        if (request.TryMarkCompleted())
            request.OnCompleted?.Invoke();
    }

    private void RecalculatePositionsLocked()
    {
        var position = 1;

        foreach (var item in _queue)
            item.Position = position++;
    }
}
