using frytech.AppleMusic.API.Models.Resources;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services;

public interface ISongFileProvider
{
    public Task<SongFile> GetSongFileAsync(Song song);
}