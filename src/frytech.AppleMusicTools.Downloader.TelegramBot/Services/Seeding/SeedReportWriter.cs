namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Seeding;

public sealed class SeedReportWriter
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ILogger<SeedReportWriter> _logger;

    public SeedReportWriter(ILogger<SeedReportWriter> logger)
    {
        _logger = logger;
    }

    public Task ReportUnmatchedArtistAsync(string artistName, string? bestCandidate, double score, CancellationToken cancellationToken)
    {
        return AppendAsync(
            "data/seed-unmatched-artists.txt",
            $"{DateTimeOffset.UtcNow:O}\t{artistName}\t{bestCandidate ?? string.Empty}\t{score:0.000}",
            cancellationToken);
    }

    public Task ReportFailedSongAsync(string artistName, string? songId, string? songName, string error, CancellationToken cancellationToken)
    {
        return AppendAsync(
            "data/seed-failed-songs.txt",
            $"{DateTimeOffset.UtcNow:O}\t{artistName}\t{songId ?? string.Empty}\t{songName ?? string.Empty}\t{error}",
            cancellationToken);
    }

    private async Task AppendAsync(string path, string line, CancellationToken cancellationToken)
    {
        var resolvedPath = SeedPathResolver.ResolveWritePath(path);

        await _gate.WaitAsync(cancellationToken);

        try
        {
            SeedPathResolver.EnsureParentDirectory(resolvedPath);
            await File.AppendAllTextAsync(resolvedPath, line + Environment.NewLine, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to append seed report to {Path}", resolvedPath);
        }
        finally
        {
            _gate.Release();
        }
    }
}
