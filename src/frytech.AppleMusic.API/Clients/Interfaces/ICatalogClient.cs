using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusic.API.Models.Enums.Relationships;
using frytech.AppleMusic.API.Models.Relationships;
using frytech.AppleMusic.API.Models.Responses;

namespace frytech.AppleMusic.API.Clients.Interfaces;

public interface ICatalogClient
{
    /// <summary>
    /// Fetch an activity by using its identifier.
    /// Route: catalog/{storefront}/activities/{id}
    /// https://developer.apple.com/documentation/applemusicapi/get_a_catalog_activity
    /// </summary>
    /// <param name="id"></param>
    /// <param name="storefront"></param>
    /// <param name="relationshipsToInclude"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<ActivityResponse> GetCatalogActivity(string id, string storefront, IReadOnlyCollection<ActivityRelationshipType>? relationshipsToInclude = null, string? locale = null);

    /// <summary>
    /// Fetch an activity's relationship by using its identifier.
    /// Route: catalog/{storefront}/activities/{id}/{relationship}
    /// https://developer.apple.com/documentation/applemusicapi/get_a_catalog_activity_s_relationship_directly_by_name
    /// </summary>
    /// <param name="id"></param>
    /// <param name="storefront"></param>
    /// <param name="relationshipType"></param>
    /// <param name="pageOptions"></param>
    /// <returns></returns>
    Task<ActivityResponse> GetCatalogActivityRelationship(string id, string storefront, ActivityRelationshipType relationshipType, PageOptions? pageOptions = null);

    /// <summary>
    /// Fetch one or more activities by using their identifiers.
    /// Route: catalog/{storefront}/activities
    /// https://developer.apple.com/documentation/applemusicapi/get_multiple_catalog_activities
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="storefront"></param>
    /// <param name="include"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<ActivityResponse> GetMultipleCatalogActivities(IReadOnlyCollection<string> ids, string storefront, IReadOnlyCollection<ActivityRelationshipType>? include = null, string? locale = null);

    /// <summary>
    /// Fetch an album by using its identifier.
    /// Route: catalog/{storefront}/albums/{id}
    /// https://developer.apple.com/documentation/applemusicapi/get_a_catalog_album
    /// </summary>
    /// <param name="id"></param>
    /// <param name="storefront"></param>
    /// <param name="relationshipsToInclude"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<AlbumResponse> GetCatalogAlbum(string id, string storefront, IReadOnlyCollection<AlbumRelationshipType>? relationshipsToInclude = null, string? locale = null);

    /// <summary>
    /// Fetch an album's relationship (tracks) by using its identifier.
    /// Route: catalog/{storefront}/albums/{id}/{relationship}
    /// https://developer.apple.com/documentation/applemusicapi/get_a_catalog_album_s_relationship_directly_by_name
    /// </summary>
    /// <param name="id"></param>
    /// <param name="storefront"></param>
    /// <param name="pageOptions"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<TrackRelationship> GetCatalogAlbumTracks(string id, string storefront, PageOptions? pageOptions, string? locale = null);

    /// <summary>
    /// Fetch one or more albums by using their identifiers.
    /// Route: catalog/{storefront}/albums
    /// https://developer.apple.com/documentation/applemusicapi/get_multiple_catalog_albums
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="storefront"></param>
    /// <param name="include"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<AlbumResponse> GetMultipleCatalogAlbums(IReadOnlyCollection<string> ids, string storefront, IReadOnlyCollection<AlbumRelationshipType>? include = null, string? locale = null);

    /// <summary>
    /// Fetch an Apple curator by using the curator's identifier.
    /// Route: catalog/{storefront}/apple-curators/{id}
    /// https://developer.apple.com/documentation/applemusicapi/get_a_catalog_apple_curator
    /// </summary>
    /// <param name="id"></param>
    /// <param name="storefront"></param>
    /// <param name="relationshipsToInclude"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<AppleCuratorResponse> GetCatalogAppleCurator(string id, string storefront, IReadOnlyCollection<AppleCuratorRelationshipType>? relationshipsToInclude = null, string? locale = null);

