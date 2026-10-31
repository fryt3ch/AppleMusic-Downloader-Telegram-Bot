using System.Text.Json.Serialization;

namespace frytech.AppleMusic.API.Models.Enums;
#pragma warning disable IDE1006 // Naming Styles
public enum iCloudMusicLibraryType
#pragma warning restore IDE1006 // Naming Styles
{
    [JsonStringEnumMemberName("albums")]
    Albums,
    [JsonStringEnumMemberName("music-videos")]
    MusicVideos,
    [JsonStringEnumMemberName("playlists")]
    Playlists,
    [JsonStringEnumMemberName("songs")]
    Songs
}