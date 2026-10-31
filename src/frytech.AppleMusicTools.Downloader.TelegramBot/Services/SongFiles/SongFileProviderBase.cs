using frytech.AppleMusic.API.Models.Resources;
using frytech.AppleMusicTools.Downloader.TelegramBot.Exceptions;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Catalog;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.SongFiles;

public abstract class SongFileProviderBase : ISongFileProvider
{
    private readonly SongFileProviderLimiter _limiter;

    protected SongFileProviderBase(AppleMusicCatalog catalog, SongFileProviderLimiter limiter)
    {
        Catalog = catalog;
        _limiter = limiter;
    }

    public abstract string ProviderName { get; }
    
    protected AppleMusicCatalog Catalog { get; }

    public Task<SongFile> GetSongFileAsync(string songId, string store, CancellationToken cancellationToken)
    {
        return _limiter.ExecuteAsync(() => GetSongFileCoreAsync(songId, store, cancellationToken), cancellationToken);
    }

    private async Task<SongFile> GetSongFileCoreAsync(string songId, string store, CancellationToken cancellationToken)
    {
        Song? song = null;

        try
        {
            song = await Catalog.GetSongAsync(songId, store);

            return await GetSongFileAsync(song, store, cancellationToken);
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

    protected abstract Task<SongFile> GetSongFileAsync(Song song, string store, CancellationToken cancellationToken);
}
