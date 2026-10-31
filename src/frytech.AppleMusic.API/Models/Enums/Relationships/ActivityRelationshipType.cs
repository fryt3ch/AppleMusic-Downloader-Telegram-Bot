using System.Text.Json.Serialization;

namespace frytech.AppleMusic.API.Models.Enums.Relationships;

public enum ActivityRelationshipType
{
    [JsonStringEnumMemberName("playlists")]
    Playlists
}