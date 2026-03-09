using System.Text.RegularExpressions;
using frytech.AppleMusic.API.Clients.Interfaces;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusic.API.Models.Resources;
using frytech.AppleMusicTools.Downloader.TelegramBot.Models;
using frytech.AppleMusicTools.Downloader.TelegramBot.Extensions;

namespace frytech.AppleMusicTools.Downloader.TelegramBot.Services;

public class MusicService
{
    private static readonly IReadOnlyDictionary<ResourceType, string> ResourceTypeKeywordDictionary =
        new Dictionary<ResourceType, string>()
        {
            [ResourceType.Songs] = "song",
            [ResourceType.MusicVideos] = "music-video",
            [ResourceType.Albums] = "album",
            [ResourceType.Artists] = "artist",
            [ResourceType.Playlists] = "playlist",
        };

    private static Regex MediaUrlRegex = new Regex(@"^.*music\.apple\.com(?:\/(?<store>[a-z]+))?\/(?<mediaType>playlist|album|song|artist)(?:\/[^\/]+)*\/(?<mainId>[^\/?]+)(?:\?.*?i=(?<songId>\d+))?\/?$");
    
    private static readonly IReadOnlyDictionary<string, ResourceType> KeywordResourceTypeDictionary = ResourceTypeKeywordDictionary.ToDictionary(x => x.Value, x => x.Key);
    
    private readonly ICatalogClient _catalogClient;

    public MusicService(ICatalogClient catalogClient)
    {
        _catalogClient = catalogClient;
    }

    public bool TryGetMusicElementInfo(Uri uri, out string? store, out ResourceType resourceType, out string musicElementId)
    {
        resourceType = ResourceType.Songs;
        musicElementId = null!;
        store = null;
        
        var match = MediaUrlRegex.Match(uri.ToString());

        if (!match.Success)
            return false;

        if (match.Groups["store"].Success)
            store = match.Groups["store"].Value;
        
        var mediaTypeStr = match.Groups["mediaType"].Value.ToLowerInvariant();
        var mainId = match.Groups["mainId"].Value;
        var songId = match.Groups["songId"].Success ? match.Groups["songId"].Value : null;

        if (!string.IsNullOrWhiteSpace(songId))
        {
            resourceType = ResourceType.Songs;
            musicElementId = songId;
        }
        else if (KeywordResourceTypeDictionary.TryGetValue(mediaTypeStr, out resourceType))
        {
            musicElementId = mainId;
        }
        else
        {
            return false;
        }

        return true;

    }

    public Uri CreateMusicElementUrl(ResourceType type, string id, string store = "us")
    {
        return new Uri($"https://music.apple.com/{store}/{ResourceTypeKeywordDictionary[type]}/{id}");
    }
    
    public async Task<Song> GetSongAsync(string id, string store)
    {
        var response = await _catalogClient.GetCatalogSong(id, store);

        return response.Data!.First();
    }

    public async Task<Album> GetAlbumAsync(string id, string store)
    {
        var response = await _catalogClient.GetCatalogAlbum(id, store);

        return response.Data!.First();
    }
    
    public async Task<Playlist> GetPlaylistAsync(string id, string store)
    {
        var response = await _catalogClient.GetCatalogPlaylist(id, store);

        return response.Data!.First();
    }
    
    public async Task<Artist> GetArtistAsync(string id, string store)
    {
        var response = await _catalogClient.GetCatalogArtist(id, store);

        return response.Data!.First();
    }

    public async Task<IEnumerable<Resource>> SearchAsync(string text, int offset, int limit, SearchMode searchMode, string store)
    {
        var searchTypes = searchMode.GetResourceTypes().ToArray();

        var response = await _catalogClient.CatalogResourcesSearchTop(store, text, searchTypes, new PageOptions()
        {
            Limit = limit,
            Offset = offset,
        });

        return response.Results?.Top?.Data ?? [];
    }

    public async Task<IEnumerable<Resource>> GetAlbumTracksAsync(int offset, int limit, string albumId, string store)
    {
        var response = await _catalogClient.GetCatalogAlbumTracks(albumId, store, new PageOptions()
        {
            Offset = offset,
            Limit = limit,
        });

        return response.Data ?? [];
    }
    
    public async Task<IEnumerable<Resource>> GetAuthorTracksAsync(int offset, int limit, string authorId, string store)
    {
        var response = await _catalogClient.GetCatalogArtistSongs(authorId, store, new PageOptions()
        {
            Offset = offset,
            Limit = limit,
        });

        return response.Data ?? [];
    }
    
    public async Task<IEnumerable<Album>> GetAuthorAlbumsAsync(int offset, int limit, string authorId, string store)
    {
        var response = await _catalogClient.GetCatalogArtistAlbums(authorId, store, new PageOptions()
        {
            Offset = offset,
            Limit = limit,
        });
        
        return response.Data ?? [];
    }
    
    public async Task<IEnumerable<Playlist>> GetAuthorPlaylistsAsync(int offset, int limit, string authorId, string store)
    {
        var response = await _catalogClient.GetCatalogArtistPlaylists(authorId, store, new PageOptions()
        {
            Offset = offset,
            Limit = limit,
        });
        
        return response.Data ?? [];
    }

    public async Task<IEnumerable<Resource>> GetPlaylistTracksAsync(int offset, int limit, string playlistId, string store)
    {
        var response = await _catalogClient.GetCatalogPlaylistTracks(playlistId, store, new PageOptions()
        {
            Offset = offset,
            Limit = limit,
        });
        
        return response.Data ?? [];
    }
}