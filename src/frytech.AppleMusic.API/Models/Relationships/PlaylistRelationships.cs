using frytech.AppleMusic.API.Models.Core;

namespace frytech.AppleMusic.API.Models.Relationships;

/// <summary>
/// The relationships for a playlist object.
/// https://developer.apple.com/documentation/applemusicapi/playlist/relationships
/// </summary>
public class PlaylistRelationships : IRelationships
{
    /// <summary>
    /// The curator that created the playlist. By default, curator includes identifiers only.
    /// Fetch limits: None
    /// </summary>
    public CuratorRelationship? Curator { get; set; }

    /// <summary>
    /// The songs and music videos included in the playlist. By default, tracks includes objects.
    /// Fetch limits: 100 default, 300 maximum
    /// </summary>
    public TrackRelationship Tracks { get; set; }
}