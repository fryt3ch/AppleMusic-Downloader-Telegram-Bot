namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Caching;

public sealed class CachedSongDbLock
{
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
}
