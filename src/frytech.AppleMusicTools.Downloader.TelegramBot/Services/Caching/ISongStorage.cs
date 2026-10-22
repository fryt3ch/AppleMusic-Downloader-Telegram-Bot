using frytech.AppleMusicTools.Downloader.TelegramBot.Models;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models.Database;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Caching;

public interface ISongStorage
{
    public Task<CachedSong> GetOrAddSong(
        string songId,
        string store,
        Func<CancellationToken, Task<SongFile>> getSongFile,
        CancellationToken cancellationToken);
}