    /// <summary>
    /// Fetch an Apple curator's relationship by using the curator's identifier.
    /// Route: catalog/{storefront}/apple-curators/{id}/{relationship}
    /// https://developer.apple.com/documentation/applemusicapi/get_a_catalog_apple_curator_s_relationship_directly_by_name
    /// </summary>
    /// <param name="id"></param>
    /// <param name="storefront"></param>
    /// <param name="relationshipType"></param>
    /// <param name="pageOptions"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<AppleCuratorResponse> GetCatalogAppleCuratorRelationship(string id, string storefront, AppleCuratorRelationshipType relationshipType, PageOptions? pageOptions, string? locale = null);

    /// <summary>
    /// Fetch one or more Apple curators by using their identifiers.
    /// Route: catalog/{storefront}/apple-curators
    /// https://developer.apple.com/documentation/applemusicapi/get_multiple_catalog_apple_curators
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="storefront"></param>
    /// <param name="include"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<AppleCuratorResponse> GetMultipleCatalogAppleCurators(IReadOnlyCollection<string> ids, string storefront, IReadOnlyCollection<AppleCuratorRelationshipType>? include = null, string? locale = null);

    /// <summary>
    /// Fetch an artist by using the artist's identifier.
    /// Route: catalog/{storefront}/artists/{id}
    /// https://developer.apple.com/documentation/applemusicapi/get_a_catalog_artist
    /// </summary>
    /// <param name="id"></param>
    /// <param name="storefront"></param>
    /// <param name="relationshipsToInclude"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<ArtistResponse> GetCatalogArtist(string id, string storefront, IReadOnlyCollection<ArtistRelationshipType>? relationshipsToInclude = null, string? locale = null);

    /// <summary>
    /// Fetch an artist's relationship (songs) by using the artist's identifier.
    /// Route: catalog/{storefront}/artists/{id}/songs
    /// https://developer.apple.com/documentation/applemusicapi/get_a_catalog_artist_s_relationship_directly_by_name
    /// </summary>
    /// <param name="id"></param>
    /// <param name="storefront"></param>
    /// <param name="pageOptions"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<SongRelationship> GetCatalogArtistSongs(string id, string storefront, PageOptions? pageOptions, string? locale = null);
    
    /// <summary>
    /// Fetch an artist's relationship (albums) by using the artist's identifier.
    /// Route: catalog/{storefront}/artists/{id}/albums
    /// https://developer.apple.com/documentation/applemusicapi/get_a_catalog_artist_s_relationship_directly_by_name
    /// </summary>
    /// <param name="id"></param>
    /// <param name="storefront"></param>
    /// <param name="pageOptions"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<AlbumRelationship> GetCatalogArtistAlbums(string id, string storefront, PageOptions? pageOptions, string? locale = null);
    
    /// <summary>
    /// Fetch an artist's relationship (playlists) by using the artist's identifier.
    /// Route: catalog/{storefront}/artists/{id}/playlists
    /// https://developer.apple.com/documentation/applemusicapi/get_a_catalog_artist_s_relationship_directly_by_name
    /// </summary>
    /// <param name="id"></param>
    /// <param name="storefront"></param>
    /// <param name="pageOptions"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<PlaylistRelationship> GetCatalogArtistPlaylists(string id, string storefront, PageOptions? pageOptions, string? locale = null);

    /// <summary>
    /// Fetch one or more artists by using their identifiers.
    /// Route: catalog/{storefront}/artists
    /// https://developer.apple.com/documentation/applemusicapi/get_multiple_catalog_artists
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="storefront"></param>
    /// <param name="include"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<ArtistResponse> GetMultipleCatalogArtists(IReadOnlyCollection<string> ids, string storefront, IReadOnlyCollection<ArtistRelationshipType>? include = null, string? locale = null);

