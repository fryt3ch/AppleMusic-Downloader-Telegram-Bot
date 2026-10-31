using System.Text.Json.Serialization;

namespace frytech.AppleMusic.API.Models.Enums;

public enum CatalogChartType
{
    [JsonStringEnumMemberName("albums")]
    Albums,
    [JsonStringEnumMemberName("music-videos")]
    MusicVideos,
    [JsonStringEnumMemberName("songs")]
    Songs
}