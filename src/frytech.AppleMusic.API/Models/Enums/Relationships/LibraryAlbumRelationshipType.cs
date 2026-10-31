using System.Text.Json.Serialization;

namespace frytech.AppleMusic.API.Models.Enums.Relationships;

public enum LibraryAlbumRelationshipType
{
    [JsonStringEnumMemberName("artists")]
    Artists,
    [JsonStringEnumMemberName("tracks")]
    Tracks
}