    /// <summary>
    /// Fetch one or more charts from the Apple Music Catalog.
    /// Route: catalog/{storefront}/charts
    /// https://developer.apple.com/documentation/applemusicapi/get_catalog_charts
    /// </summary>
    /// <param name="storefront"></param>
    /// <param name="types"></param>
    /// <param name="chart"></param>
    /// <param name="genre"></param>
    /// <param name="pageOptions"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<ChartResponse> GetCatalogCharts(string storefront, IReadOnlyCollection<CatalogChartType>? types = null, string? chart = null, string? genre = null, PageOptions? pageOptions = null, string? locale = null);

    /// <summary>
    /// Fetch a curator by using the curator's identifier.
    /// Route: catalog/{storefront}/curators/{id}
    /// https://developer.apple.com/documentation/applemusicapi/get_a_catalog_curator
    /// </summary>
    /// <param name="id"></param>
    /// <param name="storefront"></param>
    /// <param name="relationshipsToInclude"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<CuratorResponse> GetCatalogCurator(string id, string storefront, IReadOnlyCollection<CuratorRelationshipType>? relationshipsToInclude = null, string? locale = null);

    /// <summary>
    /// Fetch a curator's relationship by using the curator's identifier.
    /// Route: catalog/{storefront}/curators/{id}/{relationship}
    /// https://developer.apple.com/documentation/applemusicapi/get_a_catalog_curator_s_relationship_directly_by_name
    /// </summary>
    /// <param name="id"></param>
    /// <param name="storefront"></param>
    /// <param name="relationshipType"></param>
    /// <param name="pageOptions"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<CuratorResponse> GetCatalogCuratorRelationship(string id, string storefront, CuratorRelationshipType relationshipType, PageOptions? pageOptions, string? locale = null);

    /// <summary>
    /// Fetch one or more curators by using their identifiers.
    /// Route: catalog/{storefront}/curators
    /// https://developer.apple.com/documentation/applemusicapi/get_multiple_catalog_curators
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="storefront"></param>
    /// <param name="include"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<CuratorResponse> GetMultipleCatalogCurators(IReadOnlyCollection<string> ids, string storefront, IReadOnlyCollection<CuratorRelationshipType>? include = null, string? locale = null);

    /// <summary>
    /// Fetch a genre by using its identifier.
    /// Route: catalog/{storefront}/genres/{id}
    /// https://developer.apple.com/documentation/applemusicapi/get_a_catalog_genre
    /// </summary>
    /// <param name="id"></param>
    /// <param name="storefront"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<GenreResponse> GetCatalogGenre(string id, string storefront, string? locale = null);

    /// <summary>
    /// Fetch one or more genres.
    /// Route: catalog/{storefront}/genres
    /// https://developer.apple.com/documentation/applemusicapi/get_multiple_catalog_genres
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="storefront"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<GenreResponse> GetMultipleCatalogGenres(IReadOnlyCollection<string> ids, string storefront, string? locale = null);

    /// <summary>
    /// Fetch all genres for the current top charts.
    /// Route: catalog/{storefront}/genres
    /// https://developer.apple.com/documentation/applemusicapi/get_catalog_top_charts_genres
    /// </summary>
    /// <param name="storefront"></param>
    /// <param name="pageOptions"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<GenreResponse> GetCatalogTopChartsGenres(string storefront, PageOptions? pageOptions = null, string? locale = null);

    /// <summary>
    /// Fetch a music video by using its identifier.
    /// Route: catalog/{storefront}/music-videos/{id}
    /// https://developer.apple.com/documentation/applemusicapi/get_a_catalog_music_video
    /// </summary>
    /// <param name="id"></param>
    /// <param name="storefront"></param>
    /// <param name="relationshipsToInclude"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<MusicVideoResponse> GetCatalogMusicVideo(string id, string storefront, IReadOnlyCollection<MusicVideoRelationshipType>? relationshipsToInclude = null, string? locale = null);

