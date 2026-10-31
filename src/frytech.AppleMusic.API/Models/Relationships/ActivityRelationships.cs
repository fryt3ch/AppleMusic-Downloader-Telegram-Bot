using frytech.AppleMusic.API.Models.Core;

namespace frytech.AppleMusic.API.Models.Relationships;

/// <summary>
/// The relationships for an activity object.
/// https://developer.apple.com/documentation/applemusicapi/activity/relationships
/// </summary>
public class ActivityRelationships : IRelationships
{
    /// <summary>
    /// The playlists associated with this activity. By default, playlists includes identifiers only. Fetch limits: 10 default, 10 maximum
    /// </summary>
    public PlaylistRelationship Playlists { get; set; }
}