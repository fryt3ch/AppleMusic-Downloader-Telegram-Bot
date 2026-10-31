using frytech.AppleMusic.API.Models.Attributes;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusic.API.Models.Relationships;

namespace frytech.AppleMusic.API.Models.Resources;

/// <summary>
/// A Resource object that represents a library album.
/// https://developer.apple.com/documentation/applemusicapi/libraryalbum
/// </summary>
public class LibraryAlbum : Resource, IHasAttributes<LibraryAlbumAttributes>, IHasRelationships<LibraryAlbumRelationships>
{
    public override ResourceType Type => ResourceType.LibraryAlbums;
    
    public LibraryAlbumAttributes Attributes { get; set; }
    
    public LibraryAlbumRelationships Relationships { get; set; }
}