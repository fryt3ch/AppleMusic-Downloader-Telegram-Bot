using System.Text.Json.Serialization;

namespace frytech.AppleMusic.API.Models.Enums.Relationships;

public enum AppleCuratorRelationshipType
{
    [JsonStringEnumMemberName("playlists")]
    Playlists
}