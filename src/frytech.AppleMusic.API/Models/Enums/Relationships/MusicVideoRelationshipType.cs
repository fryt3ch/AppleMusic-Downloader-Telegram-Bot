using System.Text.Json.Serialization;

namespace frytech.AppleMusic.API.Models.Enums.Relationships;

public enum MusicVideoRelationshipType
{
    [JsonStringEnumMemberName("albums")]
    Albums,
    [JsonStringEnumMemberName("artists")]
    Artists,
    [JsonStringEnumMemberName("genres")]
    Genres
}