using frytech.AppleMusic.API.Models.Core;

namespace frytech.AppleMusic.API.Models.Relationships;

/// <summary>
/// The relationships for a curator object.
/// https://developer.apple.com/documentation/applemusicapi/curator/relationships
/// </summary>
public class CuratorRelationships : IRelationships
{
    /// <summary>
    /// The playlists associated with the curator. By default, playlists includes identifiers only.
    /// Fetch limits: 10 default, 10 maximum
    /// </summary>
    public PlaylistRelationship Playlists { get; set; }
}