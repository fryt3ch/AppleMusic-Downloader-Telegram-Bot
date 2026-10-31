using System.Text.Json.Serialization;

namespace frytech.AppleMusic.API.Models.Enums.Relationships;

public enum ArtistRelationshipType
{
    [JsonStringEnumMemberName("albums")]
    Albums,
    [JsonStringEnumMemberName("genres")]
    Genres,
    [JsonStringEnumMemberName("musicVideos")]
    MusicVideos,
    [JsonStringEnumMemberName("playlists")]
    Playlists,
    [JsonStringEnumMemberName("stations")]
    Stations,
    [JsonStringEnumMemberName("songs")]
    Songs
}