using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.SongFiles;

public static class SongFileNameBuilder
{
    public static string BuildFileName(Song song, string extension = ".m4a")
    {
        var fileName = "{artist} - {title}{extension}"
            .Replace("{artist}", song.Attributes.ArtistName)
            .Replace("{title}", song.Attributes.Name)
            .Replace("{extension}", extension);

        fileName = fileName.Replace("\"", "\'");
        fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));

        return fileName;
    }
}