using frytech.AppleMusic.API.Models.Attributes;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusic.API.Models.Relationships;

namespace frytech.AppleMusic.API.Models.Resources;

/// <summary>
/// A Resource object that represents a library playlist.
/// https://developer.apple.com/documentation/applemusicapi/libraryplaylist
/// </summary>
public class LibraryPlaylist : Resource, IHasAttributes<LibraryPlaylistAttributes>, IHasRelationships<LibraryPlaylistRelationships>
{
    public override ResourceType Type => ResourceType.LibraryPlaylists;
    
    public LibraryPlaylistAttributes Attributes { get; set; }
    
    public LibraryPlaylistRelationships Relationships { get; set; }
}