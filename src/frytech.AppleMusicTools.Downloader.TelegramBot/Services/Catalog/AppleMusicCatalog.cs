using System.Collections.Concurrent;
using frytech.AppleMusic.API.Clients.Interfaces;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;
using frytech.AppleMusicTools.Downloader.TelegramBot.Extensions;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Catalog;

public class AppleMusicCatalog
{
    private readonly ICatalogClient _catalogClient;
    private readonly ILogger<AppleMusicCatalog> _logger;
    private readonly ConcurrentDictionary<(string Id, string Store), Lazy<Task<Song>>> _songCache = new();

    public AppleMusicCatalog(ICatalogClient catalogClient, ILogger<AppleMusicCatalog> logger)
    {
        _catalogClient = catalogClient;
        _logger = logger;
    }

    public Task<Song> GetSongAsync(string id, string store)
    {
        return _songCache.GetOrAdd((id, store),
            key => new Lazy<Task<Song>>(() => GetSongCoreAsync(key.Id, key.Store), LazyThreadSafetyMode.ExecutionAndPublication))
            .Value;
    }

    private async Task<Song> GetSongCoreAsync(string id, string store)
    {
        _logger.LogDebug("Fetching Apple Music song {SongId} from store {Store}", id, store);

        var response = await _catalogClient.GetCatalogSong(id, store);

        var song = response.Data?.FirstOrDefault();

        if (song is null)
        {
            _logger.LogWarning("Apple Music song {SongId} was not found in store {Store}", id, store);

            throw new InvalidOperationException($"Apple Music song '{id}' was not found in store '{store}'.");
        }

        return song;
    }

    public async Task<Album> GetAlbumAsync(string id, string store)
    {
        _logger.LogDebug("Fetching Apple Music album {AlbumId} from store {Store}", id, store);

        var response = await _catalogClient.GetCatalogAlbum(id, store);

        var album = response.Data?.FirstOrDefault();

        if (album is null)
        {
            _logger.LogWarning("Apple Music album {AlbumId} was not found in store {Store}", id, store);

            throw new InvalidOperationException($"Apple Music album '{id}' was not found in store '{store}'.");
        }

        return album;
    }

    public async Task<Playlist> GetPlaylistAsync(string id, string store)
    {
        _logger.LogDebug("Fetching Apple Music playlist {PlaylistId} from store {Store}", id, store);

        var response = await _catalogClient.GetCatalogPlaylist(id, store);

        var playlist = response.Data?.FirstOrDefault();

        if (playlist is null)
        {
            _logger.LogWarning("Apple Music playlist {PlaylistId} was not found in store {Store}", id, store);

            throw new InvalidOperationException($"Apple Music playlist '{id}' was not found in store '{store}'.");
        }

        return playlist;
    }

    public async Task<Artist> GetArtistAsync(string id, string store)
    {
        _logger.LogDebug("Fetching Apple Music artist {ArtistId} from store {Store}", id, store);

        var response = await _catalogClient.GetCatalogArtist(id, store);

        var artist = response.Data?.FirstOrDefault();

        if (artist is null)
        {
            _logger.LogWarning("Apple Music artist {ArtistId} was not found in store {Store}", id, store);

            throw new InvalidOperationException($"Apple Music artist '{id}' was not found in store '{store}'.");
        }

        return artist;
    }

    public async Task<IEnumerable<Resource>> SearchAsync(string text, int offset, int limit, SearchMode searchMode, string store)
    {
        var searchTypes = searchMode.GetResourceTypes().ToArray();

        _logger.LogDebug("Searching Apple Music for {Term} in store {Store} with type {SearchMode}", text, store, searchMode);

        var response = await _catalogClient.CatalogResourcesSearchTop(store, text, searchTypes, CreatePageOptions(offset, limit));

        var results = response.Results?.Top?.Data ?? [];

        _logger.LogDebug("Apple Music search for {Term} in store {Store} returned {Count} results", text, store, results.Count());

        return results;
    }

    public async Task<TrackPage> GetAlbumTracksPageAsync(int offset, int limit, string albumId, string store)
    {
        var response = await _catalogClient.GetCatalogAlbumTracks(albumId, store, CreatePageOptions(offset, limit));

        return new TrackPage(response.Data ?? [], !string.IsNullOrEmpty(response.Next));
    }

    public async Task<IEnumerable<Resource>> GetAlbumTracksAsync(int offset, int limit, string albumId, string store)
    {
        return (await GetAlbumTracksPageAsync(offset, limit, albumId, store)).Items;
    }

    public async Task<IEnumerable<Resource>> GetAuthorTracksAsync(int offset, int limit, string authorId, string store)
    {
        var response = await _catalogClient.GetCatalogArtistSongs(authorId, store, CreatePageOptions(offset, limit));

        return response.Data ?? [];
    }

    public async Task<IEnumerable<Album>> GetAuthorAlbumsAsync(int offset, int limit, string authorId, string store)
    {
        var response = await _catalogClient.GetCatalogArtistAlbums(authorId, store, CreatePageOptions(offset, limit));

        return response.Data ?? [];
    }

    public async Task<IEnumerable<Playlist>> GetAuthorPlaylistsAsync(int offset, int limit, string authorId, string store)
    {
        var response = await _catalogClient.GetCatalogArtistPlaylists(authorId, store, CreatePageOptions(offset, limit));

        return response.Data ?? [];
    }

    public async Task<TrackPage> GetPlaylistTracksPageAsync(int offset, int limit, string playlistId, string store)
    {
        var response = await _catalogClient.GetCatalogPlaylistTracks(playlistId, store, CreatePageOptions(offset, limit));

        return new TrackPage(response.Data ?? [], !string.IsNullOrEmpty(response.Next));
    }

    public async Task<IEnumerable<Resource>> GetPlaylistTracksAsync(int offset, int limit, string playlistId, string store)
    {
        return (await GetPlaylistTracksPageAsync(offset, limit, playlistId, store)).Items;
    }

    private static PageOptions CreatePageOptions(int offset, int limit)
    {
        return new PageOptions()
        {
            Offset = offset,
            Limit = limit,
        };
    }
}
