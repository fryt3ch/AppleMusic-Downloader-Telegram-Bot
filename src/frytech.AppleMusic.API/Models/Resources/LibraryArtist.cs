using frytech.AppleMusic.API.Models.Attributes;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusic.API.Models.Relationships;

namespace frytech.AppleMusic.API.Models.Resources;

/// <summary>
/// A Resource object that represents a library artist.
/// https://developer.apple.com/documentation/applemusicapi/libraryartist
/// </summary>
/// <inheritdoc />
public class LibraryArtist : Resource, IHasAttributes<LibraryArtistAttributes>, IHasRelationships<LibraryArtistRelationships>
{
    public override ResourceType Type => ResourceType.LibraryArtists;
    
    public LibraryArtistAttributes Attributes { get; set; }
    
    public LibraryArtistRelationships Relationships { get; set; }
}