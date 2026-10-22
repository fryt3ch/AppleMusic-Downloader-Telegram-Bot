using frytech.AppleMusic.API.Models.Resources;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.SongFiles;

public interface ISongFileProvider
{
    public string ProviderName { get; }

    public Task<SongFile> GetSongFileAsync(Song song);
}