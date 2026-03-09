namespace frytech.AppleMusicTools.Downloader.TelegramBot.Models;

public record SongFile(string FileName, Stream Stream) : NamedStreamEntry(FileName, Stream);