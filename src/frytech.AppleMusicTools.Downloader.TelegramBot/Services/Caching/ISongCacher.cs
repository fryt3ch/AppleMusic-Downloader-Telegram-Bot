using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Database;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Caching;

public interface ISongCacher
{
    public Task<CachedSong> GetOrAddSong(string songId, string store, CancellationToken cancellationToken);
}