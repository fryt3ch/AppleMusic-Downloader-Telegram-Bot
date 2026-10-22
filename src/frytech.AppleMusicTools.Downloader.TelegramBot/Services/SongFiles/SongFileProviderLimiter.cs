using frytech.AppleMusicTools.Downloader.TelegramBot.Configuration;
using Microsoft.Extensions.Options;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.SongFiles;

public sealed class SongFileProviderLimiter
{
    private readonly SemaphoreSlim _semaphore;
    private readonly TimeSpan _delay;

    public SongFileProviderLimiter(IOptions<AppSettings> appSettings)
    {
        var settings = appSettings.Value.Downloader;

        _semaphore = new SemaphoreSlim(settings.MaxConcurrency, settings.MaxConcurrency);
        _delay = TimeSpan.FromSeconds(settings.DelayAfterDownloadSeconds);
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            return await action();
        }
        finally
        {
            if (cancellationToken.IsCancellationRequested || _delay <= TimeSpan.Zero)
            {
                _semaphore.Release();
            }
            else
            {
                _ = ReleaseAfterDelayAsync();
            }
        }
    }

    private async Task ReleaseAfterDelayAsync()
    {
        try
        {
            await Task.Delay(_delay);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
