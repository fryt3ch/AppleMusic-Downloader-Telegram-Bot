using System.Text.Json.Serialization;

namespace frytech.AppleMusic.API.Models.Enums.Relationships;

public enum PlaylistRelationshipType
{
    [JsonStringEnumMemberName("curator")]
    Curator,
    [JsonStringEnumMemberName("tracks")]
    Tracks
}