namespace frytech.AppleMusicTools.Downloader.TelegramBot.Models;

public sealed class SongDownloadFailedException : Exception
{
    public string SongId { get; }
    public string Name { get; }
    public string ArtistName { get; }

    public SongDownloadFailedException(string songId, string name, string artistName, Exception? innerException = null)
        : base($"Failed to obtain the file for '{artistName} - {name}' (id: {songId}).", innerException)
    {
        SongId = songId;
        Name = name;
        ArtistName = artistName;
    }
}
