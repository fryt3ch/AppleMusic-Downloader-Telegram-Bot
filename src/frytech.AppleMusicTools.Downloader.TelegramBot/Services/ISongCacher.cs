using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Database;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services;

public interface ISongCacher
{
    public Task<CachedSong> GetCachedSong(string songId, string store, CancellationToken cancellationToken);
}