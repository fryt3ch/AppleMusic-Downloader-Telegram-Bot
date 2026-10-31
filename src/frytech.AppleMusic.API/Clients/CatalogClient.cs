using System.Collections.Specialized;
using frytech.AppleMusic.API.Clients.Interfaces;
using frytech.AppleMusic.API.Configuration;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusic.API.Models.Enums.Relationships;
using frytech.AppleMusic.API.Models.Relationships;
using frytech.AppleMusic.API.Models.Responses;
using frytech.AppleMusic.API.Extensions;
using Microsoft.Extensions.Options;
using Models_Relationships_SongRelationship = frytech.AppleMusic.API.Models.Relationships.SongRelationship;

namespace frytech.AppleMusic.API.Clients;

public class CatalogClient : BaseClient, ICatalogClient
{
    private const string BaseRequestUri = "catalog";

    public CatalogClient(HttpClient client, IOptions<AppleMusicClientConfiguration> options) : base(client, options)
    {
    }

    #region Route: catalog/{storefront}/activities/{id}
    
    public async Task<ActivityResponse> GetCatalogActivity(string id, string storefront, IReadOnlyCollection<ActivityRelationshipType>? relationshipsToInclude = null, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetCatalogResource<ActivityResponse, ActivityRelationshipType>(ResourceType.Activities, id, storefront, relationshipsToInclude, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/activities/{id}/{relationship}
    
    public async Task<ActivityResponse> GetCatalogActivityRelationship(string id, string storefront, ActivityRelationshipType relationshipType, PageOptions? pageOptions)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetCatalogResourceRelationship<ActivityResponse, ActivityRelationshipType>(ResourceType.Activities, id, storefront, relationshipType, pageOptions)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/activities

    public async Task<ActivityResponse> GetMultipleCatalogActivities(IReadOnlyCollection<string> ids, string storefront, IReadOnlyCollection<ActivityRelationshipType>? include = null, string? locale = null)
    {
        if (ids == null || !ids.Any())
            throw new ArgumentNullException(nameof(ids));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetMultipleCatalogResources<ActivityResponse, ActivityRelationshipType>(ResourceType.Activities, ids, storefront, include, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/albums/{id}

    public async Task<AlbumResponse> GetCatalogAlbum(string id, string storefront, IReadOnlyCollection<AlbumRelationshipType>? relationshipsToInclude = null, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetCatalogResource<AlbumResponse, AlbumRelationshipType>(ResourceType.Albums, id, storefront, relationshipsToInclude, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/albums/{id}/{relationship}

    public async Task<TrackRelationship> GetCatalogAlbumTracks(string id, string storefront, PageOptions? pageOptions, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetCatalogResourceRelationship<TrackRelationship, AlbumRelationshipType>(ResourceType.Albums, id, storefront, AlbumRelationshipType.Tracks, pageOptions, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/albums

    public async Task<AlbumResponse> GetMultipleCatalogAlbums(IReadOnlyCollection<string> ids, string storefront, IReadOnlyCollection<AlbumRelationshipType>? include = null, string? locale = null)
    {
        if (ids == null || !ids.Any())
            throw new ArgumentNullException(nameof(ids));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetMultipleCatalogResources<AlbumResponse, AlbumRelationshipType>(ResourceType.Albums, ids, storefront, include, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/apple-curators/{id}

    public async Task<AppleCuratorResponse> GetCatalogAppleCurator(string id, string storefront, IReadOnlyCollection<AppleCuratorRelationshipType>? relationshipsToInclude = null, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetCatalogResource<AppleCuratorResponse, AppleCuratorRelationshipType>(ResourceType.AppleCurators, id, storefront, relationshipsToInclude, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/apple-curators/{id}/{relationship}

    public async Task<AppleCuratorResponse> GetCatalogAppleCuratorRelationship(string id, string storefront, AppleCuratorRelationshipType relationshipType, PageOptions? pageOptions, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetCatalogResourceRelationship<AppleCuratorResponse, AppleCuratorRelationshipType>(ResourceType.AppleCurators, id, storefront, relationshipType, pageOptions, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/apple-curators

    public async Task<AppleCuratorResponse> GetMultipleCatalogAppleCurators(IReadOnlyCollection<string> ids, string storefront, IReadOnlyCollection<AppleCuratorRelationshipType>? include = null, string? locale = null)
    {
        if (ids == null || !ids.Any())
            throw new ArgumentNullException(nameof(ids));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetMultipleCatalogResources<AppleCuratorResponse, AppleCuratorRelationshipType>(ResourceType.AppleCurators, ids, storefront, include, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/artists/{id}

    public async Task<ArtistResponse> GetCatalogArtist(string id, string storefront, IReadOnlyCollection<ArtistRelationshipType>? relationshipsToInclude = null, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetCatalogResource<ArtistResponse, ArtistRelationshipType>(ResourceType.Artists, id, storefront, relationshipsToInclude, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/artists/{id}/{relationship}
    
    public async Task<Models_Relationships_SongRelationship> GetCatalogArtistSongs(string id, string storefront, PageOptions? pageOptions, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetCatalogResourceRelationship<Models_Relationships_SongRelationship, ArtistRelationshipType>(ResourceType.Artists, id, storefront, ArtistRelationshipType.Songs, pageOptions, locale)
            .ConfigureAwait(false);
    }
    
    public async Task<AlbumRelationship> GetCatalogArtistAlbums(string id, string storefront, PageOptions? pageOptions, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetCatalogResourceRelationship<AlbumRelationship, ArtistRelationshipType>(ResourceType.Artists, id, storefront, ArtistRelationshipType.Albums, pageOptions, locale)
            .ConfigureAwait(false);
    }
    
    public async Task<PlaylistRelationship> GetCatalogArtistPlaylists(string id, string storefront, PageOptions? pageOptions, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetCatalogResourceRelationship<PlaylistRelationship, ArtistRelationshipType>(ResourceType.Artists, id, storefront, ArtistRelationshipType.Playlists, pageOptions, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/artists

    public async Task<ArtistResponse> GetMultipleCatalogArtists(IReadOnlyCollection<string> ids, string storefront, IReadOnlyCollection<ArtistRelationshipType>? include = null, string? locale = null)
    {
        if (ids == null || !ids.Any())
            throw new ArgumentNullException(nameof(ids));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetMultipleCatalogResources<ArtistResponse, ArtistRelationshipType>(ResourceType.Artists, ids, storefront, include, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/charts

    public async Task<ChartResponse> GetCatalogCharts(string storefront, IReadOnlyCollection<CatalogChartType>? types = null, string? chart = null, string? genre = null, PageOptions? pageOptions = null, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        var queryString = new NameValueCollection();
        if (types != null && types.Any())
            queryString.Add("types", string.Join(",", types.Select(x => x.GetValue())));

        if (!string.IsNullOrWhiteSpace(chart))
            queryString.Add("chart", chart);

        if (!string.IsNullOrWhiteSpace(genre))
            queryString.Add("genre", genre);

        return await Get<ChartResponse>($"{BaseRequestUri}/{storefront}/charts", queryString, pageOptions, locale);
    }

    #endregion

    #region Route: catalog/{storefront}/curators/{id}

    public async Task<CuratorResponse> GetCatalogCurator(string id, string storefront, IReadOnlyCollection<CuratorRelationshipType>? relationshipsToInclude = null, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetCatalogResource<CuratorResponse, CuratorRelationshipType>(ResourceType.Curators, id, storefront, relationshipsToInclude, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/curators/{id}/{relationship}

    public async Task<CuratorResponse> GetCatalogCuratorRelationship(string id, string storefront, CuratorRelationshipType relationshipType, PageOptions? pageOptions = null, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetCatalogResourceRelationship<CuratorResponse, CuratorRelationshipType>(ResourceType.Curators, id, storefront, relationshipType, pageOptions, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/curators

    public async Task<CuratorResponse> GetMultipleCatalogCurators(IReadOnlyCollection<string> ids, string storefront, IReadOnlyCollection<CuratorRelationshipType>? include = null, string? locale = null)
    {
        if (ids == null || !ids.Any())
            throw new ArgumentNullException(nameof(ids));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetMultipleCatalogResources<CuratorResponse, CuratorRelationshipType>(ResourceType.Curators, ids, storefront, include, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/genres/{id}

    public async Task<GenreResponse> GetCatalogGenre(string id, string storefront, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetCatalogResource<GenreResponse>(ResourceType.Genres, id, storefront, locale: locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/genres

    public async Task<GenreResponse> GetMultipleCatalogGenres(IReadOnlyCollection<string> ids, string storefront, string? locale = null)
    {
        if (ids == null || !ids.Any())
            throw new ArgumentNullException(nameof(ids));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        var queryString = new NameValueCollection()
        {
            {"ids", string.Join(",", ids)}
        };

        return await GetMultipleCatalogResources<GenreResponse>(ResourceType.Genres, storefront, queryString, locale)
            .ConfigureAwait(false);
    }

    public async Task<GenreResponse> GetCatalogTopChartsGenres(string storefront, PageOptions? pageOptions = null, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await Get<GenreResponse>($"{BaseRequestUri}/{storefront}/{ResourceType.Genres.GetValue()}", pageOptions: pageOptions, locale: locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/music-videos/{id}

    public async Task<MusicVideoResponse> GetCatalogMusicVideo(string id, string storefront, IReadOnlyCollection<MusicVideoRelationshipType>? relationshipsToInclude = null, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetCatalogResource<MusicVideoResponse, MusicVideoRelationshipType>(ResourceType.MusicVideos, id, storefront, relationshipsToInclude, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/music-videos/{id}/{relationship}

    public async Task<MusicVideoResponse> GetCatalogMusicVideoRelationship(string id, string storefront, MusicVideoRelationshipType relationshipType, PageOptions? pageOptions = null, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetCatalogResourceRelationship<MusicVideoResponse, MusicVideoRelationshipType>(ResourceType.MusicVideos, id, storefront, relationshipType, pageOptions, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/music-videos

    public async Task<MusicVideoResponse> GetMultipleCatalogMusicVideos(IReadOnlyCollection<string> ids, string storefront, IReadOnlyCollection<MusicVideoRelationshipType>? include = null, string? isrc = null, string? locale = null)
    {
        if (ids == null || !ids.Any())
            throw new ArgumentNullException(nameof(ids));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        var queryString = new NameValueCollection();
        
        if (!string.IsNullOrWhiteSpace(isrc))
            queryString.Add("filter[isrc]", isrc);

        return await GetMultipleCatalogResources<MusicVideoResponse, MusicVideoRelationshipType>(ResourceType.MusicVideos, ids, storefront, include, locale, queryString)
            .ConfigureAwait(false);
    }

    public async Task<MusicVideoResponse> GetMultipleCatalogMusicVideosByIsrc(string isrc, string storefront, IReadOnlyCollection<string>? ids = null, IReadOnlyCollection<MusicVideoRelationshipType>? include = null, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(isrc))
            throw new ArgumentNullException(nameof(isrc));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        var queryString = new NameValueCollection()
        {
            {"filter[isrc]", isrc},
        };

        return await GetMultipleCatalogResources<MusicVideoResponse, MusicVideoRelationshipType>(ResourceType.MusicVideos, ids, storefront, include, locale, queryString)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/playlists/{id}

    public async Task<PlaylistResponse> GetCatalogPlaylist(string id, string storefront, IReadOnlyCollection<PlaylistRelationshipType>? relationshipsToInclude = null, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetCatalogResource<PlaylistResponse, PlaylistRelationshipType>(ResourceType.Playlists, id, storefront, relationshipsToInclude, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/playlists/{id}/{relationship}

    public async Task<TrackRelationship> GetCatalogPlaylistTracks(string id, string storefront, PageOptions? pageOptions = null, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetCatalogResourceRelationship<TrackRelationship, PlaylistRelationshipType>(ResourceType.Playlists, id, storefront, PlaylistRelationshipType.Tracks, pageOptions, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/playlists

    public async Task<PlaylistResponse> GetMultipleCatalogPlaylists(IReadOnlyCollection<string> ids, string storefront, IReadOnlyCollection<PlaylistRelationshipType>? include = null, string? locale = null)
    {
        if (ids == null || !ids.Any())
            throw new ArgumentNullException(nameof(ids));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetMultipleCatalogResources<PlaylistResponse, PlaylistRelationshipType>(ResourceType.Playlists, ids, storefront, include, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/search

    public async Task<SearchResponse> CatalogResourcesSearch(string storefront, string term, IReadOnlyCollection<ResourceType>? types = null, PageOptions? pageOptions = null, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));
        
        if (string.IsNullOrWhiteSpace(term))
            throw new ArgumentNullException(nameof(storefront));

        var queryString = new NameValueCollection
        {
            { "term", term }
        };

        if (types != null && types.Any())
            queryString.Add("types", string.Join(",", types.Select(x => x.GetValue())));

        return await Get<SearchResponse>($"{BaseRequestUri}/{storefront}/search", queryString, pageOptions, locale)
            .ConfigureAwait(false);
    }

    public async Task<TopSearchResponse> CatalogResourcesSearchTop(string storefront, string term, IReadOnlyCollection<ResourceType>? types = null,
        PageOptions? pageOptions = null, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        var queryString = new NameValueCollection
        {
            { "term", term },
            { "groups", "top" },
            { "with", "serverBubbles" },
        };

        if (types != null && types.Any())
            queryString.Add("types", string.Join(",", types.Select(x => x.GetValue())));

        return await Get<TopSearchResponse>($"{BaseRequestUri}/{storefront}/search", queryString, pageOptions, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/search/hints

    public async Task<SearchHintsResponse> GetCatalogSearchHints(string storefront, string? term = null, IReadOnlyCollection<ResourceType>? types = null, PageOptions? pageOptions = null, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        var queryString = new NameValueCollection();

        if (!string.IsNullOrWhiteSpace(term))
            queryString.Add("term", term);

        if (types != null && types.Any())
            queryString.Add("types", string.Join(",", types.Select(x => x.GetValue())));

        return await Get<SearchHintsResponse>($"{BaseRequestUri}/{storefront}/search/hints", queryString, pageOptions, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/songs

    public async Task<SongResponse> GetCatalogSong(string id, string storefront, IReadOnlyCollection<SongRelationshipType>? relationshipsToInclude = null, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetCatalogResource<SongResponse, SongRelationshipType>(ResourceType.Songs, id, storefront, relationshipsToInclude, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/songs/{id}/{relationship}

    public async Task<SongResponse> GetCatalogSongRelationship(string id, string storefront, SongRelationshipType relationship, PageOptions? pageOptions = null, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetCatalogResourceRelationship<SongResponse, SongRelationshipType>(ResourceType.Songs, id, storefront, relationship, pageOptions, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/songs

    public async Task<SongResponse> GetMultipleCatalogSongs(IReadOnlyCollection<string> ids, string storefront, IReadOnlyCollection<SongRelationshipType>? include = null, string? isrc = null, string? locale = null)
    {
        if (ids == null || !ids.Any())
            throw new ArgumentNullException(nameof(ids));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        var queryString = new NameValueCollection();
        
        if (!string.IsNullOrWhiteSpace(isrc))
            queryString.Add("filter[isrc]", isrc);

        return await GetMultipleCatalogResources<SongResponse, SongRelationshipType>(ResourceType.Songs, ids, storefront, include, locale, queryString)
            .ConfigureAwait(false);
    }

    public async Task<SongResponse> GetMultipleCatalogSongsByIsrc(string isrc, string storefront, IReadOnlyCollection<string>? ids = null, IReadOnlyCollection<SongRelationshipType>? include = null, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(isrc))
            throw new ArgumentNullException(nameof(isrc));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        var queryString = new NameValueCollection()
        {
            {"filter[isrc]", isrc},
        };

        return await GetMultipleCatalogResources<SongResponse, SongRelationshipType>(ResourceType.Songs, ids, storefront, include, locale, queryString)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/stations/{id}

    public async Task<StationResponse> GetCatalogStation(string id, string storefront, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        return await GetCatalogResource<StationResponse>(ResourceType.Stations, id, storefront, locale: locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Route: catalog/{storefront}/stations

    public async Task<StationResponse> GetMultipleCatalogStations(IReadOnlyCollection<string> ids, string storefront, string? locale = null)
    {
        if (ids == null || !ids.Any())
            throw new ArgumentNullException(nameof(ids));

        if (string.IsNullOrWhiteSpace(storefront))
            throw new ArgumentNullException(nameof(storefront));

        var queryString = new NameValueCollection()
        {
            {"ids", string.Join(",", ids)}
        };

        return await GetMultipleCatalogResources<StationResponse>(ResourceType.Stations, storefront, queryString, locale)
            .ConfigureAwait(false);
    }

    #endregion

    #region Private Methods

    private async Task<TResponse> GetCatalogResource<TResponse>(ResourceType resourceType, string id, string storefront, NameValueCollection? queryString = null, string? locale = null)
    {
        return await Get<TResponse>($"{BaseRequestUri}/{storefront}/{resourceType.GetValue()}/{id}", queryString, locale: locale)
            .ConfigureAwait(false);
    }

    private async Task<TResponse> GetCatalogResource<TResponse, TRelationshipEnum>(ResourceType resourceType, string id, string storefront, IReadOnlyCollection<TRelationshipEnum>? relationshipsToInclude = null, string? locale = null)
        where TRelationshipEnum : IConvertible
    {
        var queryString = new NameValueCollection();
        
        if (relationshipsToInclude != null && relationshipsToInclude.Any())
            queryString.Add("include", string.Join(",", relationshipsToInclude.Select(x => x.GetValue())));

        return await GetCatalogResource<TResponse>(resourceType, id, storefront, queryString, locale)
            .ConfigureAwait(false);
    }

    private async Task<TResponse> GetCatalogResourceRelationship<TResponse, TRelationshipEnum>(ResourceType resourceType, string id, string storefront, TRelationshipEnum relationship, PageOptions? pageOptions = null, string? locale = null)
        where TRelationshipEnum : IConvertible
    {
        return await Get<TResponse>($"{BaseRequestUri}/{storefront}/{resourceType.GetValue()}/{id}/{relationship.GetValue()}", pageOptions: pageOptions, locale: locale)
            .ConfigureAwait(false);
    }

    private async Task<TResponse> GetMultipleCatalogResources<TResponse>(ResourceType resourceType, string storefront, NameValueCollection? queryString = null, string? locale = null)
    {
        return await Get<TResponse>($"{BaseRequestUri}/{storefront}/{resourceType.GetValue()}", queryString, locale: locale)
            .ConfigureAwait(false);
    }

    private async Task<TResponse> GetMultipleCatalogResources<TResponse, TRelationshipEnum>(ResourceType resourceType, IReadOnlyCollection<string>? ids, string storefront, IReadOnlyCollection<TRelationshipEnum>? include = null, string? locale = null, NameValueCollection? additionalQuerystringParams = null)
        where TRelationshipEnum : IConvertible
    {
        var queryString = new NameValueCollection();
        
        if (ids != null && ids.Any())
            queryString.Add("ids", string.Join(",", ids));

        if (include != null && include.Any())
            queryString.Add("include", string.Join(",", include.Select(x => x.GetValue())));

        if (additionalQuerystringParams != null && additionalQuerystringParams.HasKeys())
            queryString.Add(additionalQuerystringParams);

        return await GetMultipleCatalogResources<TResponse>(resourceType, storefront, queryString, locale)
            .ConfigureAwait(false);
    }

    #endregion
}