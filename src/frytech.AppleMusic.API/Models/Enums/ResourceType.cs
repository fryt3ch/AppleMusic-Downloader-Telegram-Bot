using System.Text.Json.Serialization;

namespace frytech.AppleMusic.API.Models.Enums;

public enum ResourceType
{
    [JsonStringEnumMemberName("activities")]
    Activities,
    
    [JsonStringEnumMemberName("artists")]
    Artists,
    
    [JsonStringEnumMemberName("albums")]
    Albums,
    
    [JsonStringEnumMemberName("apple-curators")]
    AppleCurators,
    
    [JsonStringEnumMemberName("curators")]
    Curators,
    
    [JsonStringEnumMemberName("genres")]
    Genres,
    
    [JsonStringEnumMemberName("playlists")]
    Playlists,
    
    [JsonStringEnumMemberName("songs")]
    Songs,
    
    [JsonStringEnumMemberName("stations")]
    Stations,
    
    [JsonStringEnumMemberName("music-videos")]
    MusicVideos,
    
    [JsonStringEnumMemberName("personal-recommendation")]
    Recommendation,
    
    [JsonStringEnumMemberName("storefronts")]
    Storefronts,
    
    [JsonStringEnumMemberName("ratings")]
    Ratings,
    
    [JsonStringEnumMemberName("library-albums")]
    LibraryAlbums,
    
    [JsonStringEnumMemberName("library-artists")]
    LibraryArtists,
    
    [JsonStringEnumMemberName("library-music-videos")]
    LibraryMusicVideos,
    
    [JsonStringEnumMemberName("library-playlists")]
    LibraryPlaylists,
    
    [JsonStringEnumMemberName("library-songs")]
    LibrarySongs
}