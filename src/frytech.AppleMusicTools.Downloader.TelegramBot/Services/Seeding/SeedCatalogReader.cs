using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusic.API.Models.Resources;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;
using frytech.AppleMusicTools.Downloader.TelegramBot.Services.Catalog;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Seeding;

public sealed class SeedCatalogReader
{
    private const int AlbumPageSize = 100;
    private const int TrackPageSize = 200;
    private const int ArtistSongsPageSize = 20;
    private const int MaxAlbumPages = 100;
    private const int MaxTrackPages = 50;
    private const int MaxArtistSongPages = 50;

    private readonly AppleMusicCatalog _catalog;
    private readonly ILogger<SeedCatalogReader> _logger;

    public SeedCatalogReader(AppleMusicCatalog catalog, ILogger<SeedCatalogReader> logger)
    {
        _catalog = catalog;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Artist>> SearchArtistsAsync(string name, string store, CancellationToken cancellationToken)
    {
        var resources = await ExecuteWithRetryAsync(
            $"search artist '{name}'",
            () => _catalog.SearchAsync(name, 0, 5, SearchMode.Authors, store),
            result => result.Any(),
            cancellationToken);

        return resources.OfType<Artist>().ToArray();
    }

    public async Task<IReadOnlyList<Album>> GetArtistAlbumsAsync(string artistId, string store, CancellationToken cancellationToken)
    {
        var albums = new List<Album>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var offset = 0;

        for (var pageIndex = 0; pageIndex < MaxAlbumPages; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = (await ExecuteWithRetryAsync(
                $"albums for artist {artistId} at offset {offset}",
                () => _catalog.GetAuthorAlbumsAsync(offset, AlbumPageSize, artistId, store),
                result => offset > 0 || result.Any(),
                cancellationToken)).ToArray();

            if (page.Length == 0)
                break;

            foreach (var album in page)
            {
                if (seen.Add(album.Id))
                    albums.Add(album);
            }

            offset += page.Length;

            if (page.Length < AlbumPageSize)
                break;
        }

        _logger.LogDebug("Loaded {Count} albums for artist {ArtistId}", albums.Count, artistId);

        return albums;
    }

    public async Task<IReadOnlyList<Song>> GetAlbumSongsAsync(string albumId, string store, CancellationToken cancellationToken)
    {
        var songs = new List<Song>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var offset = 0;

        for (var pageIndex = 0; pageIndex < MaxTrackPages; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = await ExecuteWithRetryAsync(
                $"tracks for album {albumId} at offset {offset}",
                () => _catalog.GetAlbumTracksPageAsync(offset, TrackPageSize, albumId, store),
                result => offset > 0 || result.Items.Count > 0,
                cancellationToken);

            if (page.Items.Count == 0)
                break;

            foreach (var resource in page.Items)
            {
                if (resource.Type is ResourceType.Songs && resource is Song song && seen.Add(song.Id))
                    songs.Add(song);
            }

            offset += page.Items.Count;

            if (!page.HasMore)
                break;
        }

        _logger.LogDebug("Loaded {Count} songs for album {AlbumId}", songs.Count, albumId);

        return songs;
    }

    public async Task<IReadOnlyList<Song>> GetArtistTopSongsAsync(string artistId, string store, int limit, CancellationToken cancellationToken)
    {
        if (limit <= 0)
            return [];

        var songs = new List<Song>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var offset = 0;

        for (var pageIndex = 0; pageIndex < MaxArtistSongPages && songs.Count < limit; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = (await ExecuteWithRetryAsync(
                $"songs for artist {artistId} at offset {offset}",
                () => _catalog.GetAuthorTracksAsync(offset, ArtistSongsPageSize, artistId, store),
                result => offset > 0 || result.Any(),
                cancellationToken)).ToArray();

            if (page.Length == 0)
                break;

            foreach (var resource in page)
            {
                if (resource.Type is ResourceType.Songs && resource is Song song && seen.Add(song.Id))
                    songs.Add(song);

                if (songs.Count >= limit)
                    break;
            }

            offset += page.Length;

            if (page.Length < ArtistSongsPageSize)
                break;
        }

        _logger.LogDebug("Loaded {Count} fallback songs for artist {ArtistId}", songs.Count, artistId);

        return songs;
    }

    private async Task<T> ExecuteWithRetryAsync<T>(string operation, Func<Task<T>> action, Func<T, bool> hasResult, CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await action();

                if (hasResult(result) || attempt == maxAttempts)
                    return result;

                _logger.LogDebug("Apple Music returned no result for {Operation} (attempt {Attempt}/{MaxAttempts})", operation, attempt, maxAttempts);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                _logger.LogWarning(ex, "Apple Music request failed for {Operation} (attempt {Attempt}/{MaxAttempts})", operation, attempt, maxAttempts);
            }

            await DelayAsync(attempt, cancellationToken);
        }

        return await action();
    }

    private static Task DelayAsync(int attempt, CancellationToken cancellationToken)
    {
        return Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)), cancellationToken);
    }
}
