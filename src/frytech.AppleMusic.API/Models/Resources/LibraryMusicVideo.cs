using frytech.AppleMusic.API.Models.Attributes;
using frytech.AppleMusic.API.Models.Core;
using frytech.AppleMusic.API.Models.Enums;
using frytech.AppleMusic.API.Models.Relationships;

namespace frytech.AppleMusic.API.Models.Resources;

/// <summary>
/// A Resource object that represents a library music video.
/// https://developer.apple.com/documentation/applemusicapi/librarymusicvideo
/// </summary>
public class LibraryMusicVideo : Resource, IHasAttributes<LibraryMusicVideoAttributes>, IHasRelationships<LibraryMusicVideoRelationships>
{
    public override ResourceType Type => ResourceType.LibraryMusicVideos;
    
    public LibraryMusicVideoAttributes Attributes { get; set; }
    
    public LibraryMusicVideoRelationships Relationships { get; set; }
}