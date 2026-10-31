using System.Text.Json.Serialization;

namespace frytech.AppleMusic.API.Models.Enums.Relationships;

public enum AlbumRelationshipType
{
    [JsonStringEnumMemberName("artists")]
    Artists,
    [JsonStringEnumMemberName("genres")]
    Genres,
    [JsonStringEnumMemberName("tracks")]
    Tracks
}