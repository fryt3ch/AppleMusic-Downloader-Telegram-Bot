namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Seeding;

public sealed class SeedLock : IDisposable
{
    private readonly FileStream _stream;

    private SeedLock(FileStream stream)
    {
        _stream = stream;
    }

    public static SeedLock? TryAcquire(string path, ILogger logger)
    {
        var resolvedPath = SeedPathResolver.ResolveWritePath(path);
        SeedPathResolver.EnsureParentDirectory(resolvedPath);

        try
        {
            var stream = new FileStream(resolvedPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            return new SeedLock(stream);
        }
        catch (IOException)
        {
            logger.LogWarning("Another seed process is already running (lock file: {Path})", resolvedPath);

            return null;
        }
    }

    public void Dispose()
    {
        _stream.Dispose();
    }
}
