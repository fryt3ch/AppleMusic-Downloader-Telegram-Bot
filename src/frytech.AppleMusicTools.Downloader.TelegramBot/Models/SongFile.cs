using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Models;

public record SongFile(Song Song, string FileName, Stream Stream) : NamedStreamEntry(FileName, Stream);