    /// <summary>
    /// Fetch a music video's relationship by using its identifier.
    /// Route: catalog/{storefront}/music-videos/{id}/{relationship}
    /// https://developer.apple.com/documentation/applemusicapi/get_a_catalog_music_video_s_relationship_directly_by_name
    /// </summary>
    /// <param name="id"></param>
    /// <param name="storefront"></param>
    /// <param name="relationshipType"></param>
    /// <param name="pageOptions"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<MusicVideoResponse> GetCatalogMusicVideoRelationship(string id, string storefront, MusicVideoRelationshipType relationshipType, PageOptions? pageOptions, string? locale = null);

    /// <summary>
    /// Fetch one or more music videos by using their identifiers.
    /// Route: catalog/{storefront}/music-videos
    /// https://developer.apple.com/documentation/applemusicapi/get_multiple_catalog_music_videos_by_id
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="storefront"></param>
    /// <param name="include"></param>
    /// <param name="isrc"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<MusicVideoResponse> GetMultipleCatalogMusicVideos(IReadOnlyCollection<string> ids, string storefront, IReadOnlyCollection<MusicVideoRelationshipType>? include = null, string? isrc = null, string? locale = null);

    /// <summary>
    /// Fetch one or more music videos by using their ISRC values.
    /// Route: catalog/{storefront}/music-videos
    /// https://developer.apple.com/documentation/applemusicapi/get_multiple_catalog_music_videos_by_isrc
    /// </summary>
    /// <param name="isrc"></param>
    /// <param name="storefront"></param>
    /// <param name="ids"></param>
    /// <param name="include"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<MusicVideoResponse> GetMultipleCatalogMusicVideosByIsrc(string isrc, string storefront, IReadOnlyCollection<string>? ids = null, IReadOnlyCollection<MusicVideoRelationshipType>? include = null, string? locale = null);

    /// <summary>
    /// Fetch a playlist by using its identifier.
    /// Route: catalog/{storefront}/playlists/{id}
    /// https://developer.apple.com/documentation/applemusicapi/get_a_catalog_playlist
    /// </summary>
    /// <param name="id"></param>
    /// <param name="storefront"></param>
    /// <param name="relationshipsToInclude"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<PlaylistResponse> GetCatalogPlaylist(string id, string storefront, IReadOnlyCollection<PlaylistRelationshipType>? relationshipsToInclude = null, string? locale = null);

    /// <summary>
    /// Fetch a playlist's relationship (tracks) by using its identifier.
    /// Route: catalog/{storefront}/playlists/{id}/tracks
    /// https://developer.apple.com/documentation/applemusicapi/get_a_catalog_playlist_s_relationship_directly_by_name
    /// </summary>
    /// <param name="id"></param>
    /// <param name="storefront"></param>
    /// <param name="pageOptions"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<TrackRelationship> GetCatalogPlaylistTracks(string id, string storefront, PageOptions? pageOptions, string? locale = null);

    /// <summary>
    /// Fetch one or more playlists by using their identifiers.
    /// Route: catalog/{storefront}/playlists
    /// https://developer.apple.com/documentation/applemusicapi/get_multiple_catalog_playlists
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="storefront"></param>
    /// <param name="include"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<PlaylistResponse> GetMultipleCatalogPlaylists(IReadOnlyCollection<string> ids, string storefront, IReadOnlyCollection<PlaylistRelationshipType>? include = null, string? locale = null);

    /// <summary>
    /// Search the catalog by using a query.
    /// Route: catalog/{storefront}/search
    /// https://developer.apple.com/documentation/applemusicapi/search_for_catalog_resources
    /// </summary>
    /// <param name="storefront"></param>
    /// <param name="term"></param>
    /// <param name="types"></param>
    /// <param name="pageOptions"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<SearchResponse> CatalogResourcesSearch(string storefront, string term, IReadOnlyCollection<ResourceType>? types = null, PageOptions? pageOptions = null, string? locale = null);
    
