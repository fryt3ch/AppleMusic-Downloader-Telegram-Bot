using frytech.AppleMusic.API.Models.Resources;
using frytech.AppleMusicTools.Downloader.TelegramBot.Exceptions;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Catalog;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.SongFiles;

public abstract class SongFileProviderBase : ISongFileProvider
{
    protected SongFileProviderBase(AppleMusicCatalog catalog)
    {
        Catalog = catalog;
    }

    public abstract string ProviderName { get; }
    
    protected AppleMusicCatalog Catalog { get; }

    public async Task<SongFile> GetSongFileAsync(string songId, string store, CancellationToken cancellationToken)
    {
        Song? song = null;

        try
        {
            song = await Catalog.GetSongAsync(songId, store);

            return await GetSongFileAsync(song, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SongDownloadFailedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SongDownloadFailedException(songId, song?.Attributes.Name ?? songId, song?.Attributes.ArtistName ?? string.Empty, ex);
        }
    }

    protected abstract Task<SongFile> GetSongFileAsync(Song song, CancellationToken cancellationToken);
}
