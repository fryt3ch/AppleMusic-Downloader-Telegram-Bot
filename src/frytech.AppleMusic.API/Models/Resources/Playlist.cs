using frytech.AppleMusic.API.Models.Attributes;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusic.API.Models.Relationships;

namespace frytech.AppleMusic.API.Models.Resources;

/// <summary>
/// A Resource object that represents a playlist.
/// https://developer.apple.com/documentation/applemusicapi/playlist
/// </summary>
/// <inheritdoc />
public class Playlist : Resource, IHasAttributes<PlaylistAttributes>, IHasRelationships<PlaylistRelationships>
{
    public override ResourceType Type => ResourceType.Playlists;
    
    public PlaylistAttributes Attributes { get; set; }
    
    public PlaylistRelationships Relationships { get; set; }
}