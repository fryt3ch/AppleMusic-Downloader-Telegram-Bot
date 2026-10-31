using System.Text.Json.Serialization;

namespace frytech.AppleMusic.API.Models.Enums.Relationships;

public enum LibraryMusicVideoRelationshipType
{
    [JsonStringEnumMemberName("albums")]
    Albums,
    [JsonStringEnumMemberName("artists")]
    Artists
}