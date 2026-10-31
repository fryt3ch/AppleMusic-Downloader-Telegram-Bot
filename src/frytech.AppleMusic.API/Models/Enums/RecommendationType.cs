using System.Text.Json.Serialization;

namespace frytech.AppleMusic.API.Models.Enums;

public enum RecommendationType
{
    [JsonStringEnumMemberName("albums")]
    Albums,
    [JsonStringEnumMemberName("playlists")]
    Playlists
}