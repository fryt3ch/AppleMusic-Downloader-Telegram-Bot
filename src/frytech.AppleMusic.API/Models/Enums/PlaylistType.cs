using System.Text.Json.Serialization;

namespace frytech.AppleMusic.API.Models.Enums;

/// <summary>
/// The type of playlist.
/// </summary>
public enum PlaylistType
{
    /// <summary>
    /// A playlist created and shared by an Apple Music user.
    /// </summary>
    [JsonStringEnumMemberName("user-shared")]
    UserShared,

    /// <summary>
    /// A playlist created by an Apple Music curator.
    /// </summary>
    [JsonStringEnumMemberName("editorial")]
    Editorial,

    /// <summary>
    /// A playlist created by a non-Apple curator or brand.
    /// </summary>
    [JsonStringEnumMemberName("external")]
    External,

    /// <summary>
    /// A personalized playlist for an Apple Music user.
    /// </summary>
    [JsonStringEnumMemberName("personal-mix")]
    PersonalMix
}