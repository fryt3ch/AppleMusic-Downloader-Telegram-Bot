using frytech.AppleMusic.API.Models.Attributes;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusic.API.Models.Relationships;

namespace frytech.AppleMusic.API.Models.Resources;

/// <summary>
/// A Resource object that represents a library song.
/// https://developer.apple.com/documentation/applemusicapi/librarysong
/// </summary>
public class LibrarySong : Resource, IHasAttributes<LibrarySongAttributes>, IHasRelationships<LibrarySongRelationships>
{
    public override ResourceType Type => ResourceType.LibrarySongs;
    
    public LibrarySongAttributes Attributes { get; set; }
    
    public LibrarySongRelationships Relationships { get; set; }
}