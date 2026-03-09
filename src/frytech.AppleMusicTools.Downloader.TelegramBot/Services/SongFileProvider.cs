using frytech.AppleMusic.API.Models.Resources;
using frytech.AppleMusicTools.Downloader.Configuration;
using frytech.AppleMusicTools.Downloader.Core;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services;

public class SongFileProvider : ISongFileProvider
{
    private readonly AppleMusicContentDownloader _downloader;

    private static readonly ContentDownloadOptions DownloadOptions = new ContentDownloadOptions()
    {
        IncludeArtwork = true,
    };

    public SongFileProvider(AppleMusicContentDownloader downloader)
    {
        _downloader = downloader;
    }
    
    public async Task<SongFile> GetSongFileAsync(Song song)
    {
        var fileName = BuildFileName(song);
        var stream = await _downloader.DownloadContent(song.Id, DownloadOptions);

        var songFile = new SongFile(fileName, stream);

        return songFile;
    }
    
    private string BuildFileName(Song song)
    {
        var fileNameNoExtension = "{artist} - {title}.m4a"
            .Replace("{artist}", song.Attributes.ArtistName)
            .Replace("{title}", song.Attributes.Name);

        fileNameNoExtension = fileNameNoExtension.Replace("\"", "\'");
        fileNameNoExtension = string.Join("_", fileNameNoExtension.Split(Path.GetInvalidFileNameChars()));

        return fileNameNoExtension;
    }
}