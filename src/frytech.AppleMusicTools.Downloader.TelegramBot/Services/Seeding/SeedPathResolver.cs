namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Seeding;

public static class SeedPathResolver
{
    public static string ResolveReadPath(string path)
    {
        if (Path.IsPathRooted(path))
            return path;

        var basePath = Path.Combine(AppContext.BaseDirectory, path);

        if (File.Exists(basePath))
            return basePath;

        var currentPath = Path.Combine(Directory.GetCurrentDirectory(), path);

        return File.Exists(currentPath) ? currentPath : basePath;
    }

    public static string ResolveWritePath(string path)
    {
        if (Path.IsPathRooted(path))
            return path;

        return Path.Combine(Directory.GetCurrentDirectory(), path);
    }

    public static void EnsureParentDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }
}