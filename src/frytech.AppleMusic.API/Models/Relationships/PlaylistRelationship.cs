using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Resources;

namespace frytech.AppleMusic.API.Models.Relationships;

/// <summary>
/// An object that represents the playlist relationship for a Resource object.
/// https://developer.apple.com/documentation/applemusicapi/playlistrelationship
/// </summary>
public class PlaylistRelationship : Relationship<Playlist>;