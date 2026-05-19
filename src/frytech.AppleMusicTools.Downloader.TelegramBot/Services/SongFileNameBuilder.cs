using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services;

public static class SongFileNameBuilder
{
    public static string BuildFileName(Song song)
    {
        var fileName = "{artist} - {title}.m4a"
            .Replace("{artist}", song.Attributes.ArtistName)
            .Replace("{title}", song.Attributes.Name);

        fileName = fileName.Replace("\"", "\'");
        fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));

        return fileName;
    }
}