    /// <summary>
    /// Search the catalog by using a query.
    /// Route: catalog/{storefront}/search
    /// https://developer.apple.com/documentation/applemusicapi/search_for_catalog_resources
    /// </summary>
    /// <param name="storefront"></param>
    /// <param name="term"></param>
    /// <param name="types"></param>
    /// <param name="pageOptions"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<TopSearchResponse> CatalogResourcesSearchTop(string storefront, string term, IReadOnlyCollection<ResourceType>? types = null, PageOptions? pageOptions = null, string? locale = null);

    /// <summary>
    /// Fetch the search term results for a hint.
    /// Route: catalog/{storefront}/search/hints
    /// https://developer.apple.com/documentation/applemusicapi/get_catalog_search_hints
    /// </summary>
    /// <param name="storefront"></param>
    /// <param name="term"></param>
    /// <param name="types"></param>
    /// <param name="pageOptions"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<SearchHintsResponse> GetCatalogSearchHints(string storefront, string? term = null, IReadOnlyCollection<ResourceType>? types = null, PageOptions? pageOptions = null, string? locale = null);

    /// <summary>
    /// Fetch a song by using its identifier.
    /// Route: catalog/{storefront}/songs
    /// https://developer.apple.com/documentation/applemusicapi/get_a_catalog_song
    /// </summary>
    /// <param name="id"></param>
    /// <param name="storefront"></param>
    /// <param name="relationshipsToInclude"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<SongResponse> GetCatalogSong(string id, string storefront, IReadOnlyCollection<SongRelationshipType>? relationshipsToInclude = null, string? locale = null);

    /// <summary>
    /// Fetch a song's relationship by using its identifier.
    /// Route: catalog/{storefront}/songs/{id}/{relationship}
    /// https://developer.apple.com/documentation/applemusicapi/get_a_catalog_song_s_relationship_directly_by_name
    /// </summary>
    /// <param name="id"></param>
    /// <param name="storefront"></param>
    /// <param name="relationship"></param>
    /// <param name="pageOptions"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<SongResponse> GetCatalogSongRelationship(string id, string storefront, SongRelationshipType relationship, PageOptions? pageOptions, string? locale = null);

    /// <summary>
    /// Fetch one or more songs by using their identifiers.
    /// Route: catalog/{storefront}/songs
    /// https://developer.apple.com/documentation/applemusicapi/get_multiple_catalog_songs_by_id
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="storefront"></param>
    /// <param name="include"></param>
    /// <param name="isrc"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<SongResponse> GetMultipleCatalogSongs(IReadOnlyCollection<string> ids, string storefront, IReadOnlyCollection<SongRelationshipType>? include = null, string? isrc = null, string? locale = null);

    /// <summary>
    /// Fetch one or more songs by using their ISRC values.
    /// Route: catalog/{storefront}/songs
    /// https://developer.apple.com/documentation/applemusicapi/get_multiple_catalog_songs_by_isrc
    /// </summary>
    /// <param name="isrc"></param>
    /// <param name="storefront"></param>
    /// <param name="ids"></param>
    /// <param name="include"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<SongResponse> GetMultipleCatalogSongsByIsrc(string isrc, string storefront, IReadOnlyCollection<string>? ids = null, IReadOnlyCollection<SongRelationshipType>? include = null, string? locale = null);

    /// <summary>
    /// Fetch a station by using its identifier.
    /// Route: catalog/{storefront}/stations/{id}
    /// https://developer.apple.com/documentation/applemusicapi/get_a_catalog_station
    /// </summary>
    /// <param name="id"></param>
    /// <param name="storefront"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<StationResponse> GetCatalogStation(string id, string storefront, string? locale = null);

    /// <summary>
    /// Fetch one or more stations by using their identifiers.
    /// Route: catalog/{storefront}/stations
    /// https://developer.apple.com/documentation/applemusicapi/get_multiple_catalog_stations
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="storefront"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    Task<StationResponse> GetMultipleCatalogStations(IReadOnlyCollection<string> ids, string storefront, string? locale = null);
}