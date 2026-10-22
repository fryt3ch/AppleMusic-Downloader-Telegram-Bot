using frytech.AppleMusic.API.Clients.Interfaces;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusic.API.Models.Resources;
using frytech.AppleMusicTools.Downloader.TelegramBot.Extensions;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services.Catalog;

public class AppleMusicCatalog
{
    private readonly ICatalogClient _catalogClient;

    public AppleMusicCatalog(ICatalogClient catalogClient)
    {
        _catalogClient = catalogClient;
    }

    public async Task<Song> GetSongAsync(string id, string store)
    {
        var response = await _catalogClient.GetCatalogSong(id, store);

        return response.Data?.FirstOrDefault()
            ?? throw new InvalidOperationException($"Apple Music song '{id}' was not found in store '{store}'.");
    }

    public async Task<Album> GetAlbumAsync(string id, string store)
    {
        var response = await _catalogClient.GetCatalogAlbum(id, store);

        return response.Data?.FirstOrDefault()
            ?? throw new InvalidOperationException($"Apple Music album '{id}' was not found in store '{store}'.");
    }

    public async Task<Playlist> GetPlaylistAsync(string id, string store)
    {
        var response = await _catalogClient.GetCatalogPlaylist(id, store);

        return response.Data?.FirstOrDefault()
            ?? throw new InvalidOperationException($"Apple Music playlist '{id}' was not found in store '{store}'.");
    }

    public async Task<Artist> GetArtistAsync(string id, string store)
    {
        var response = await _catalogClient.GetCatalogArtist(id, store);

        return response.Data?.FirstOrDefault()
            ?? throw new InvalidOperationException($"Apple Music artist '{id}' was not found in store '{store}'.");
    }

    public async Task<IEnumerable<Resource>> SearchAsync(string text, int offset, int limit, SearchMode searchMode, string store)
    {
        var searchTypes = searchMode.GetResourceTypes().ToArray();

        var response = await _catalogClient.CatalogResourcesSearchTop(store, text, searchTypes, CreatePageOptions(offset, limit));

        return response.Results?.Top?.Data ?? [];
    }

    public async Task<IEnumerable<Resource>> GetAlbumTracksAsync(int offset, int limit, string albumId, string store)
    {
        var response = await _catalogClient.GetCatalogAlbumTracks(albumId, store, CreatePageOptions(offset, limit));

        return response.Data ?? [];
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

    public async Task<IEnumerable<Resource>> GetPlaylistTracksAsync(int offset, int limit, string playlistId, string store)
    {
        var response = await _catalogClient.GetCatalogPlaylistTracks(playlistId, store, CreatePageOptions(offset, limit));

        return response.Data ?? [];
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
