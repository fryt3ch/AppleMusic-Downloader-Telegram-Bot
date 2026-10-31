using System.Text.Json.Serialization;

namespace frytech.AppleMusic.API.Models.Enums.Relationships;

public enum CuratorRelationshipType
{
    [JsonStringEnumMemberName("playlists")]
    Playlists
}